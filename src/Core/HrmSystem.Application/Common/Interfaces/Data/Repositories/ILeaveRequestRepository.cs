using HrmSystem.Domain.Entities.Employees.ValueObjects;
using HrmSystem.Domain.Entities.LeaveRequests;
using HrmSystem.Domain.Entities.LeaveRequests.Enums;
using HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface ILeaveRequestRepository : IBaseRepository<LeaveRequest, LeaveRequestId>
{
    //? Overlap guard — an employee cannot hold two Pending/Approved leaves for intersecting days.
    Task<bool> HasOverlappingAsync(
        EmployeeId employeeId,
        DateOnly start,
        DateOnly end,
        CancellationToken ct
    );

    /*
        //?     Offset-paged leave listing with optional filters (DevHabit pagination pattern).
        //?     Returned as (Items, TotalCount) so the handler builds the PaginationResult.
    */
    Task<(IReadOnlyList<LeaveRequest> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        LeaveStatus? status,
        EmployeeId? employeeId,
        CancellationToken ct
    );
}
