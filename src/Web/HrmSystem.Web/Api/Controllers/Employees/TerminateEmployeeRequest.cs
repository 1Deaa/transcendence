namespace HrmSystem.Web.Api.Controllers.Employees;

//! Value-type members are [required] so model binding rejects a missing field instead of
//! silently defaulting it (Sonar S6964).
public sealed record TerminateEmployeeRequest
{
    public required DateOnly TerminatedOn { get; init; }
}
