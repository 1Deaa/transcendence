using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Departments.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Departments.ValueObjects;

namespace HrmSystem.Application.Features.Departments.GetDepartmentById;

internal sealed class GetDepartmentByIdQueryHandler(IDepartmentRepository departmentRepository)
    : IQueryHandler<GetDepartmentByIdQuery, DepartmentResponse>
{
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    public async Task<Result<DepartmentResponse>> Handle(
        GetDepartmentByIdQuery query,
        CancellationToken cancellationToken
    )
    {
        Result<DepartmentId> idResult = DepartmentId.From(query.DepartmentId);
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

        return DepartmentResponse.FromDepartment(department);
    }
}
