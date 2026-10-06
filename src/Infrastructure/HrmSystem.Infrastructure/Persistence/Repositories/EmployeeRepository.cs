using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments.ValueObjects;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.Enums;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository
    : ABaseRepository<Employee, EmployeeId>, IEmployeeRepository
{
    public EmployeeRepository(ApplicationDbContext dbContext)
        : base(dbContext) { }

    public async Task<Employee?> FindByEmailAsync(string email, CancellationToken ct)
    {
        /*
            //!     Email is converter-mapped, so SQL can only compare the WHOLE value object —
            //!     [e.Email.Value == email] would not translate. An unparseable input can never
            //!     match a stored (always-valid) email, so it short-circuits to null.
            //!     The tenant query filter scopes this to the caller's tenant automatically.
        */
        Result<Email> emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            return null;
        }

        return await DbContext
            .Set<Employee>()
            .FirstOrDefaultAsync(e => e.Email == emailResult.Value, ct);
    }

    public async Task<bool> AnyInDepartmentAsync(DepartmentId departmentId, CancellationToken ct)
    {
        return await DbContext.Set<Employee>().AnyAsync(e => e.DepartmentId == departmentId, ct);
    }

    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm,
        DepartmentId? departmentId,
        EmployeeStatus? status,
        string? sortBy,
        string? sortDirection,
        CancellationToken ct
    )
    {
        IQueryable<Employee> query = DbContext.Set<Employee>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            /*
                //?     Case-insensitivity comes from the default SQL Server collation, not from C#.
                //!     Email is intentionally NOT searched — it is converter-mapped, so partial
                //!     Contains() on it cannot translate to SQL (only whole-value equality can).
            */
            string term = searchTerm.Trim();
            query = query.Where(e =>
                e.Name.FirstName.Contains(term)
                || e.Name.LastName.Contains(term)
                || e.JobTitle.Value.Contains(term)
            );
        }

        if (departmentId is not null)
        {
            query = query.Where(e => e.DepartmentId == departmentId);
        }

        if (status is not null)
        {
            query = query.Where(e => e.Status == status);
        }

        int totalCount = await query.CountAsync(ct);

        /*
            //?     Sort whitelist — the API accepts a known field name + direction, never a
            //?     raw column. Unknown values fall through to the default ordering.
            //?     Default = newest first: UUIDv7 IDs are creation-time ordered, which is
            //?     the listing's original behaviour without a CreatedAt sort.
        */
        query = (sortBy?.Trim().ToLowerInvariant(), sortDirection?.Trim().ToLowerInvariant()) switch
        {
            ("name", "desc") => query
                .OrderByDescending(e => e.Name.FirstName)
                .ThenByDescending(e => e.Name.LastName)
                .ThenByDescending(e => e.Id),
            ("name", _) => query
                .OrderBy(e => e.Name.FirstName)
                .ThenBy(e => e.Name.LastName)
                .ThenBy(e => e.Id),
            ("jobtitle", "desc") => query
                .OrderByDescending(e => e.JobTitle.Value)
                .ThenByDescending(e => e.Id),
            ("jobtitle", _) => query.OrderBy(e => e.JobTitle.Value).ThenBy(e => e.Id),
            ("hiredon", "desc") => query.OrderByDescending(e => e.HiredOn).ThenByDescending(e => e.Id),
            ("hiredon", _) => query.OrderBy(e => e.HiredOn).ThenBy(e => e.Id),
            _ => query.OrderByDescending(e => e.Id),
        };

        List<Employee> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task DeletePermanentlyByEmailAsync(string email, CancellationToken ct)
    {
        Employee? employee = await DbContext.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Email.Value == email, ct);
        if (employee is not null)
        {
            DbContext.Employees.Remove(employee);
        }
    }
}
