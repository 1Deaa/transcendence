using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HrmSystem.Infrastructure.Persistence.Seeding;

/*
    //*     Orchestrates the full database startup sequence:
    //*       1. InitialiseAsync — apply pending EF Core migrations (Identity first, then Application).
    //*       2. SeedAsync       — run every ISeeder in Order sequence, respecting RunInProduction.
    //
    //!     Production guard: only seeders with [RunInProduction = true] run in Production.
    //!     Every ISeeder is idempotent: safe to call on every startup; skips when data already exists.
    //
    //>     To add a new seeder: create a class implementing ISeeder, register it as
    //>       services.AddScoped<ISeeder, YourSeeder>();
    //>     DatabaseInitialiser picks it up automatically — no other changes needed.
*/
public sealed class DatabaseInitialiser(
    ILogger<DatabaseInitialiser> logger,
    ApplicationDbContext db,
    ApplicationIdentityDbContext identityDb,
    IHostEnvironment env,
    IEnumerable<ISeeder> seeders
)
{
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /*
                //!     Identity must migrate FIRST — [ApplicationDbContext] creates [HrmSystem].[Users]
                //!     with a FK referencing [Identity].[IdentityUsers]. Migrating Application first would
                //!     cause SQL Server to reject the FK because the referenced table doesn't exist yet.
            */
            await identityDb.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Identity database migrated successfully.");

            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Application database migrated successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed.");
            throw;
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /*
                //*     In Production: only run seeders marked [RunInProduction = true] (e.g. roles/permissions).
                //*     In Development/Staging: run ALL seeders including test-data seeders.
                //!     Seeders are still ordered by [Order] within each tier.
            */
            IEnumerable<ISeeder> toRun = env.IsProduction()
                ? seeders.Where(s => s.RunInProduction)
                : seeders;

            foreach (ISeeder seeder in toRun.OrderBy(s => s.Order))
            {
                logger.LogInformation(
                    "Running seeder {Seeder} (order {Order})...",
                    seeder.GetType().Name,
                    seeder.Order
                );

                await seeder.SeedAsync(cancellationToken);
            }

            logger.LogInformation("All seeders completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seeding failed.");
            throw;
        }
    }
}
