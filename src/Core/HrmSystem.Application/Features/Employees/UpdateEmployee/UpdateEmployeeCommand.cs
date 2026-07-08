using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Employees.UpdateEmployee;

public sealed record UpdateEmployeeCommand(
    string EmployeeId,
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    string DepartmentId
) : ICommand;
