using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HrmSystem.Infrastructure.Persistence.Seeding.Seeders;

/*
    //*     Seeds four fixed departments into EVERY demo tenant.
    //!     Each tenant's inserts run inside tenantContext.BeginScope(...) so the
    //!     TenantWriteGuardInterceptor stamps TenantId — exactly like a background job would.
*/
internal sealed class DepartmentSeeder(
    ApplicationDbContext db,
    ITenantContext tenantContext,
    ILogger<DepartmentSeeder> logger
) : ISeeder
{
    //? Order: 40 — after users (10); first of the tenant-scoped data seeders.
    public int Order => 40;

    internal static readonly (string Name, string Code)[] DemoDepartments =
    [
        ("Engineering", "ENG"),
        ("Human Resources", "HR"),
        ("Finance", "FIN"),
        ("Sales", "SLS"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        List<Tenant> tenants = await db.Tenants.AsNoTracking().ToListAsync(cancellationToken);

        foreach (Tenant tenant in tenants)
        {
            using IDisposable scope = tenantContext.BeginScope(tenant.Id!);

            //? The tenant query filter scopes this existence check to the current tenant.
            if (await db.Departments.AnyAsync(cancellationToken))
            {
                continue;
            }

            var departments = new List<Department>(DemoDepartments.Length);

            foreach ((string name, string code) in DemoDepartments)
            {
                Result<Department> departmentResult = Department.Create(name, code);
                if (departmentResult.IsFailure)
                {
                    logger.LogWarning(
                        "Seed skipped — Department.Create failed for '{Code}': {Errors}",
                        code,
                        string.Join(", ", departmentResult.Errors.Select(e => e.Description))
                    );
                    continue;
                }

                departments.Add(departmentResult.Value);
            }

            await db.Departments.AddRangeAsync(departments, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Seeded {Count} departments for tenant '{Slug}'.",
                departments.Count,
                tenant.Slug.Value
            );
        }
    }
}
