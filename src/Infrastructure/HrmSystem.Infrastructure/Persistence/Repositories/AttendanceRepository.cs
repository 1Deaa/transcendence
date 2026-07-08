using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Entities.Attendances;
using HrmSystem.Domain.Entities.Attendances.ValueObjects;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class AttendanceRepository
    : ABaseRepository<Attendance, AttendanceId>, IAttendanceRepository
{
    public AttendanceRepository(ApplicationDbContext dbContext)
        : base(dbContext) { }

    public async Task<Attendance?> FindByEmployeeAndDateAsync(
        EmployeeId employeeId,
        DateOnly date,
        CancellationToken ct
    )
    {
        //* Served by the unique IX_Attendances_TenantId_EmployeeId_Date index — always a seek.
        return await DbContext
            .Set<Attendance>()
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == date, ct);
    }

    public async Task<IReadOnlyList<Attendance>> GetForEmployeeAsync(
        EmployeeId employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    )
    {
        return await DbContext
            .Set<Attendance>()
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Date >= from && a.Date <= to)
            .OrderByDescending(a => a.Date)
            .ToListAsync(ct);
    }
}
