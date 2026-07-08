using HrmSystem.Domain.Entities.Employees;

namespace HrmSystem.Application.Features.Employees.Shared;

//? Flat read model returned by every employee endpoint — primitives only, no VOs leak out.
public sealed record EmployeeResponse(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    string DepartmentId,
    string Status,
    DateOnly HiredOn,
    DateOnly? TerminatedOn
)
{
    public static EmployeeResponse FromEmployee(Employee employee) =>
        new(
            employee.Id!.Value,
            employee.Name.FirstName,
            employee.Name.LastName,
            employee.Email.Value,
            employee.JobTitle.Value,
            employee.DepartmentId.Value,
            employee.Status.ToString(),
            employee.HiredOn,
            employee.TerminatedOn
        );
}
