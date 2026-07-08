using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Departments.ValueObjects;
using HrmSystem.Domain.Entities.Employees;

namespace HrmSystem.Application.Features.Employees.HireEmployee;

internal sealed class HireEmployeeCommandHandler(
    IEmployeeRepository employeeRepository,
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<HireEmployeeCommand, string>
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<string>> Handle(
        HireEmployeeCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<DepartmentId> departmentIdResult = DepartmentId.From(command.DepartmentId);
        if (departmentIdResult.IsFailure)
        {
            return departmentIdResult.Errors.ToList();
        }

        //! The department must exist inside the caller's tenant (query filter scoped).
        Department? department = await _departmentRepository.GetByIdAsync(
            departmentIdResult.Value,
            cancellationToken
        );
        if (department is null)
        {
            return DepartmentErrors.NotFound;
        }

        Result<Employee> employeeResult = Employee.Hire(
            command.FirstName,
            command.LastName,
            command.Email,
            command.JobTitle,
            departmentIdResult.Value,
            command.HiredOn
        );
        if (employeeResult.IsFailure)
        {
            return employeeResult.Errors.ToList();
        }

        //! Uniqueness check uses the VALIDATED email (trimmed + lowercased by the VO).
        Employee employee = employeeResult.Value;
        Employee? existing = await _employeeRepository.FindByEmailAsync(
            employee.Email.Value,
            cancellationToken
        );
        if (existing is not null)
        {
            return EmployeeErrors.DuplicateEmail(employee.Email.Value);
        }

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return employee.Id!.Value;
    }
}
