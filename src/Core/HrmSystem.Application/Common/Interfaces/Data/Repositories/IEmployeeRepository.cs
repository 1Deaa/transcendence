using HrmSystem.Domain.Entities.Departments.ValueObjects;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.Enums;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface IEmployeeRepository : IBaseRepository<Employee, EmployeeId>
{
    //? Duplicate-email guard for hire/update — scoped to the current tenant by the query filter.
    Task<Employee?> FindByEmailAsync(string email, CancellationToken ct);

    //? Blocks department removal while staff are still assigned (DepartmentErrors.HasEmployees).
    Task<bool> AnyInDepartmentAsync(DepartmentId departmentId, CancellationToken ct);

    /*
        //?     Offset-paged employee listing with optional filters (DevHabit pagination pattern).
        //?     Returned as (Items, TotalCount) so the handler builds the PaginationResult —
        //?     the repository stays free of API DTO shapes.
    */
    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm,
        DepartmentId? departmentId,
        EmployeeStatus? status,
        string? sortBy,
        string? sortDirection,
        CancellationToken ct
    );

    Task DeletePermanentlyByEmailAsync(string email, CancellationToken ct);
}
