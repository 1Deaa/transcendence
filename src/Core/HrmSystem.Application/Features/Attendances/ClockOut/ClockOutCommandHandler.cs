using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Attendances;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Application.Features.Attendances.ClockOut;

internal sealed class ClockOutCommandHandler(
    IAttendanceRepository attendanceRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork
) : ICommandHandler<ClockOutCommand>
{
    private readonly IAttendanceRepository _attendanceRepository = attendanceRepository;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result> Handle(ClockOutCommand command, CancellationToken cancellationToken)
    {
        Result<EmployeeId> employeeIdResult = EmployeeId.From(command.EmployeeId);
        if (employeeIdResult.IsFailure)
        {
            return employeeIdResult.Errors.ToList();
        }

        DateTime utcNow = _dateTimeProvider.UtcNow;

        Attendance? attendance = await _attendanceRepository.FindByEmployeeAndDateAsync(
            employeeIdResult.Value,
            DateOnly.FromDateTime(utcNow),
            cancellationToken
        );
        if (attendance is null)
        {
            return AttendanceErrors.NotClockedIn;
        }

        Result clockOutResult = attendance.ClockOut(utcNow);
        if (clockOutResult.IsFailure)
        {
            return clockOutResult.Errors.ToList();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
