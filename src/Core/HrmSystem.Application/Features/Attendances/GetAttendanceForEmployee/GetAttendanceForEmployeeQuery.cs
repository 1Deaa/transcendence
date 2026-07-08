using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Attendances.Shared;

namespace HrmSystem.Application.Features.Attendances.GetAttendanceForEmployee;

public sealed record GetAttendanceForEmployeeQuery(string EmployeeId, DateOnly From, DateOnly To)
    : IQuery<IReadOnlyList<AttendanceResponse>>;
