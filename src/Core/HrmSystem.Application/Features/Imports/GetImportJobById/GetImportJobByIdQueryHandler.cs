using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Imports.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Imports;
using HrmSystem.Domain.Entities.Imports.ValueObjects;

namespace HrmSystem.Application.Features.Imports.GetImportJobById;

internal sealed class GetImportJobByIdQueryHandler(IImportJobRepository importJobRepository)
    : IQueryHandler<GetImportJobByIdQuery, ImportJobResponse>
{
    private readonly IImportJobRepository _importJobRepository = importJobRepository;

    public async Task<Result<ImportJobResponse>> Handle(
        GetImportJobByIdQuery query,
        CancellationToken cancellationToken
    )
    {
        Result<ImportJobId> idResult = ImportJobId.From(query.ImportJobId);
        if (idResult.IsFailure)
        {
            return idResult.Errors.ToList();
        }

        ImportJob? job = await _importJobRepository.GetByIdAsync(
            idResult.Value,
            cancellationToken
        );
        if (job is null)
        {
            return ImportJobErrors.NotFound;
        }

        return ImportJobResponse.FromImportJob(job);
    }
}
