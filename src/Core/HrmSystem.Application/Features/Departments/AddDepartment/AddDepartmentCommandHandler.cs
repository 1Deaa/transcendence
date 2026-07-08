using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;

namespace HrmSystem.Application.Features.Departments.AddDepartment;

internal sealed class AddDepartmentCommandHandler(
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<AddDepartmentCommand, string>
{
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<string>> Handle(
        AddDepartmentCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<Department> departmentResult = Department.Create(command.Name, command.Code);
        if (departmentResult.IsFailure)
        {
            return departmentResult.Errors.ToList();
        }

        //! Uniqueness check uses the VALIDATED code (trimmed + uppercased by the VO).
        Department department = departmentResult.Value;
        Department? existing = await _departmentRepository.FindByCodeAsync(
            department.Code.Value,
            cancellationToken
        );
        if (existing is not null)
        {
            return DepartmentErrors.DuplicateCode(department.Code.Value);
        }

        await _departmentRepository.AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return department.Id!.Value;
    }
}
