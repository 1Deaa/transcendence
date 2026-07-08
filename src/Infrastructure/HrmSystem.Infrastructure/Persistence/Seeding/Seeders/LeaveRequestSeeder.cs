using Bogus;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.Enums;
using HrmSystem.Domain.Entities.LeaveRequests;
using HrmSystem.Domain.Entities.LeaveRequests.Enums;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HrmSystem.Infrastructure.Persistence.Seeding.Seeders;

/*
    //*     Seeds ~40 leave requests per tenant across active employees:
    //*     mixed types, ~50% decided (approved/rejected via the domain methods so DecidedAt/By
    //*     are populated), the rest left Pending for the approvals dashboard.
*/
internal sealed class LeaveRequestSeeder(
    ApplicationDbContext db,
    ITenantContext tenantContext,
    ILogger<LeaveRequestSeeder> logger
) : ISeeder
{
    //? Order: 70 — needs employees (50).
    public int Order => 70;

    private const int LeaveRequestsPerTenant = 40;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        List<Tenant> tenants = await db.Tenants.AsNoTracking().ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (int tenantIndex = 0; tenantIndex < tenants.Count; tenantIndex++)
        {
            Tenant tenant = tenants[tenantIndex];
            using IDisposable scope = tenantContext.BeginScope(tenant.Id!);

            if (await db.LeaveRequests.AnyAsync(cancellationToken))
            {
                continue;
            }

            List<Employee> activeEmployees = await db
                .Employees.AsNoTracking()
                .Where(e => e.Status == EmployeeStatus.Active)
                .ToListAsync(cancellationToken);
            if (activeEmployees.Count == 0)
            {
                continue;
            }

            var faker = new Faker { Random = new Randomizer(7300 + tenantIndex) };
            var leaveRequests = new List<LeaveRequest>(LeaveRequestsPerTenant);

            for (int i = 0; i < LeaveRequestsPerTenant; i++)
            {
                Employee employee = faker.PickRandom(activeEmployees);
                //? Spread periods over ±60 days around today so charts show past and upcoming leave.
                DateOnly start = today.AddDays(faker.Random.Int(-60, 45));
                DateOnly end = start.AddDays(faker.Random.Int(0, 9));
                LeaveType type = faker.PickRandom(
                    LeaveType.Annual,
                    LeaveType.Sick,
                    LeaveType.Unpaid,
                    LeaveType.Other
                );

                Result<LeaveRequest> submitResult = LeaveRequest.Submit(
                    employee.Id!,
                    type,
                    start,
                    end,
                    faker.Lorem.Sentence(6)
                );
                if (submitResult.IsFailure)
                {
                    continue;
                }

                LeaveRequest leaveRequest = submitResult.Value;

                //? ~50% already decided — 70/30 approved vs rejected, decided shortly after start.
                if (faker.Random.Bool())
                {
                    DateTime decidedAt = DateTime.UtcNow.AddDays(-faker.Random.Int(0, 30));
                    Result decisionResult = faker.Random.Int(1, 100) <= 70
                        ? leaveRequest.Approve(decidedAt, "seed-manager")
                        : leaveRequest.Reject(decidedAt, "seed-manager");

                    if (decisionResult.IsFailure)
                    {
                        logger.LogWarning("Seeded leave request could not be decided.");
                    }
                }

                leaveRequests.Add(leaveRequest);
            }

            await db.LeaveRequests.AddRangeAsync(leaveRequests, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Seeded {Count} leave requests for tenant '{Slug}'.",
                leaveRequests.Count,
                tenant.Slug.Value
            );
        }
    }
}
