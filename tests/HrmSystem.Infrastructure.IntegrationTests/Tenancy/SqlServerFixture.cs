using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using HrmSystem.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace HrmSystem.Infrastructure.IntegrationTests.Tenancy;

/*
    //?     One real SQL Server (Testcontainers) per test class: migrations for BOTH contexts
    //?     run once (Identity first — same order as the app's DatabaseInitialiser), then each
    //?     test builds ApplicationDbContext instances over the same database with a
    //?     controllable fake tenant context.
    //!     Requires a running Docker daemon — same prerequisite as `docker compose up`.
*/
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public FakeTenantContext TenantContext { get; } = new();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        DbContextOptions<ApplicationIdentityDbContext> identityOptions =
            new DbContextOptionsBuilder<ApplicationIdentityDbContext>()
                .UseSqlServer(_container.GetConnectionString())
                .Options;
        await using (var identityContext = new ApplicationIdentityDbContext(identityOptions))
        {
            await identityContext.Database.MigrateAsync();
        }

        await using ApplicationDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ApplicationDbContext CreateContext()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(_container.GetConnectionString())
                .AddInterceptors(
                    new TenantWriteGuardInterceptor(TenantContext),
                    new AuditableEntityInterceptor(new FakeCurrentUserContext(), new FakeClock())
                )
                .Options;

        return new ApplicationDbContext(options, new NoOpPublisher(), TenantContext);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /*
        //?     Minimal ITenantContext for tests: Current is directly settable — no HTTP
        //?     context, no AsyncLocal ceremony needed to simulate "who is the tenant now".
    */
    public sealed class FakeTenantContext : ITenantContext
    {
        public TenantId? Current { get; set; }

        public IDisposable BeginScope(TenantId tenantId)
        {
            TenantId? previous = Current;
            Current = tenantId;
            return new ScopeRestorer(this, previous);
        }

        private sealed class ScopeRestorer(FakeTenantContext owner, TenantId? previous)
            : IDisposable
        {
            public void Dispose() => owner.Current = previous;
        }
    }

    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default
        )
            where TNotification : INotification => Task.CompletedTask;
    }

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated => false;

        public UserId? DomainUserId => null;

        public string? IdentityId => null;

        public string? Email => null;

        public string? UserName => null;

        public string? PhoneNumber => null;
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
