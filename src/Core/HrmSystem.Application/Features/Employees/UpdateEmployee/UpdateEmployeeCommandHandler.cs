using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Departments.ValueObjects;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Application.Features.Employees.UpdateEmployee;

internal sealed class UpdateEmployeeCommandHandler(
    IEmployeeRepository employeeRepository,
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<UpdateEmployeeCommand>
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result> Handle(
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<EmployeeId> employeeIdResult = EmployeeId.From(command.EmployeeId);
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

        //! Another employee (different id) holding the same email is a conflict.
        Employee? sameEmail = await _employeeRepository.FindByEmailAsync(
            command.Email.Trim().ToLowerInvariant(),
            cancellationToken
        );
        if (sameEmail is not null && sameEmail.Id != employee.Id)
        {
            return EmployeeErrors.DuplicateEmail(sameEmail.Email.Value);
        }

        Result profileResult = employee.UpdateProfile(
            command.FirstName,
            command.LastName,
            command.Email,
            command.JobTitle
        );
        if (profileResult.IsFailure)
        {
            return profileResult.Errors.ToList();
        }

        if (employee.DepartmentId.Value != command.DepartmentId)
        {
            Result<DepartmentId> departmentIdResult = DepartmentId.From(command.DepartmentId);
            if (departmentIdResult.IsFailure)
            {
                return departmentIdResult.Errors.ToList();
            }

            Department? department = await _departmentRepository.GetByIdAsync(
                departmentIdResult.Value,
                cancellationToken
            );
            if (department is null)
            {
                return DepartmentErrors.NotFound;
            }

            Result moveResult = employee.ChangeDepartment(departmentIdResult.Value);
            if (moveResult.IsFailure)
            {
                return moveResult.Errors.ToList();
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
