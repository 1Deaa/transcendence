using Bogus;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Attendances;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.Enums;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HrmSystem.Infrastructure.Persistence.Seeding.Seeders;

/*
    //*     Seeds ~60 weekdays of attendance history for every ACTIVE employee:
    //*      ~90% attendance (clock-in 08:30–09:45 → the >09:15 tail lands as Late via the
    //*      domain rule) with an 8–9.5h shift, ~10% recorded absences.
    //*     This is the raw material for the analytics attendance-trend chart.
    //
    //!     Domain events raised by ClockIn/ClockOut publish after each tenant's save —
    //!     harmless while no analytics handlers exist, and correct once they do.
*/
internal sealed class AttendanceSeeder(
    ApplicationDbContext db,
    ITenantContext tenantContext,
    ILogger<AttendanceSeeder> logger
) : ISeeder
{
    //? Order: 60 — needs employees (50).
    public int Order => 60;

    private const int WorkingDaysToSeed = 60;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        List<Tenant> tenants = await db.Tenants.AsNoTracking().ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (int tenantIndex = 0; tenantIndex < tenants.Count; tenantIndex++)
        {
            Tenant tenant = tenants[tenantIndex];
            using IDisposable scope = tenantContext.BeginScope(tenant.Id!);

            if (await db.Attendances.AnyAsync(cancellationToken))
            {
                continue;
            }

            List<Employee> activeEmployees = await db
                .Employees.AsNoTracking()
                .Where(e => e.Status == EmployeeStatus.Active)
                .ToListAsync(cancellationToken);

            var faker = new Faker { Random = new Randomizer(6100 + tenantIndex) };
            var attendances = new List<Attendance>();

            //? Walk backwards from yesterday collecting the last N weekdays.
            DateOnly date = today.AddDays(-1);
            int weekdaysCollected = 0;

            while (weekdaysCollected < WorkingDaysToSeed)
            {
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    date = date.AddDays(-1);
                    continue;
                }

                foreach (Employee employee in activeEmployees)
                {
                    //! Skip days before the employee was hired.
                    if (date < employee.HiredOn)
                    {
                        continue;
                    }

                    if (faker.Random.Int(1, 100) <= 10)
                    {
                        Result<Attendance> absenceResult = Attendance.RecordAbsence(
                            employee.Id!,
                            date
                        );
                        if (absenceResult.IsSuccess)
                        {
                            attendances.Add(absenceResult.Value);
                        }

                        continue;
                    }

                    //? 08:30–09:45 window — the domain marks everything after 09:15 as Late.
                    var clockInTime = new TimeOnly(
                        faker.Random.Int(8, 9),
                        faker.Random.Int(0, 59)
                    );
                    if (clockInTime.Hour == 9 && clockInTime.Minute > 45)
                    {
                        clockInTime = new TimeOnly(9, 45);
                    }
                    if (clockInTime.Hour == 8 && clockInTime.Minute < 30)
                    {
                        clockInTime = new TimeOnly(8, clockInTime.Minute + 30);
                    }

                    var clockInAt = date.ToDateTime(clockInTime, DateTimeKind.Utc);

                    Result<Attendance> clockInResult = Attendance.ClockIn(
                        employee.Id!,
                        date,
                        clockInAt
                    );
                    if (clockInResult.IsFailure)
                    {
                        continue;
                    }

                    Attendance attendance = clockInResult.Value;

                    DateTime clockOutAt = clockInAt.AddMinutes(faker.Random.Int(480, 570));
                    Result clockOutResult = attendance.ClockOut(clockOutAt);
                    if (clockOutResult.IsFailure)
                    {
                        logger.LogWarning(
                            "Seeded attendance for {Date} could not be clocked out.",
                            date
                        );
                    }

                    attendances.Add(attendance);
                }

                weekdaysCollected++;
                date = date.AddDays(-1);
            }

            await db.Attendances.AddRangeAsync(attendances, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Seeded {Count} attendance rows for tenant '{Slug}'.",
                attendances.Count,
                tenant.Slug.Value
            );
        }
    }
}
