using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Operations.Enums;
using HrmSystem.Domain.Entities.Operations.ValueObjects;

namespace HrmSystem.Domain.Entities.Operations;

/*
    //?     One row per database backup attempt — the audit trail behind the status page's
    //?     "last backup" widget and the BackupFreshnessHealthCheck.
    //!     HOST-LEVEL: not tenant-owned; never filtered by tenant.
    //?     Lifecycle: Start() → Running → Complete(size) | Fail(error); Purge() after retention.
*/
public class BackupHistory : AAuditableEntity<BackupHistoryId>
{
    #region Constructor

    private BackupHistory(BackupHistoryId id, string fileName, DateTime startedAtUtc)
        : base(id)
    {
        FileName = fileName;
        StartedAtUtc = startedAtUtc;
        Status = BackupStatus.Running;
    }

    //! Private Parameterless Constructor: for EF Core materialisation only — never use in domain or application code.
    private BackupHistory()
        : base() { }

    #endregion

    #region Properties

    //? Backup file name inside the backups volume, e.g. "HrmSystem_20260707_020000.bak".
    public string FileName { get; private set; } = string.Empty;

    public BackupStatus Status { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    //? Size of the finished .bak file; null until Complete().
    public long? SizeBytes { get; private set; }

    //? Failure detail captured for the ops dashboard; null unless Fail() was called.
    public string? ErrorMessage { get; private set; }

    //? The retention job deletes the file and calls Purge() after this moment.
    public DateTime? RetainedUntilUtc { get; private set; }

    #endregion

    #region Static factory

    //? Starts a backup attempt in Running state — Complete()/Fail() must follow.
    public static Result<BackupHistory> Start(string fileName, DateTime startedAtUtc)
    {
        return new BackupHistory(BackupHistoryId.New(), fileName, startedAtUtc);
    }

    #endregion

    #region Domain methods

    public Result Complete(DateTime completedAtUtc, long sizeBytes, DateTime retainedUntilUtc)
    {
        if (Status != BackupStatus.Running)
        {
            return OperationsErrors.BackupAlreadyFinished;
        }

        Status = BackupStatus.Succeeded;
        CompletedAtUtc = completedAtUtc;
        SizeBytes = sizeBytes;
        RetainedUntilUtc = retainedUntilUtc;

        return Result.Success();
    }

    public Result Fail(DateTime completedAtUtc, string errorMessage)
    {
        if (Status != BackupStatus.Running)
        {
            return OperationsErrors.BackupAlreadyFinished;
        }

        Status = BackupStatus.Failed;
        CompletedAtUtc = completedAtUtc;
        ErrorMessage = errorMessage;

        return Result.Success();
    }

    //? Called by the retention job after the .bak file is deleted — the row stays for history.
    public Result Purge()
    {
        if (Status != BackupStatus.Succeeded)
        {
            return OperationsErrors.BackupAlreadyFinished;
        }

        Status = BackupStatus.Purged;

        return Result.Success();
    }

    #endregion
}
