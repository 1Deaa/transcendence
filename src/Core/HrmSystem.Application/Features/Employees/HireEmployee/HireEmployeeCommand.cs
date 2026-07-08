using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Employees.HireEmployee;

public sealed record HireEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    string DepartmentId,
    DateOnly HiredOn
) : ICommand<string>;
