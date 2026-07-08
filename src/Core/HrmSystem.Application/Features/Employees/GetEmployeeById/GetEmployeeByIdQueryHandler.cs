using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Employees.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Application.Features.Employees.GetEmployeeById;

internal sealed class GetEmployeeByIdQueryHandler(IEmployeeRepository employeeRepository)
    : IQueryHandler<GetEmployeeByIdQuery, EmployeeResponse>
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;

    public async Task<Result<EmployeeResponse>> Handle(
        GetEmployeeByIdQuery query,
        CancellationToken cancellationToken
    )
    {
        Result<EmployeeId> employeeIdResult = EmployeeId.From(query.EmployeeId);
        if (employeeIdResult.IsFailure)
        {
            return employeeIdResult.Errors.ToList();
        }

        Employee? employee = await _employeeRepository.GetByIdAsync(
            employeeIdResult.Value,
            cancellationToken
        );
        if (employee is null)
        {
            return EmployeeErrors.NotFound;
        }

        return EmployeeResponse.FromEmployee(employee);
    }
}
