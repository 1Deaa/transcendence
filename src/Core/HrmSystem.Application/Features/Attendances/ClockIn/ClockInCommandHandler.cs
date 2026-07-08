using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Attendances;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.Enums;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Application.Features.Attendances.ClockIn;

internal sealed class ClockInCommandHandler(
    IAttendanceRepository attendanceRepository,
    IEmployeeRepository employeeRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork
) : ICommandHandler<ClockInCommand, string>
{
    private readonly IAttendanceRepository _attendanceRepository = attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<string>> Handle(
        ClockInCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<EmployeeId> employeeIdResult = EmployeeId.From(command.EmployeeId);
        if (employeeIdResult.IsFailure)
        {
            return employeeIdResult.Errors.ToList();
        }

        Employee? employee = await _employeeRepository.GetByIdAsync(
            employeeIdResult.Value,
            cancellationToken
        );
        if (employee is null)
        {
            return EmployeeErrors.NotFound;
        }

        if (employee.Status != EmployeeStatus.Active)
        {
            return EmployeeErrors.NotActive;
        }

        DateTime utcNow = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);

        /*
            //?     Fast-path duplicate check; the unique (TenantId, EmployeeId, Date) index is
            //?     the authoritative backstop for two concurrent clock-ins in the same instant.
        */
        Attendance? existing = await _attendanceRepository.FindByEmployeeAndDateAsync(
            employeeIdResult.Value,
            today,
            cancellationToken
        );
        if (existing is not null)
        {
            return AttendanceErrors.AlreadyClockedIn;
        }

        Result<Attendance> attendanceResult = Attendance.ClockIn(
            employeeIdResult.Value,
            today,
            utcNow
        );
        if (attendanceResult.IsFailure)
        {
            return attendanceResult.Errors.ToList();
        }

        await _attendanceRepository.AddAsync(attendanceResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return attendanceResult.Value.Id!.Value;
    }
}
