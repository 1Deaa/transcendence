using HrmSystem.Domain.Entities.Departments;

namespace HrmSystem.Application.Features.Departments.Shared;

//? Flat read model returned by every department endpoint — primitives only, no VOs leak out.
public sealed record DepartmentResponse(
    string Id,
    string Name,
    string Code,
    bool IsActive,
    DateTime CreatedAt
)
{
    public static DepartmentResponse FromDepartment(Department department) =>
        new(
            department.Id!.Value,
            department.Name.Value,
            department.Code.Value,
            department.IsActive,
            department.CreatedAt
        );
}
