using Bogus;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HrmSystem.Infrastructure.Persistence.Seeding.Seeders;

/*
    //*     Seeds ~50 Bogus employees per tenant (deterministic — fixed seed per tenant index).
    //*     Hire dates span the last two years; ~8% are terminated so the analytics
    //*     headcount trend has both directions from day one.
*/
internal sealed class EmployeeSeeder(
    ApplicationDbContext db,
    ITenantContext tenantContext,
    ILogger<EmployeeSeeder> logger
) : ISeeder
{
    //? Order: 50 — needs departments (40) for the FK.
    public int Order => 50;

    private const int EmployeesPerTenant = 50;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        List<Tenant> tenants = await db.Tenants.AsNoTracking().ToListAsync(cancellationToken);

        //! Anchor "today" once so reseeded environments stay comparable within a day.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (int tenantIndex = 0; tenantIndex < tenants.Count; tenantIndex++)
        {
            Tenant tenant = tenants[tenantIndex];
            using IDisposable scope = tenantContext.BeginScope(tenant.Id!);

            if (await db.Employees.AnyAsync(cancellationToken))
            {
                continue;
            }

            List<Department> departments = await db
                .Departments.AsNoTracking()
                .ToListAsync(cancellationToken);
            if (departments.Count == 0)
            {
                logger.LogWarning(
                    "EmployeeSeeder skipped for '{Slug}' — no departments.",
                    tenant.Slug.Value
                );
                continue;
            }

            //* Deterministic per tenant: seed 4200 + index → same people on every fresh seed.
            var faker = new Faker { Random = new Randomizer(4200 + tenantIndex) };
            string slug = tenant.Slug.Value;

            var employees = new List<Employee>(EmployeesPerTenant);

            /*
                //!     Employee records for the seeded LOGIN accounts, carrying the very same
                //!     email as the account. The user↔employee link is the email, so without
                //!     these rows GET /api/employees/me returns 404 for every demo account and
                //!     an Employee-role user cannot submit their own leave request.
                //?     Runs before the Bogus batch so these people exist even if faking fails.
            */
            List<User> tenantUsers = await db
                .Users.AsNoTracking()
                .Where(user => user.TenantId == tenant.Id)
                .ToListAsync(cancellationToken);

            foreach (User user in tenantUsers)
            {
                Result<Employee> accountEmployeeResult = Employee.Hire(
                    user.FirstName.Value,
                    user.LastName.Value,
                    user.Email.Value,
                    jobTitle: "Staff",
                    departmentId: departments[0].Id!,
                    hiredOn: today.AddDays(-365)
                );
                if (accountEmployeeResult.IsFailure)
                {
                    logger.LogWarning(
                        "Seed skipped — Employee.Hire failed for account '{Email}': {Errors}",
                        user.Email.Value,
                        string.Join(", ", accountEmployeeResult.Errors.Select(e => e.Description))
                    );
                    continue;
                }

                employees.Add(accountEmployeeResult.Value);
            }

            for (int i = 0; i < EmployeesPerTenant; i++)
            {
                string firstName = faker.Name.FirstName();
                string lastName = faker.Name.LastName();
                //? UniqueIndex-style suffix prevents duplicate-email collisions inside the tenant.
                string email = $"{firstName}.{lastName}.{i}@{slug}.example.com".ToLowerInvariant();
                string jobTitle = faker.Name.JobTitle();
                Department department = faker.PickRandom(departments);
                DateOnly hiredOn = today.AddDays(-faker.Random.Int(30, 730));

                Result<Employee> employeeResult = Employee.Hire(
                    firstName,
                    lastName,
                    email,
                    jobTitle,
                    department.Id!,
                    hiredOn
                );
                if (employeeResult.IsFailure)
                {
                    logger.LogWarning(
                        "Seed skipped — Employee.Hire failed for '{Email}': {Errors}",
                        email,
                        string.Join(", ", employeeResult.Errors.Select(e => e.Description))
                    );
                    continue;
                }

                Employee employee = employeeResult.Value;

                //? ~8% of staff already left — termination happens between hire date and today.
                if (faker.Random.Int(1, 100) <= 8)
                {
                    int daysEmployed = today.DayNumber - hiredOn.DayNumber;
                    DateOnly terminatedOn = hiredOn.AddDays(faker.Random.Int(30, Math.Max(31, daysEmployed)));
                    Result terminationResult = employee.Terminate(terminatedOn);
                    if (terminationResult.IsFailure)
                    {
                        logger.LogWarning(
                            "Employee '{Email}' could not be terminated during seeding.",
                            email
                        );
                    }
                }

                employees.Add(employee);
            }

            await db.Employees.AddRangeAsync(employees, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Seeded {Count} employees for tenant '{Slug}'.",
                employees.Count,
                slug
            );
        }
    }
}
