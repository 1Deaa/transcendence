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

        //? UUIDv7 IDs are creation-time ordered — newest employees first without a CreatedAt sort.
        List<Employee> items = await query
            .OrderByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
