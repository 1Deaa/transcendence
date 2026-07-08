using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Imports.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Operations;

/*
    //?     Application-facing abstraction over the background job scheduler for imports —
    //?     enqueues an immediate ProcessImportJob run carrying the ImportJobId.
    //!     Scheduler down → BackupErrors-style Unavailable result; the job row stays Pending
    //!     and the stale-Pending sweep (CleanupImportJobsJob) eventually fails it.
*/
public interface IImportScheduler
{
    Task<Result> EnqueueImportAsync(ImportJobId importJobId, CancellationToken ct);
}
