using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HrmSystem.Infrastructure.Persistence.Seeding.Seeders;

/*
    //*     Seeds the three demo company workspaces every downstream seeder hangs data on.
    //*     Fixed slugs (acme / globex / initech) so dev URLs, docs and tests are stable.
    //!     Tenants are HOST-LEVEL — no tenant scope is opened here.
*/
internal sealed class TenantSeeder(ApplicationDbContext db, ILogger<TenantSeeder> logger)
    : ISeeder
{
    //? Order: 8 — after roles (5), before users (10) so accounts can be tenant-assigned.
    public int Order => 8;

    internal static readonly (string CompanyName, string Slug)[] DemoTenants =
    [
        ("Acme Corporation", "acme"),
        ("Globex International", "globex"),
        ("Initech Solutions", "initech"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Tenants.AnyAsync(cancellationToken))
        {
            return;
        }

        var tenants = new List<Tenant>(DemoTenants.Length);

        foreach ((string companyName, string slug) in DemoTenants)
        {
            Result<Tenant> tenantResult = Tenant.Create(companyName, slug);
            if (tenantResult.IsFailure)
            {
                logger.LogWarning(
                    "Seed skipped — Tenant.Create failed for '{Slug}': {Errors}",
                    slug,
                    string.Join(", ", tenantResult.Errors.Select(e => e.Description))
                );
                continue;
            }

            //? Demo workspaces are fully enabled — Trial status would hide billing-gated features.
            Result activationResult = tenantResult.Value.ActivateSubscription();
            if (activationResult.IsFailure)
            {
                logger.LogWarning(
                    "Tenant '{Slug}' could not be activated: {Errors}",
                    slug,
                    string.Join(", ", activationResult.Errors.Select(e => e.Description))
                );
            }

            tenants.Add(tenantResult.Value);
        }

        await db.Tenants.AddRangeAsync(tenants, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} tenants.", tenants.Count);
    }
}
