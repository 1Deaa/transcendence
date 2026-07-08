using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Employees.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments.ValueObjects;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.Enums;

namespace HrmSystem.Application.Features.Employees.GetAllEmployees;

internal sealed class GetAllEmployeesQueryHandler(IEmployeeRepository employeeRepository)
    : IQueryHandler<GetAllEmployeesQuery, PaginationResult<EmployeeResponse>>
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;

    public async Task<Result<PaginationResult<EmployeeResponse>>> Handle(
        GetAllEmployeesQuery query,
        CancellationToken cancellationToken
    )
    {
        DepartmentId? departmentId = null;
        if (!string.IsNullOrWhiteSpace(query.DepartmentId))
        {
            Result<DepartmentId> departmentIdResult = DepartmentId.From(query.DepartmentId);
            if (departmentIdResult.IsFailure)
            {
                return departmentIdResult.Errors.ToList();
            }

            departmentId = departmentIdResult.Value;
        }

        EmployeeStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            //! Unknown status strings are a validation error, not a silent no-filter.
            if (
                !Enum.TryParse(query.Status, ignoreCase: true, out EmployeeStatus parsed)
                || parsed == EmployeeStatus.None
            )
            {
                return HrmSystem.Domain.Common.Result.Errors.Error.Validation(
                    "Employee.Status.Invalid",
                    $"'{query.Status}' is not a valid employee status."
                );
            }

            status = parsed;
        }

        (IReadOnlyList<Employee> items, int totalCount) = await _employeeRepository.GetPagedAsync(
            query.Page,
            query.PageSize,
            query.SearchTerm,
            departmentId,
            status,
            cancellationToken
        );

        var result = PaginationResult<EmployeeResponse>.Create(
            items.Select(EmployeeResponse.FromEmployee).ToList(),
            query.Page,
            query.PageSize,
            totalCount
        );

        return result;
    }
}
