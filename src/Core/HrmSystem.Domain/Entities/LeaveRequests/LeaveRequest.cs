using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using HrmSystem.Domain.Entities.LeaveRequests.Enums;
using HrmSystem.Domain.Entities.LeaveRequests.Events;
using HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;

namespace HrmSystem.Domain.Entities.LeaveRequests;

/*
    //?     The LeaveRequest aggregate root — TENANT-OWNED.
    //?     State machine: Pending → Approved | Rejected | Cancelled (all decisions are terminal).
    //?     Every transition validates the current state and raises a decision event so
    //?     notifications and analytics react after the save commits.
*/
public class LeaveRequest : ATenantEntity<LeaveRequestId>
{
    #region Constructor

    private LeaveRequest(
        LeaveRequestId id,
        EmployeeId employeeId,
        LeaveType type,
        DateRange period,
        LeaveReason reason
    )
        : base(id)
    {
        EmployeeId = employeeId;
        Type = type;
        Period = period;
        Reason = reason;
        Status = LeaveStatus.Pending;
    }

    //! Private Parameterless Constructor: for EF Core materialisation only — never use in domain or application code.
    private LeaveRequest()
        : base() { }

    #endregion

    #region Properties

    public LeaveType Type { get; private set; }

    //? Inclusive start–end day range, max DateRange.MaxDays days.
    public DateRange Period { get; private set; } = null!;

    public LeaveReason Reason { get; private set; } = null!;

    public LeaveStatus Status { get; private set; }

    //? UTC moment the manager decided (approved/rejected); null while Pending/Cancelled.
    public DateTime? DecidedAt { get; private set; }

    //? Who decided — the deciding user's domain UserId string; null while Pending/Cancelled.
    public string? DecidedBy { get; private set; }

    #endregion

    #region Static factory

    //? The only way to submit a valid LeaveRequest — collects every broken rule before returning.
    public static Result<LeaveRequest> Submit(
        EmployeeId employeeId,
        LeaveType type,
        DateOnly start,
        DateOnly end,
        string reason
    )
    {
        var errors = new List<Error>();

        if (type == LeaveType.None)
        {
            errors.Add(LeaveRequestErrors.InvalidLeaveType);
        }

        Result<DateRange> periodResult = DateRange.Create(start, end);
        if (periodResult.IsFailure)
        {
            errors.AddRange(periodResult.Errors);
        }

        Result<LeaveReason> reasonResult = LeaveReason.Create(reason);
        if (reasonResult.IsFailure)
        {
            errors.AddRange(reasonResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var leaveRequest = new LeaveRequest(
            LeaveRequestId.New(),
            employeeId,
            type,
            periodResult.Value,
            reasonResult.Value
        );
        leaveRequest.RaiseDomainEvent(
            new LeaveRequestSubmittedDomainEvent(leaveRequest.Id!, employeeId)
        );

        return leaveRequest;
    }

    #endregion

    #region Domain methods

    //? Manager approval — only a Pending request can be approved.
    public Result Approve(DateTime decidedAtUtc, string decidedBy)
    {
        if (Status != LeaveStatus.Pending)
        {
            return LeaveRequestErrors.AlreadyDecided;
        }

        Status = LeaveStatus.Approved;
        DecidedAt = decidedAtUtc;
        DecidedBy = decidedBy;
        RaiseDomainEvent(new LeaveRequestDecidedDomainEvent(Id!, EmployeeId, Status));

        return Result.Success();
    }

    //? Manager rejection — only a Pending request can be rejected.
    public Result Reject(DateTime decidedAtUtc, string decidedBy)
    {
        if (Status != LeaveStatus.Pending)
        {
            return LeaveRequestErrors.AlreadyDecided;
        }

        Status = LeaveStatus.Rejected;
        DecidedAt = decidedAtUtc;
        DecidedBy = decidedBy;
        RaiseDomainEvent(new LeaveRequestDecidedDomainEvent(Id!, EmployeeId, Status));

        return Result.Success();
    }

    //? Requester withdrawal — only allowed while still Pending.
    public Result Cancel()
    {
        if (Status != LeaveStatus.Pending)
        {
            return LeaveRequestErrors.AlreadyDecided;
        }

        Status = LeaveStatus.Cancelled;
        RaiseDomainEvent(new LeaveRequestDecidedDomainEvent(Id!, EmployeeId, Status));

        return Result.Success();
    }

    #endregion

    #region Navigation & Relations

    //! Every LeaveRequest belongs to exactly one Employee. (1 Employee : M LeaveRequests)
    public EmployeeId EmployeeId { get; private set; } = null!;
    public Employee Employee { get; private set; } = null!;

    #endregion
}
