using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Application.Common.Errors;

/*
    //?     Application-level errors for the backup workflow.
    //>     SchedulerUnavailable is THE showcase of ErrorType.Unavailable → 503:
    //>     an operation depending on an unhealthy component refuses with "retry later".
*/
public static class BackupErrors
{
    public static readonly Error SchedulerUnavailable = Error.Unavailable(
        "Backups.SchedulerUnavailable",
        "The background job scheduler is not running — the backup cannot be triggered right now."
    );

    public static readonly Error TriggerFailed = Error.Unexpected(
        "Backups.TriggerFailed",
        "The backup job could not be enqueued."
    );
}
