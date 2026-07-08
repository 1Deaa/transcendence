using HrmSystem.Domain.Entities.Employees;

namespace HrmSystem.Application.Features.Employees.ExportEmployees;

/*
    //?     Flat export row for the multi-format export endpoint (JSON / XML / CSV).
    //!     A mutable POCO CLASS on purpose — XmlSerializer requires a parameterless
    //!     constructor and settable properties; records with positional params fail it.
*/
public sealed class EmployeeExportRow
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string HiredOn { get; set; } = string.Empty;
    public string? TerminatedOn { get; set; }

    public static EmployeeExportRow FromEmployee(Employee employee) =>
        new()
        {
            Id = employee.Id!.Value,
            FirstName = employee.Name.FirstName,
            LastName = employee.Name.LastName,
            Email = employee.Email.Value,
            JobTitle = employee.JobTitle.Value,
            DepartmentId = employee.DepartmentId.Value,
            Status = employee.Status.ToString(),
            HiredOn = employee.HiredOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            TerminatedOn = employee.TerminatedOn?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        };
}
