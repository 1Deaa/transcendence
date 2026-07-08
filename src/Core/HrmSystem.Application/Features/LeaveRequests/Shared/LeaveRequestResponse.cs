using HrmSystem.Domain.Entities.LeaveRequests;

namespace HrmSystem.Application.Features.LeaveRequests.Shared;

//? Flat read model returned by every leave-request endpoint.
public sealed record LeaveRequestResponse(
    string Id,
    string EmployeeId,
    string Type,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int TotalDays,
    string Reason,
    string Status,
    DateTime? DecidedAt,
    string? DecidedBy
)
{
    public static LeaveRequestResponse FromLeaveRequest(LeaveRequest leaveRequest) =>
        new(
            leaveRequest.Id!.Value,
            leaveRequest.EmployeeId.Value,
            leaveRequest.Type.ToString(),
            leaveRequest.Period.Start,
            leaveRequest.Period.End,
            leaveRequest.Period.TotalDays,
            leaveRequest.Reason.Value,
            leaveRequest.Status.ToString(),
            leaveRequest.DecidedAt,
            leaveRequest.DecidedBy
        );
}
