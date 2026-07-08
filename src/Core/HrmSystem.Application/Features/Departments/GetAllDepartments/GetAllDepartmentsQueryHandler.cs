using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Departments.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;

namespace HrmSystem.Application.Features.Departments.GetAllDepartments;

internal sealed class GetAllDepartmentsQueryHandler(IDepartmentRepository departmentRepository)
    : IQueryHandler<GetAllDepartmentsQuery, IReadOnlyList<DepartmentResponse>>
{
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    public async Task<Result<IReadOnlyList<DepartmentResponse>>> Handle(
        GetAllDepartmentsQuery query,
        CancellationToken cancellationToken
    )
    {
        //? Departments per tenant are few (tens, not thousands) — no pagination needed here.
        IEnumerable<Department> departments = await _departmentRepository.GetAllAsync(
            cancellationToken
        );

        IReadOnlyList<DepartmentResponse> responses = departments
            .Select(DepartmentResponse.FromDepartment)
            .ToList();

        return Result.Success(responses);
    }
}
