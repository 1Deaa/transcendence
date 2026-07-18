using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;
using Xunit;

namespace HrmSystem.Web.IntegrationTests;

/*
    //?     Boots the REAL Web host (full composition root: EF migrations, Quartz, SignalR,
    //?     health checks, JWT auth) against a throwaway SQL Server container.
    //
    //!     Environment is [Production] on purpose:
    //!      - config-only seeding (roles/permissions) — no multi-minute Bogus demo data.
    //!      - proves the release pipeline (HSTS, https redirect, no dev CORS) composes cleanly.
    //!     Redis points at a closed port with [abortConnect=false] — the cache degrades
    //!     gracefully by design (CacheService swallows connection failures).
*/
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync() => await _sqlContainer.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting(
            "ConnectionStrings:Database",
            _sqlContainer.GetConnectionString()
        );
        builder.UseSetting(
            "ConnectionStrings:RedisCache",
            "localhost:59999,abortConnect=false"
        );
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }
}
