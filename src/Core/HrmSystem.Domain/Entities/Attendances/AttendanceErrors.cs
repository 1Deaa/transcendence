using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Attendances.ValueObjects;

namespace HrmSystem.Domain.Entities.Attendances;

/*
    //?     Error catalog for the Attendance aggregate.
    //>     Codes: "Attendance.<Reason>" — stable once shipped.
*/
public static class AttendanceErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Attendance.NotFound",
        "The attendance record was not found."
    );

    public static readonly Error AlreadyClockedIn = Error.Conflict(
        "Attendance.AlreadyClockedIn",
        "The employee has already clocked in for this working day."
    );

    public static readonly Error NotClockedIn = Error.Conflict(
        "Attendance.NotClockedIn",
        "Cannot clock out — there is no clock-in recorded for this working day."
    );

    public static readonly Error AlreadyClockedOut = Error.Conflict(
        "Attendance.AlreadyClockedOut",
        "The employee has already clocked out for this working day."
    );

    public static readonly Error ClockOutBeforeClockIn = Error.Validation(
        "Attendance.ClockOutBeforeClockIn",
        "The clock-out time must be after the clock-in time."
    );

    public static class Id
    {
        public static readonly Error Invalid = Error.Validation(
            "Attendance.Id.Invalid",
            "The attendance identifier must be a non-empty string."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "Attendance.Id.InvalidFormat",
            $"The attendance identifier must start with the '{AttendanceId.Prefix}' prefix."
        );
    }
}
