using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Departments.ValueObjects;

namespace HrmSystem.Application.Features.Departments.UpdateDepartment;

internal sealed class UpdateDepartmentCommandHandler(
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<UpdateDepartmentCommand>
{
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result> Handle(
        UpdateDepartmentCommand command,
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

        //! Another department (different id) holding the same code is a conflict.
        Department? sameCode = await _departmentRepository.FindByCodeAsync(
            command.Code.Trim().ToUpperInvariant(),
            cancellationToken
        );
        if (sameCode is not null && sameCode.Id != department.Id)
        {
            return DepartmentErrors.DuplicateCode(sameCode.Code.Value);
        }

        Result updateResult = department.Update(command.Name, command.Code);
        if (updateResult.IsFailure)
        {
            return updateResult.Errors.ToList();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
