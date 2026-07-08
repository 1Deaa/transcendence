using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Imports.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Imports;

namespace HrmSystem.Application.Features.Imports.GetImportJobs;

internal sealed class GetImportJobsQueryHandler(IImportJobRepository importJobRepository)
    : IQueryHandler<GetImportJobsQuery, PaginationResult<ImportJobResponse>>
{
    private readonly IImportJobRepository _importJobRepository = importJobRepository;

    public async Task<Result<PaginationResult<ImportJobResponse>>> Handle(
        GetImportJobsQuery query,
        CancellationToken cancellationToken
    )
    {
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        (IReadOnlyList<ImportJob> items, int totalCount) =
            await _importJobRepository.GetPagedAsync(page, pageSize, cancellationToken);

        var result =
            PaginationResult<ImportJobResponse>.Create(
                items.Select(ImportJobResponse.FromImportJob).ToList(),
                page,
                pageSize,
                totalCount
            );

        return result;
    }
}
