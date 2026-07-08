namespace HrmSystem.Domain.Entities.LeaveRequests.Enums;

public enum LeaveStatus
{
    //! Sentinel — never persist; used to catch uninitialized values in validation.
    None = 0,

    //? Submitted, awaiting a manager decision.
    Pending = 1,

    Approved = 2,
    Rejected = 3,

    //? Withdrawn by the requester while still Pending.
    Cancelled = 4,
}
