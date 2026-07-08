using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Attendances.Enums;
using HrmSystem.Domain.Entities.Attendances.Events;
using HrmSystem.Domain.Entities.Attendances.ValueObjects;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Domain.Entities.Attendances;

/*
    //?     The Attendance aggregate root — one row per employee per working day. TENANT-OWNED.
    //?     Uniqueness (TenantId, EmployeeId, Date) is enforced by a database unique index;
    //?     the ClockIn handler translates that violation into AttendanceErrors.AlreadyClockedIn.
    //!     All timestamps are UTC; Date is the tenant's working day the record belongs to.
*/
public class Attendance : ATenantEntity<AttendanceId>
{
    #region Constants

    //? Clock-ins after WorkDayStart + LateThreshold are marked Late.
    public static readonly TimeOnly WorkDayStart = new(9, 0);
    public static readonly TimeSpan LateThreshold = TimeSpan.FromMinutes(15);

    #endregion

    #region Constructor

    private Attendance(
        AttendanceId id,
        EmployeeId employeeId,
        DateOnly date,
        DateTime? clockInAt,
        AttendanceStatus status
    )
        : base(id)
    {
        EmployeeId = employeeId;
        Date = date;
        ClockInAt = clockInAt;
        Status = status;
    }

    //! Private Parameterless Constructor: for EF Core materialisation only — never use in domain or application code.
    private Attendance()
        : base() { }

    #endregion

    #region Properties

    //? The working day this record belongs to.
    public DateOnly Date { get; private set; }

    //? UTC clock-in moment; null only for Absent records created by reconciliation.
    public DateTime? ClockInAt { get; private set; }

    //? UTC clock-out moment; null while the employee is still clocked in.
    public DateTime? ClockOutAt { get; private set; }

    public AttendanceStatus Status { get; private set; }

    //? Minutes between clock-in and clock-out; computed by ClockOut(), null until then.
    public int? WorkedMinutes { get; private set; }

    #endregion

    #region Static factories

    /*
        //?     Records a clock-in for the given working day.
        //?     Late/Present is decided here — the aggregate owns the lateness rule, not the handler:
        //?     late when the local clock-in time is after WorkDayStart + LateThreshold.
        //!     "Already clocked in today" is guarded by the unique (TenantId, EmployeeId, Date) index —
        //!     a second ClockIn for the same day fails at save time, not here.
    */
    public static Result<Attendance> ClockIn(
        EmployeeId employeeId,
        DateOnly date,
        DateTime clockInAtUtc
    )
    {
        var clockInTime = TimeOnly.FromDateTime(clockInAtUtc);
        AttendanceStatus status =
            clockInTime > WorkDayStart.Add(LateThreshold)
                ? AttendanceStatus.Late
                : AttendanceStatus.Present;

        var attendance = new Attendance(
            AttendanceId.New(),
            employeeId,
            date,
            clockInAtUtc,
            status
        );
        attendance.RaiseDomainEvent(
            new EmployeeClockedInDomainEvent(attendance.Id!, employeeId)
        );

        return attendance;
    }

    //? Records a full absence for the day (reconciliation jobs and seeding — never user-initiated).
    public static Result<Attendance> RecordAbsence(EmployeeId employeeId, DateOnly date)
    {
        return new Attendance(
            AttendanceId.New(),
            employeeId,
            date,
            clockInAt: null,
            AttendanceStatus.Absent
        );
    }

    #endregion

    #region Domain methods

    //? Records the clock-out and computes the worked duration.
    public Result ClockOut(DateTime clockOutAtUtc)
    {
        if (ClockInAt is null)
        {
            return AttendanceErrors.NotClockedIn;
        }

        if (ClockOutAt is not null)
        {
            return AttendanceErrors.AlreadyClockedOut;
        }

        if (clockOutAtUtc <= ClockInAt.Value)
        {
            return AttendanceErrors.ClockOutBeforeClockIn;
        }

        ClockOutAt = clockOutAtUtc;
        WorkedMinutes = (int)(clockOutAtUtc - ClockInAt.Value).TotalMinutes;
        RaiseDomainEvent(new EmployeeClockedOutDomainEvent(Id!, EmployeeId));

        return Result.Success();
    }

    #endregion

    #region Navigation & Relations

    //! Every Attendance row belongs to exactly one Employee. (1 Employee : M Attendances)
    public EmployeeId EmployeeId { get; private set; } = null!;
    public Employee Employee { get; private set; } = null!;

    #endregion
}
