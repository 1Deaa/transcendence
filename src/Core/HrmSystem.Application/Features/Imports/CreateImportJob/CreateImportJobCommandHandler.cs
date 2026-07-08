using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Interfaces.Operations;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Imports;
using HrmSystem.Domain.Entities.Imports.Enums;

namespace HrmSystem.Application.Features.Imports.CreateImportJob;

/*
    //?     Persists the upload as a Pending ImportJob (file bytes in the row), then enqueues
    //?     the background processor. Returns the job id for Location + polling.
    //!     Enqueue failure surfaces as 503 — the Pending row stays; CleanupImportJobsJob
    //!     marks it Failed if nothing ever picks it up.
*/
internal sealed class CreateImportJobCommandHandler(
    IImportJobRepository importJobRepository,
    IImportScheduler importScheduler,
    IUnitOfWork unitOfWork
) : ICommandHandler<CreateImportJobCommand, string>
{
    private readonly IImportJobRepository _importJobRepository = importJobRepository;
    private readonly IImportScheduler _importScheduler = importScheduler;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<string>> Handle(
        CreateImportJobCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<ImportJob> jobResult = ImportJob.Create(
            ImportEntityType.Employees,
            command.FileName,
            command.FileContent
        );
        if (jobResult.IsFailure)
        {
            return jobResult.Errors.ToList();
        }

        ImportJob job = jobResult.Value;
        await _importJobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Result enqueueResult = await _importScheduler.EnqueueImportAsync(
            job.Id!,
            cancellationToken
        );
        if (enqueueResult.IsFailure)
        {
            return enqueueResult.Errors.ToList();
        }

        return job.Id!.Value;
    }
}
