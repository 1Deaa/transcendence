using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Interfaces.Operations;
using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Application.Features.Backups.TriggerBackup;

/*
    //?     Enqueues an immediate DatabaseBackupJob run.
    //!     When the Quartz scheduler is down this returns BackupErrors.SchedulerUnavailable
    //!     (ErrorType.Unavailable) → 503 — the health module's Result-pattern showcase.
*/
internal sealed class TriggerBackupCommandHandler(IBackupScheduler backupScheduler)
    : ICommandHandler<TriggerBackupCommand>
{
    private readonly IBackupScheduler _backupScheduler = backupScheduler;

    public async Task<Result> Handle(
        TriggerBackupCommand command,
        CancellationToken cancellationToken
    )
    {
        return await _backupScheduler.TriggerBackupAsync(cancellationToken);
    }
}
