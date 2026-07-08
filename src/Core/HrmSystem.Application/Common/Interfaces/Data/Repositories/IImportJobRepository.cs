using HrmSystem.Domain.Entities.Imports;
using HrmSystem.Domain.Entities.Imports.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface IImportJobRepository : IBaseRepository<ImportJob, ImportJobId>
{
    //? Newest-first page for the imports dashboard (tenant-scoped by the query filter).
    Task<(IReadOnlyList<ImportJob> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct
    );
}
