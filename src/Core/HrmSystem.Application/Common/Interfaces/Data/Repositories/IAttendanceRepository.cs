using HrmSystem.Domain.Entities.Attendances;
using HrmSystem.Domain.Entities.Attendances.ValueObjects;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface IAttendanceRepository : IBaseRepository<Attendance, AttendanceId>
{
    //? The clock-in/clock-out anchor — at most one row exists per employee per working day.
    Task<Attendance?> FindByEmployeeAndDateAsync(
        EmployeeId employeeId,
        DateOnly date,
        CancellationToken ct
    );

    //? Per-employee attendance history for an inclusive date window.
    Task<IReadOnlyList<Attendance>> GetForEmployeeAsync(
        EmployeeId employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    );
}
