namespace HrmSystem.Domain.Entities.Attendances.Enums;

public enum AttendanceStatus
{
    //! Sentinel — never persist; used to catch uninitialized values in validation.
    None = 0,

    //? Clocked in at or before the late threshold.
    Present = 1,

    //? Clocked in after the late threshold (Attendance.LateThreshold).
    Late = 2,

    //? No clock-in for the working day (created by reconciliation/seeding, not by ClockIn).
    Absent = 3,
}
