using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Attendances.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Attendances;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Application.Features.Attendances.GetAttendanceForEmployee;

internal sealed class GetAttendanceForEmployeeQueryHandler(
    IAttendanceRepository attendanceRepository
) : IQueryHandler<GetAttendanceForEmployeeQuery, IReadOnlyList<AttendanceResponse>>
{
    private readonly IAttendanceRepository _attendanceRepository = attendanceRepository;

    public async Task<Result<IReadOnlyList<AttendanceResponse>>> Handle(
        GetAttendanceForEmployeeQuery query,
        CancellationToken cancellationToken
    )
    {
        Result<EmployeeId> employeeIdResult = EmployeeId.From(query.EmployeeId);
        if (employeeIdResult.IsFailure)
        {
            return employeeIdResult.Errors.ToList();
        }

        IReadOnlyList<Attendance> attendances = await _attendanceRepository.GetForEmployeeAsync(
            employeeIdResult.Value,
            query.From,
            query.To,
            cancellationToken
        );

        IReadOnlyList<AttendanceResponse> responses = attendances
            .Select(AttendanceResponse.FromAttendance)
            .ToList();

        return Result.Success(responses);
    }
}
