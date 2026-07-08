using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Application.Common.Interfaces.Operations;

/*
    //?     Application-facing abstraction over the background job scheduler (Quartz lives in
    //?     Infrastructure). TriggerBackupAsync enqueues an immediate DatabaseBackupJob run.
    //!     Returns BackupErrors.SchedulerUnavailable (→ 503) when the scheduler is down —
    //!     the Result-pattern integration point of the health module.
*/
public interface IBackupScheduler
{
    Task<Result> TriggerBackupAsync(CancellationToken ct);
}
