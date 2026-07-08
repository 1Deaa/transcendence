using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Departments.ValueObjects;

namespace HrmSystem.Application.Features.Departments.RemoveDepartment;

internal sealed class RemoveDepartmentCommandHandler(
    IDepartmentRepository departmentRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<RemoveDepartmentCommand>
{
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result> Handle(
        RemoveDepartmentCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<DepartmentId> idResult = DepartmentId.From(command.DepartmentId);
        if (idResult.IsFailure)
        {
            return idResult.Errors.ToList();
        }

        Department? department = await _departmentRepository.GetByIdAsync(
            idResult.Value,
            cancellationToken
        );
        if (department is null)
        {
            return DepartmentErrors.NotFound;
        }

        //! Guard: cannot remove a department while employees are still assigned to it.
        bool hasEmployees = await _employeeRepository.AnyInDepartmentAsync(
            idResult.Value,
            cancellationToken
        );
        if (hasEmployees)
        {
            return DepartmentErrors.HasEmployees;
        }

        //? Soft delete — the row stays for history; the query filter hides it everywhere.
        await _departmentRepository.DeleteEntityAsync(department);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
