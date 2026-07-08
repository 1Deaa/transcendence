using HrmSystem.Domain.Entities.Attendances;

namespace HrmSystem.Application.Features.Attendances.Shared;

//? Flat read model returned by every attendance endpoint.
public sealed record AttendanceResponse(
    string Id,
    string EmployeeId,
    DateOnly Date,
    DateTime? ClockInAt,
    DateTime? ClockOutAt,
    string Status,
    int? WorkedMinutes
)
{
    public static AttendanceResponse FromAttendance(Attendance attendance) =>
        new(
            attendance.Id!.Value,
            attendance.EmployeeId.Value,
            attendance.Date,
            attendance.ClockInAt,
            attendance.ClockOutAt,
            attendance.Status.ToString(),
            attendance.WorkedMinutes
        );
}
