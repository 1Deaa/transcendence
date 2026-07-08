namespace HrmSystem.Domain.Entities.Operations.Enums;

public enum BackupStatus
{
    //! Sentinel — never persist; used to catch uninitialized values in validation.
    None = 0,

    //? BACKUP DATABASE is executing.
    Running = 1,

    Succeeded = 2,
    Failed = 3,

    //? The .bak file was removed by the retention policy — the history row remains.
    Purged = 4,
}
