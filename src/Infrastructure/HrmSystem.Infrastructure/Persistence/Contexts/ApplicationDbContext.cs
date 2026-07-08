using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using HrmSystem.Application.Common.Exceptions;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Entities.Attendances;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Imports;
using HrmSystem.Domain.Entities.LeaveRequests;
using HrmSystem.Domain.Entities.Operations;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.RefreshTokens;
using HrmSystem.Infrastructure.Persistence.Configurations;
using HrmSystem.Infrastructure.Services.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Contexts;

public sealed class ApplicationDbContext : DbContext, IUnitOfWork
{
    private readonly IPublisher _publisher;
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> dbContextOptions,
        IPublisher publisher,
        ITenantContext tenantContext
    )
        : base(dbContextOptions)
    {
        _publisher = publisher;
        _tenantContext = tenantContext;
    }

    /*
        //?     Read by the tenant query filter below. EF Core rewrites members reached through the
        //?     DbContext instance inside filter lambdas to the CURRENT context instance at query
        //?     time — so every request/scope gets ITS tenant, despite the model being cached once.
    */
    private TenantId? CurrentTenantId => _tenantContext.Current;

    /*
        //*     Typed DbSet properties — prefer these over .Set<T>() everywhere in Infrastructure.
        //*     EF Core resolves them from the same internal set; this is just the intended surface API.
    */
    public DbSet<User> Users { get; init; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; init; } = null!;
    public DbSet<Tenant> Tenants { get; init; } = null!;
    public DbSet<Department> Departments { get; init; } = null!;
    public DbSet<Employee> Employees { get; init; } = null!;
    public DbSet<Attendance> Attendances { get; init; } = null!;
    public DbSet<LeaveRequest> LeaveRequests { get; init; } = null!;
    public DbSet<ImportJob> ImportJobs { get; init; } = null!;

    //? Host-level operations data (health/backups module) — never tenant-filtered.
    public DbSet<BackupHistory> BackupHistories { get; init; } = null!;
    public DbSet<HealthCheckSnapshot> HealthCheckSnapshots { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DbConfigurationSettings.Schemas.Application);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ApplyTenantQueryFilters(modelBuilder);

        /*
            //?  [AppUser] is simple — no owned types, no complex navigations to cascade-discover.
            //?   Registering it here (the Application context) with [ExcludeFromMigrations()] lets
            //?   [UserConfiguration] express a proper 1:1 FK via [HasOne<AppUser>().WithOne()].
            //!  [ApplicationIdentityDbContext] ignores [AppUser.DomainUser] to prevent the reverse
            //!  auto-discovery from pulling [User] and all its owned types into the Identity context.
        */
        modelBuilder
            .Entity<AppUser>()
            .ToTable(
                "IdentityUsers",
                DbConfigurationSettings.Schemas.Identity,
                t => t.ExcludeFromMigrations()
            );

        base.OnModelCreating(modelBuilder);
    }

    /*
        //?     Read-side tenant isolation — the twin of TenantWriteGuardInterceptor.
        //?     EF Core 10 NAMED query filters let soft-delete and tenant isolation coexist and be
        //?     ignored independently: .IgnoreQueryFilters(["Tenant"]) for host-admin queries keeps
        //?     the soft-delete filter active (a single anonymous filter can't do that).
        //
        //!     The base configuration set an anonymous !IsDeleted filter — replaced here with the
        //!     named pair for tenant-owned entities only; host-level entities keep the anonymous one.
        //
        //!     "CurrentTenantId == null → no rows" is deliberate: a host-level context (no tenant
        //!     claim, no ambient scope) must never silently read tenant data — it must opt in
        //!     explicitly via IgnoreQueryFilters.
    */
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        MethodInfo applyMethod = typeof(ApplicationDbContext).GetMethod(
            nameof(ApplyTenantEntityFilters),
            BindingFlags.Public | BindingFlags.Instance
        )!;

        foreach (Type clrType in modelBuilder.Model.GetEntityTypes().Select(e => e.ClrType))
        {
            if (typeof(ITenantOwned).IsAssignableFrom(clrType) && !clrType.IsAbstract)
            {
                applyMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
            }
        }
    }

    //! Public (not private) so the reflection call above needs no accessibility bypass (Sonar S3011).
    public void ApplyTenantEntityFilters<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantOwned, ISoftDeletable
    {
        /*
            //!     The base configuration hierarchy applied an ANONYMOUS !IsDeleted filter, and
            //!     EF Core 10 refuses to mix anonymous and named filters on one entity — so the
            //!     anonymous one is cleared first, then re-expressed as the named "SoftDelete".
        */
        modelBuilder
            .Entity<TEntity>()
            .HasQueryFilter((Expression<Func<TEntity, bool>>?)null)
            .HasQueryFilter("SoftDelete", entity => !entity.IsDeleted)
            .HasQueryFilter("Tenant", entity => entity.TenantId == CurrentTenantId);
    }

    /*
        //!   - Why rowversion (not LastModifiedAt)
        //?         - [LastModifiedAt] is [DateTime] — on Windows, clock resolution is ~15ms. Two concurrent writes in the same tick both read the same timestamp, both pass the check,   and you silently lose one write. SQL Server's rowversion is a monotonically increasing 8-byte counter maintained at the database engine level — one transaction, one increment, no   collisions ever.
        //!
        //!   - Why ((byte[])) and not ((uint)) like the instructor
        //?         - The Guide uses [[PostgreSQL]]. [[Npgsql]] maps [[ uint IsRowVersion() ]] to the [[xmin]] system column (no migration needed).
        //?         - [[SQL Server's]] provider maps [[ byte[] IsRowVersion() ]] to the [[rowversion]]
        //>       Different column types. Different providers, same EF Core API, different underlying types.
        //!
        //!   - The full race condition flow that's now protected:
        //!
        //!   - Tab A loads Habit  (RowVersion = 0x0000000000000001)
        //!   - Tab B loads Habit  (RowVersion = 0x0000000000000001)
        //!   - Tab A saves        → WHERE Id=@id AND RowVersion=0x01 → 1 row, SQL bumps to 0x02 ✓
        //!   - Tab B saves        → WHERE Id=@id AND RowVersion=0x01 -> Not match with DB value: 0x02 → 0 rows → ConcurrencyConflict ✓
        //!
     */
    /*
    //?  SaveChangesAsync persists all staged EF changes first, then publishes domain domainEvents.
    //?  Events fire AFTER the DB write succeeds — handlers see a consistent database state.
    //
    //!  If publishing fails after a successful save, the DB write is already committed.
    //!  Keep domain event handlers idempotent to tolerate this.
    */
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        /*
            //! - Wrapped in try catch => To support Optimistic Concurrency! =>
            //!     - To Solve any Race Conditions that we could have in (Application Project)
            //? - So now my [ApplicationDbContext] which is my [UOF] will be handling the (Db Concurrency Exception) and throwing an exception: (DbConcurrencyProblemException) that is known to my (Application Project)
         */
        try
        {
            /*
                //!  - I Put [[SaveChangesAsync(...)]] before [[PublishDomainEventsAsync()]] Because:
                //!     - I don't see a meaning of making an Domain Event Call before the Event Really Occurs
                //!         - After User is Created/Added (To the DB) Successfully
                //!             ====>> I can say Fire the: [[UserCreatedDomainEvent]] Domain Event!
                //!  - العكس غير صحيح ==>> I can't say:  Fire the: [[UserCreatedDomainEvent]] before User is Added/Created Successfully by adding it to the database
             */
            int result = await base.SaveChangesAsync(cancellationToken);

            /*
             //! - In case of an Event Handler failure after the DB write is successful (Event Handler can: Call External Services, Send Emails, Make HTTP Requests, Or use our databases and create connections), but if [[DomainEvent]] Failed as above; it will be appear like the whole transaction failed, but in reality the DB write is already succeeded and committed and the failure is only in the Event Handler.
             //! - This is a Major Concern with this approach and it is something that I need to be aware of it
             */
            await PublishDomainEventsAsync();

            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            /*
                //!   - I Passed the [[ex]] instance as the (Inner Exception) so it can be available for Logging and further inspecting
                //!   - I Created an (Custom) [Exception] ==>> To not leak [EF Core] Details into my [Application Project]; so I abstracted EF Core behind this
                //* - I Created an (Custom) [Exception] and not returning [DbUpdateConcurrencyException] immediately! ==>> To not leak [EF Core] Details into my [Application Project]; so I abstracted EF Core behind this
            */
            throw new DbConcurrencyProblemException("Concurrency exception occurred.", ex);
        }
    }

    private async Task PublishDomainEventsAsync()
    {
        /*
            //?  IEntity (non-generic) is the key — ChangeTracker.Entries<T>() needs a concrete type.
            //?  AEntity<TId> is generic so it cannot be used here; IEntity is the correct hook.
            //>
            //>  .ToList() is critical: it forces full enumeration so every entity's domainEvents are
            //>  collected AND cleared BEFORE the first Publish() call. This prevents a handler
            //>  that triggers another SaveChanges from seeing stale domainEvents a second time.
        */
        var domainEvents = ChangeTracker
            .Entries<IEntity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                IReadOnlyList<IDomainEvent> domainEvents = entity.GetDomainEvents();

                /*
                 //! Clearing domainEvents here is ULTRA IMPORTANT!! because when we [[Publish()]] the [[DomainEvents]] because we don't know what could be happening in the handlers; their could be another DbContext is created which could use the same entities and if we don't clear the domainEvents, then those handlers could be triggered again and cause a loop or a string behavior. So we need to clear the domainEvents after we get them.
                 */
                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            //! [[Publish(..)]] is going to trigger the Events Handlers that are registered for the specific [[IDomainEvent]] type, which we defined in the [[Application Layer]].
            await _publisher.Publish(domainEvent);
        }
    }
}
