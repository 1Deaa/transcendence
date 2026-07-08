namespace HrmSystem.Web.Api.Controllers.Employees;

//! Value-type members are [required] so model binding rejects a missing field instead of
//! silently defaulting it (Sonar S6964).
public sealed record HireEmployeeRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string JobTitle { get; init; }
    public required string DepartmentId { get; init; }
    public required DateOnly HiredOn { get; init; }
}
