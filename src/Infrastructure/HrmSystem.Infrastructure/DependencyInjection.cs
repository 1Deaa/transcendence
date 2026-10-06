using System.Text;
using Dapper;
using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Caching;
using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Emailing;
using HrmSystem.Application.Common.Interfaces.Operations;
using HrmSystem.Application.Common.Interfaces.Reporting;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Infrastructure.BackgroundJobs;
using HrmSystem.Infrastructure.HealthChecks;
using HrmSystem.Infrastructure.Persistence.Configurations;
using HrmSystem.Infrastructure.Persistence.Connections;
using HrmSystem.Infrastructure.Persistence.Contexts;
using HrmSystem.Infrastructure.Persistence.Interceptors;
using HrmSystem.Infrastructure.Persistence.Queries;
using HrmSystem.Infrastructure.Persistence.Repositories;
using HrmSystem.Infrastructure.Persistence.Seeding;
using HrmSystem.Infrastructure.Persistence.Seeding.Seeders;
using HrmSystem.Infrastructure.Persistence.TypeHandlers;
using HrmSystem.Infrastructure.Services.Backup;
using HrmSystem.Infrastructure.Services.Export;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HrmSystem.Infrastructure.Services.Caching;
using HrmSystem.Infrastructure.Services.Client;
using HrmSystem.Infrastructure.Services.Clock;
using HrmSystem.Infrastructure.Services.Emailing;
using HrmSystem.Infrastructure.Services.Identity;
using HrmSystem.Infrastructure.Services.Jwt;
using HrmSystem.Infrastructure.Services.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HrmSystem.Infrastructure;

public static class DependencyInjection
{
    /*
    //? Entry point called once from the Web layer:
    //>   builder.Services.AddInfrastructure()
    */
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddEmailingServices(configuration)
            .AddClockService()
            .AddClientUrlProvider(configuration)
            .AddPersistenceServices(configuration)
            .AddIdentityDbContextService(configuration)
            .AddIdentityAuthenticationService()
            .AddJwtAuthenticationService(configuration)
            .AddAuthorization()
            .AddRedisCaching(configuration)
            .AddOperationsModule(configuration);

