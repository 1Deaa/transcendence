using HrmSystem.Domain.Entities.LeaveRequests.Enums;

namespace HrmSystem.Web.Api.Controllers.LeaveRequests;

//! Value-type members are [required] so model binding rejects a missing field instead of
//! silently defaulting it (Sonar S6964).
public sealed record SubmitLeaveRequestRequest
{
    public required string EmployeeId { get; init; }
    public required LeaveType Type { get; init; }
    public required DateOnly Start { get; init; }
    public required DateOnly End { get; init; }
    public required string Reason { get; init; }
}
