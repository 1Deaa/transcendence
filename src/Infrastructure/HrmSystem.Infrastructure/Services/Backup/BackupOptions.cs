namespace HrmSystem.Infrastructure.Services.Backup;

/*
    //?     Bound from the "BackupSettings" configuration section.
    //!     Directory is a path AS SEEN BY SQL SERVER (the container's /var/backups volume) —
    //!     BACKUP DATABASE writes server-side, not from the API process.
*/
public sealed class BackupOptions
{
    public const string SectionName = "BackupSettings";

    public string Directory { get; init; } = "/var/backups";

    public string DatabaseName { get; init; } = "HrmSystem";

    //? Every backup is kept at least this long.
    public int DailyRetentionDays { get; init; } = 7;

    //? The first successful backup of each ISO week is kept this long instead (7 daily + 4 weekly).
    public int WeeklyRetentionDays { get; init; } = 28;

    //> Quartz cron — default: every day at 02:00 UTC.
    public string BackupCron { get; init; } = "0 0 2 * * ?";

    //> Quartz cron — default: every day at 03:30 UTC.
    public string RetentionCron { get; init; } = "0 30 3 * * ?";
}
