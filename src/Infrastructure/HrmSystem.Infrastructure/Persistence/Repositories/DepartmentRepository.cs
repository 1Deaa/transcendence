using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Departments.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentRepository
    : ABaseRepository<Department, DepartmentId>, IDepartmentRepository
{
    public DepartmentRepository(ApplicationDbContext dbContext)
        : base(dbContext) { }

    public async Task<Department?> FindByCodeAsync(string code, CancellationToken ct)
    {
        /*
            //!     Code is converter-mapped, so SQL can only compare the WHOLE value object —
            //!     [d.Code.Value == code] would not translate. An unparseable input can never
            //!     match a stored (always-valid) code, so it short-circuits to null.
            //!     The tenant query filter scopes this to the caller's tenant automatically.
        */
        Result<DepartmentCode> codeResult = DepartmentCode.Create(code);
        if (codeResult.IsFailure)
        {
            return null;
        }

        return await DbContext
            .Set<Department>()
            .FirstOrDefaultAsync(d => d.Code == codeResult.Value, ct);
    }
}
