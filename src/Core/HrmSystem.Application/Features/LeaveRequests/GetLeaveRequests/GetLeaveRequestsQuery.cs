using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.LeaveRequests.Shared;

namespace HrmSystem.Application.Features.LeaveRequests.GetLeaveRequests;

public sealed record GetLeaveRequestsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? EmployeeId = null
) : IQuery<PaginationResult<LeaveRequestResponse>>;
