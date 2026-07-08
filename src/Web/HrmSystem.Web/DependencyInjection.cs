using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using HrmSystem.Application.Common.Interfaces.RealTime;
using HrmSystem.Web.Authentication;
using HrmSystem.Web.Formatters;
using HrmSystem.Web.Hubs;
using HrmSystem.Web.Middlewares;
using HrmSystem.Web.OpenApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HrmSystem.Web;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        WebApplicationBuilder builder
    )
    {
        services
            .AddControllersServices()
            .AddVersioningService()
            .AddOpenTelemeterySetup(builder)
            .AddOpenApiService()
            .AddExceptionHandlingService()
            .AddMvcCookieScheme()
            .AddAuthorizationPolicies()
            .AddRealTimeServices();

        return services;
    }

    /*
        //?     SignalR hubs + the Web-side implementations of the Application notifier ports.
        //>     StatusHub (/hubs/status) — anonymous status-page push (HealthProbeJob transitions).
    */
    private static IServiceCollection AddRealTimeServices(this IServiceCollection services)
    {
        services.AddSignalR();

        services.AddScoped<IStatusNotifier, StatusNotifier>();
        services.AddScoped<IAnalyticsNotifier, AnalyticsNotifier>();
        services.AddScoped<IImportProgressNotifier, ImportProgressNotifier>();

        return services;
    }

    public static IServiceCollection AddControllersServices(this IServiceCollection services)
    {
        //! Without StringEnumConverter: {"status": 0} <-------> With StringEnumConverter: {"status": "Active"}
        /*
            //?     [AddControllers] (not [AddControllersWithViews]) — this host is API-only:
            //?     all UI lives in the standalone Blazor WASM client (src/Client/HrmSystem.Client),
            //?     which talks to this API exclusively over REST + SignalR.
        */
        services
            .AddControllers(controllerOptions =>
            {
                controllerOptions.ReturnHttpNotAcceptable = true;

                //? CSV joins JSON/XML in content negotiation (export endpoints).
                controllerOptions.OutputFormatters.Add(new CsvOutputFormatter());

                /*
                    //?     ?format= → media type for [FormatFilter] endpoints:
                    //>     GET /api/employees/export?format=csv | xml | json
                */
                controllerOptions.FormatterMappings.SetMediaTypeMappingForFormat(
                    "csv",
                    "text/csv"
                );
                controllerOptions.FormatterMappings.SetMediaTypeMappingForFormat(
                    "xml",
                    "application/xml"
                );
                controllerOptions.FormatterMappings.SetMediaTypeMappingForFormat(
                    "json",
                    "application/json"
                );
            })
            .AddNewtonsoftJson(options =>
            {
                //options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
                options.SerializerSettings.NullValueHandling = NullValueHandling.Include;

                options.SerializerSettings.Converters.Add(
                    //Newtonsoft.Json.Converters.StringEnumConverter()
                    //Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()

                    new StringEnumConverter() { NamingStrategy = new CamelCaseNamingStrategy() }
                );
            })
            .AddXmlSerializerFormatters();

        /*
            //!     Newtonsoft's output formatter only registers [application/json] by default.
            //!     ASP.NET Core's [Problem()] method sets the response content-type to
            //!     [application/problem+json] (RFC 9457). With [ReturnHttpNotAcceptable = true],
            //!     if no formatter claims that media type the response becomes 406 instead of
            //!     the intended 400/401/404 problem JSON.
            //*     Adding [application/problem+json] to the Newtonsoft formatter makes it handle
            //*     [ProblemDetails] responses the same way it handles ordinary JSON — no more 406.
            //!     Do NOT add [application/problem+xml] here: MVC's media-type matching treats the
            //!     [+xml] suffix as a subset of [application/xml], so the JSON formatter would
            //!     steal [?format=xml] requests and write JSON with an XML content type.
            //?     Problem-details-as-XML is already covered by the XmlSerializer formatter.
        */
        services.Configure<MvcOptions>(options =>
        {
            NewtonsoftJsonOutputFormatter? newtonsoftFormatter = options
                .OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>()
                .FirstOrDefault();

            newtonsoftFormatter?.SupportedMediaTypes.Add("application/problem+json");
        });

        return services;
    }

    public static IServiceCollection AddOpenTelemeterySetup(
        this IServiceCollection services,
        WebApplicationBuilder builder
    )
    {
        //! This will Setup OpenTelemetry with exporting [traces] and [metrics] using the OpenTelemetry Protocol (OTLP) exporter, which can send data to various backends that support OTLP, such as Jaeger, Zipkin, or OpenTelemetry Collector.
        services
            .AddOpenTelemetry()
            .ConfigureResource(
                (resource) =>
                {
                    resource.AddService(builder.Environment.ApplicationName);
                }
            )
            .WithTracing(
                (tracing) =>
                {
                    tracing
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddSqlClientInstrumentation();
                }
            )
            .WithMetrics(
                (metrics) =>
                {
                    metrics
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddSqlClientInstrumentation()
                        .AddRuntimeInstrumentation();
                }
            )
            .UseOtlpExporter();

        builder.Logging.AddOpenTelemetry(
            (oltpLoggingOptions) =>
            {
                //! [IncludeScopes]; Default is [false]: This option determines whether to include the scopes of log messages in the exported telemetry data. Scopes provide additional context about the log messages, such as the logical operation or activity they are associated with. By setting this option to true, you can include this contextual information in your telemetry data, which can be helpful for debugging and monitoring purposes.
                oltpLoggingOptions.IncludeFormattedMessage = true;
                oltpLoggingOptions.IncludeScopes = true;
            }
        );

        return services;
    }

    public static IServiceCollection AddVersioningService(this IServiceCollection services)
    {
        /*
              //?     Two-step chain: [AddApiVersioning] returns [IApiVersioningBuilder], not [IMvcBuilder],
              //?     so it cannot be appended to the [AddControllersWithViews] chain above.
              //
              //!     Temporary: [QueryStringApiVersionReader] — no route or header changes needed.
              //!     When HATEOAS is ready, swap it for [MediaTypeApiVersionReader] with your
              //!     custom template: application/vnd.milestonestack.hateoas.{version}+json
              //
              //*     [.AddMvc()] here is [Asp.Versioning.IApiVersioningBuilder.AddMvc()] — the versioning
              //*     MVC integration — NOT [IServiceCollection.AddMvc()]. Different method, same name.
        */
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new QueryStringApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }

    //TODO: Search/Study/Find Scalar way to conditionally add OpenAPI only in development, or at least not add the services in production. We can still map the endpoint in production but it won't do anything if the services aren't registered.
    public static IServiceCollection AddOpenApiService(this IServiceCollection services)
    {
        //services.AddOpenApi(); //! As we commented: `app.MapOpenApi()` because it has a limitations in documenting our Apis

        //!    This is the Swagger generator for OpenApi Specification, which is more powerful and flexible than the built-in [[OpenApi]] library. It can generate a Swagger document (The OpenApi Specification) better than [`app.MapOpenApi()`]. [services.AddSwaggerGen(...)] describes our API endpoints, parameters, responses, etc. We can also customize the generated document using various options and attributes.
        services.AddSwaggerGen();
        services.ConfigureOptions<ConfigureSwaggerGenOptions>();
        services.ConfigureOptions<ConfigureSwaggerUiOptions>();

        return services;
    }

    private static IServiceCollection AddMvcCookieScheme(this IServiceCollection services)
    {
        /*
            //?     Infrastructure already called [AddAuthentication(opts => defaults = JWT)].
            //?     Calling [AddAuthentication()] here with NO arguments just gets the builder
            //?     so we can chain [AddCookie] — it does NOT override the JWT defaults.
            //
            //!     Never call [AddAuthentication("MvcCookie")] here — that would override
            //!     the JWT default scheme set by Infrastructure and break all API endpoints.
            //
            //*     The cookie scheme is only used by Razor View controllers that explicitly
            //*     declare [Authorize(AuthenticationSchemes = AuthSchemes.MvcCookie)] or
            //*     use the [MvcUser] policy defined below.
        */
        services
            .AddAuthentication()
            .AddCookie(
                AuthSchemes.MvcCookie,
                opts =>
                {
                    opts.LoginPath = "/auth/login";
                    opts.LogoutPath = "/auth/logout";
                    opts.AccessDeniedPath = "/auth/forbidden";
                    opts.Cookie.Name = "HrmSystem.Auth";
                    opts.Cookie.HttpOnly = true;
                    opts.Cookie.SameSite = SameSiteMode.Lax;
                }
            );

        return services;
    }

    private static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        /*
            //?     Two policies — one per client type.
            //*     [ApiUser]  — for API controllers; uses JWT Bearer (the global default).
            //*     [MvcUser]  — for Razor View controllers; uses the cookie scheme.
            //
            //!     API controllers can also just use plain [Authorize] since JWT is the
            //!     default scheme — [ApiUser] policy is optional but makes intent explicit.
            //!     MVC controllers MUST use [MvcUser] or specify the scheme explicitly,
            //!     because the default is JWT and a browser cannot send JWT automatically.


            //?       How to use this on controllers
            //?
            //>       // API controller — JWT is the default, just [Authorize] works
            //>       [Authorize]
            //>       public sealed class HabitsController : ApiBaseController { }
            //?
            //>       // Or explicitly:
            //>       [Authorize(Policy = AuthPolicies.ApiUser)]
            //>       public sealed class HabitsController : ApiBaseController { }
            //?
            //>       // MVC controller — MUST declare cookie scheme
            //>       [Authorize(Policy = AuthPolicies.MvcUser)]
            //>       public class DashboardController : Controller { }
            //?
            //?       Why the cookie warning is true but not an absolute rule
            //?
            //!       Browser hits /admin/dashboard
            //!         → [Authorize(Policy = "MvcUser")] → cookie scheme
            //!         → not logged in → redirects to /Account/Login  ✓ (browser-friendly)
            //?
            //!       Browser hits /api/habits
            //!         → [Authorize] → JWT default scheme
            //!         → no token → 401 Unauthorized JSON response  ✓ (API-friendly)
        */
        /*
            //?     [PermissionAuthorizationPolicyProvider] replaces the default provider.
            //?     It falls through to the base for named policies (ApiUser, MvcUser) and
            //?     creates a [PermissionAuthorizationRequirement] policy on-the-fly for "resource:action" names.
            //!     Must be registered as [Singleton] — [IAuthorizationPolicyProvider] is resolved
            //!     once per application lifetime by the authorization middleware.
        */
        services.AddSingleton<
            IAuthorizationPolicyProvider,
            PermissionAuthorizationPolicyProvider
        >();

        /*
            //?     [PermissionAuthorizationHandler] evaluates [PermissionAuthorizationRequirement] against
            //?     the current principal's claims for both JWT and Cookie auth schemes.
            //!     Must be registered as [Singleton] — it is stateless and thread-safe.
        */
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        /*
            //?     [BlockedUserAuthorizationHandler] is the second handler for the same
            //?      [PermissionAuthorizationRequirement]. [ASP.NET Core] runs all registered handlers —
            //?      so every permission check also runs this veto.
            //*     Only vetoes ([context.Fail]) when the domain [User.IsActive] is false.
            //!     Registered as [Scoped] (not Singleton) because it depends on [IIdentityService],
            //!     which owns [UserManager<AppUser>] — a Scoped service.
        */
        services.AddScoped<IAuthorizationHandler, BlockedUserAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthPolicies.ApiUser,
                p => p.RequireAuthenticatedUser().AddAuthenticationSchemes(AuthSchemes.JwtBearer)
            );

            options.AddPolicy(
                AuthPolicies.MvcUser,
                p => p.RequireAuthenticatedUser().AddAuthenticationSchemes(AuthSchemes.MvcCookie)
            );
        });

        return services;
    }

    public static IServiceCollection AddExceptionHandlingService(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
