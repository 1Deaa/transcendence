using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Employees.BulkHireEmployees;

namespace HrmSystem.Application.Features.Employees.BulkDeleteEmployees;

public sealed record BulkDeleteEmployeesCommand(IReadOnlyList<string> EmployeeIds)
    : ICommand<BulkOperationResult>;
