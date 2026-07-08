using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Entities.Imports;
using HrmSystem.Domain.Entities.Imports.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class ImportJobRepository
    : ABaseRepository<ImportJob, ImportJobId>, IImportJobRepository
{
    public ImportJobRepository(ApplicationDbContext dbContext)
        : base(dbContext) { }

    public async Task<(IReadOnlyList<ImportJob> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        IQueryable<ImportJob> query = DbContext.Set<ImportJob>().AsNoTracking();

        int totalCount = await query.CountAsync(ct);

        List<ImportJob> items = await query
            .OrderByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
