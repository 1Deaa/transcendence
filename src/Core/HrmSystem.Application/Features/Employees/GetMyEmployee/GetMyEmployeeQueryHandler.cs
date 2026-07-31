using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Employees.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Employees;

namespace HrmSystem.Application.Features.Employees.GetMyEmployee;

internal sealed class GetMyEmployeeQueryHandler(
    IEmployeeRepository employeeRepository,
    ICurrentUserContext currentUserContext
) : IQueryHandler<GetMyEmployeeQuery, EmployeeResponse>
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly ICurrentUserContext _currentUserContext = currentUserContext;

    public async Task<Result<EmployeeResponse>> Handle(
        GetMyEmployeeQuery query,
        CancellationToken cancellationToken
    )
    {
        string? email = _currentUserContext.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            return EmployeeErrors.NotFound;
        }

        //! The user↔employee link is the email: Employee.Email is stored lowercased
        //! and unique per tenant, and accounts are provisioned with the same address.
        Employee? employee = await _employeeRepository.FindByEmailAsync(
            email.ToLowerInvariant(),
            cancellationToken
        );
        if (employee is null)
        {
            return EmployeeErrors.NotFound;
        }

        return EmployeeResponse.FromEmployee(employee);
    }
}