        return services;
    }

    /*
        //?     The health/status/backup module: options, Quartz jobs, custom health checks,
        //?     the Dapper status queries, and the backup services.
        //!     Tags drive the endpoints: "live" → /health, "ready" → /health/ready,
        //!     "component" → persisted by HealthProbeJob and shown on the status page.
    */
    private static IServiceCollection AddOperationsModule(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<BackupOptions>(configuration.GetSection(BackupOptions.SectionName));
        services.Configure<HealthOptions>(configuration.GetSection(HealthOptions.SectionName));

        services.AddScoped<SqlServerBackupService>();
        services.AddScoped<IBackupScheduler, QuartzBackupScheduler>();
        services.AddScoped<IImportScheduler, QuartzImportScheduler>();
        services.AddScoped<IBackupHistoryRepository, BackupHistoryRepository>();
        services.AddScoped<IImportJobRepository, ImportJobRepository>();
        services.AddScoped<IStatusQueries, StatusQueries>();
        services.AddScoped<IAnalyticsQueries, AnalyticsQueries>();
        services.AddScoped<IChatQueries, ChatQueries>();
        services.AddScoped<IAdminQueries, AdminQueries>();

        /*
            //?     One renderer per export format — the export handler picks by Format key.
            //!     QuestPDF Community license constraint documented in docs/architecture.md.
        */
        services.AddSingleton<IAnalyticsReportRenderer, CsvAnalyticsReportRenderer>();
        services.AddSingleton<IAnalyticsReportRenderer, PdfAnalyticsReportRenderer>();

        services.AddBackgroundJobs(configuration);

        string databaseConnection =
            configuration.GetConnectionString("Database")
            ?? throw new ArgumentNullException(nameof(configuration));
        string redisConnection =
            configuration.GetConnectionString("RedisCache")
            ?? throw new ArgumentNullException(nameof(configuration));

        services
            .AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy("API process is running."),
                tags: ["live"]
            )
            .AddSqlServer(databaseConnection, name: "sqlserver", tags: ["ready", "component"])
            .AddRedis(redisConnection, name: "redis", tags: ["ready", "component"])
            .AddCheck<QuartzSchedulerHealthCheck>("scheduler", tags: ["ready", "component"])
            .AddCheck<DiskSpaceHealthCheck>("disk-space", tags: ["component"])
            .AddCheck<MemoryHealthCheck>("memory", tags: ["component"])
            .AddCheck<BackupFreshnessHealthCheck>("backup-freshness", tags: ["component"]);

        return services;
    }

    private static IServiceCollection AddClientUrlProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<ClientSettings>(configuration.GetSection("ClientSettings"));
        services.AddTransient<IClientUrlProvider, ClientUrlProvider>();

        return services;
    }

    private static IServiceCollection AddEmailingServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));

        //? Singleton: TemplateRenderer is stateless and its compiled Regex is thread-safe.
        services.AddSingleton<ITemplateRenderer, TemplateRenderer>();
        services.AddTransient<IEmailService, EmailService>();

        return services;
    }

    private static IServiceCollection AddClockService(this IServiceCollection services)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();

        return services;
    }

    private static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        string connectionString =
            configuration.GetConnectionString("Database")
            ?? throw new ArgumentNullException(nameof(configuration));

        /*
            //?     Both SaveChanges interceptors are resolved from the scoped provider:
            //!      1. TenantWriteGuardInterceptor — stamps/validates TenantId (MUST run first).
            //!      2. AuditableEntityInterceptor  — stamps CreatedAt/By + LastModifiedAt/By.
        */
        services.AddScoped<TenantWriteGuardInterceptor>();
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>(
            (serviceProvider, dbContextOptionsBuilder) =>
            {
                dbContextOptionsBuilder.UseSqlServer(
                    connectionString,
                    sqlServerDbContextOptionsBuilder =>
                    {
                        sqlServerDbContextOptionsBuilder.MigrationsHistoryTable(
                            HistoryRepository.DefaultTableName,
                            DbConfigurationSettings.Schemas.Application
                        );
                    }
                );
                ////.UseSnakeCaseNamingConvention(); //! This is example I can add if I want to use for example [PostgreSQL] to Align Naming of {Table, Cols} with Snake Case Naming Convention which is default for [PostgreSQL]

                dbContextOptionsBuilder.AddInterceptors(
                    serviceProvider.GetRequiredService<TenantWriteGuardInterceptor>(),
                    serviceProvider.GetRequiredService<AuditableEntityInterceptor>()
                );
            }
        );

        services.AddRepositoriesAndUOF();
        services.AddConnectionFactoryForDapper(connectionString);
        services.AddSeedingServices();

        //! Process-global Dapper registration — DateOnly ⇄ SQL [date] for parameters AND reads.
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        return services;
    }

    private static IServiceCollection AddIdentityDbContextService(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        string connectionString =
            configuration.GetConnectionString("Database")
            ?? throw new ArgumentNullException(nameof(configuration));

        services.AddDbContext<ApplicationIdentityDbContext>(dbContextOptionsBuilder =>
        {
            dbContextOptionsBuilder.UseSqlServer(
                connectionString,
                sqlServerDbContextOptionsBuilder =>
                {
                    sqlServerDbContextOptionsBuilder.MigrationsHistoryTable(
                        HistoryRepository.DefaultTableName,
                        DbConfigurationSettings.Schemas.Identity
                    );
                }
            );
        });

        return services;
    }

    private static IServiceCollection AddIdentityAuthenticationService(
        this IServiceCollection services
    )
    {
        /*
            //! ✗ Wrong for API — auto-registers cookie schemes you don't need
             ////   services
             ////       .AddIdentity<IdentityUser, IdentityRole>()
             ////       .AddEntityFrameworkStores<ApplicationIdentityDbContext>();
         */

        services
            .AddIdentityCore<AppUser>(identityOptionsSetupAction =>
            {
                /*
                    //!     Keep in sync with CommonValidationRules.StrongPassword — the
                    //!     FluentValidation pre-check must reject exactly what Identity rejects.
                    //?     RequireNonAlphanumeric stays off so the seeded demo accounts
                    //?     (password "Seed1234") remain valid.
                */
                identityOptionsSetupAction.Password.RequiredLength = 8;
                identityOptionsSetupAction.Password.RequireDigit = true;
                identityOptionsSetupAction.Password.RequireNonAlphanumeric = false;
                identityOptionsSetupAction.Password.RequireUppercase = true;
                identityOptionsSetupAction.Password.RequireLowercase = true;
                identityOptionsSetupAction.Password.RequiredUniqueChars = 0;
                identityOptionsSetupAction.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationIdentityDbContext>()
            /*
                //!     [AddDefaultTokenProviders] registers the Data Protection token providers
                //!     required for [GeneratePasswordResetTokenAsync] and
                //!     [GenerateEmailConfirmationTokenAsync] to work.
                //!     Without this, calling those methods throws [NotSupportedException].
                //!     [AddIdentityCore] (unlike [AddIdentity]) does NOT register these automatically.
            */
            .AddDefaultTokenProviders();

        /*
            //*     [AuthenticationService] owns the cross-context transaction logic.
            //*     Registered as Scoped because [UserManager<AppUser>] and both DbContexts are Scoped.
            //!     Never register as Singleton — DbContext is Scoped and cannot be consumed by a Singleton.
        */
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        /*
            //?     [CurrentTenantContext] resolves the tenant from the ambient AsyncLocal scope
            //?     (background jobs / seeders) or the "tenant_id" JWT claim (HTTP requests).
            //!     Consumed by the TenantWriteGuardInterceptor and (Task/Step 5) the named
            //!     tenant query filter in ATenantEntityConfiguration.
        */
        services.AddScoped<ITenantContext, CurrentTenantContext>();

        return services;
    }

    private static IServiceCollection AddJwtAuthenticationService(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<JwtAuthOptions>(configuration.GetSection("JwtSettings"));

        //*     Scoped — stateless token generation but depends on IOptions<JwtAuthOptions> which is Singleton-safe.
        services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();

        JwtAuthOptions jwtAuthOptions = configuration
            .GetSection("JwtSettings")
            .Get<JwtAuthOptions>()!;

        services
            .AddAuthentication(authConfigOptions =>
            {
                authConfigOptions.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;
                authConfigOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(jwtBearerConfigOptions =>
            {
                /*
                    //!     Keep the JWT's own claim names — do NOT rewrite them.
                    //!     With the default (true), the handler renames the short OIDC claims
                    //!     to WS-Federation URIs: "sub" → ClaimTypes.NameIdentifier and
                    //!     "email" → ClaimTypes.Email. Every lookup by [CustomClaimTypes.Sub]
                    //!     and [CustomClaimTypes.Email] then returned null, which silently
                    //!     disabled the blocked-account veto (BlockedUserAuthorizationHandler)
                    //!     and made ICurrentUserContext.Email always null.
                    //?     Roles are unaffected either way — JwtTokenProvider already writes
                    //?     them as the full [ClaimTypes.Role] URI.
                */
                jwtBearerConfigOptions.MapInboundClaims = false;

                jwtBearerConfigOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtAuthOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAuthOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtAuthOptions.Key)
                    ),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        /*
            //?     Applies JwtBearerOptionsSetup — its OnMessageReceived handler reads
            //?     the JWT from ?access_token on /hubs paths, which is how the SignalR
            //?     JS client authenticates WebSocket handshakes (browsers cannot set an
            //?     Authorization header on a WS upgrade).
        */
        services.ConfigureOptions<JwtBearerOptionsSetup>();

        return services;
    }

    private static IServiceCollection AddSeedingServices(this IServiceCollection services)
    {
        services.AddScoped<DatabaseInitialiser>();

        //? Register seeders — DI resolves IEnumerable<ISeeder> automatically in DatabaseInitialiser.
        //? To add a new seeder: one line here. DatabaseInitialiser.SeedAsync runs them in Order.
        services.AddScoped<ISeeder, RolesAndPermissionsSeeder>(); // Order: 5  — runs in Production too
        services.AddScoped<ISeeder, TenantSeeder>();              // Order: 8  — dev/staging only
        services.AddScoped<ISeeder, UserSeeder>();                // Order: 10 — dev/staging only
        services.AddScoped<ISeeder, DepartmentSeeder>();          // Order: 40 — dev/staging only
        services.AddScoped<ISeeder, EmployeeSeeder>();            // Order: 50 — dev/staging only
        services.AddScoped<ISeeder, AttendanceSeeder>();          // Order: 60 — dev/staging only
        services.AddScoped<ISeeder, LeaveRequestSeeder>();        // Order: 70 — dev/staging only

        return services;
    }

    private static IServiceCollection AddRepositoriesAndUOF(this IServiceCollection services)
    {
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IUnitOfWork>(serviceProvider =>
        {
            return serviceProvider.GetRequiredService<ApplicationDbContext>();
        });

        return services;
    }

    private static IServiceCollection AddConnectionFactoryForDapper(
        this IServiceCollection services,
        string ConnectionString
    )
    {
        services.AddSingleton<ISqlConnectionFactory>(_ =>
        {
            return new SqlConnectionFactory(ConnectionString);
        });

        //! Consider Adding For Dapper cases that needs it in SQL Server
        //SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        return services;
    }

    private static IServiceCollection AddRedisCaching(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        string redisCachingConfigString =
            configuration.GetConnectionString("RedisCache")
            ?? throw new ArgumentNullException(nameof(configuration));

        services.AddStackExchangeRedisCache(redisCacheOptions =>
        {
            redisCacheOptions.Configuration = redisCachingConfigString;
        });

        services.AddSingleton<ICacheService, CacheService>();

        return services;
    }
}
