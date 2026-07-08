namespace HrmSystem.Domain.Entities.LeaveRequests.Enums;

public enum LeaveType
{
    //! Sentinel — never persist; used to catch uninitialized values in validation.
    None = 0,

    Annual = 1,
    Sick = 2,
    Unpaid = 3,
    Maternity = 4,
    Other = 5,
}
