using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Employees.Shared;

namespace HrmSystem.Application.Features.Employees.GetAllEmployees;

public sealed record GetAllEmployeesQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? DepartmentId = null,
    string? Status = null
) : IQuery<PaginationResult<EmployeeResponse>>;
