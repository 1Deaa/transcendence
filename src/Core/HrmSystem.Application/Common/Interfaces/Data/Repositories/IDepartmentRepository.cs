using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Departments.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface IDepartmentRepository : IBaseRepository<Department, DepartmentId>
{
    //? Duplicate-code guard for create/update — scoped to the current tenant by the query filter.
    Task<Department?> FindByCodeAsync(string code, CancellationToken ct);
}
