namespace HrmSystem.Web.Api.Controllers.Employees;

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    string DepartmentId
);
