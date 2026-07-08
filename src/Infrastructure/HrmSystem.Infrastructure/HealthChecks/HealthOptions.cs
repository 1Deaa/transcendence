namespace HrmSystem.Infrastructure.HealthChecks;

//? Bound from the "HealthSettings" configuration section — thresholds for the custom checks.
public sealed class HealthOptions
{
    public const string SectionName = "HealthSettings";

    //? Disk check: Degraded below this, Unhealthy below half of it.
    public int MinimumFreeDiskGb { get; init; } = 5;

    //? Memory check: Degraded above this working set, Unhealthy above 1.5×.
    public int MaxWorkingSetMb { get; init; } = 2048;

    //? Backup freshness: Degraded when the last success is older than this…
    public int BackupFreshDegradedHours { get; init; } = 26;

    //? …Unhealthy when older than this.
    public int BackupFreshUnhealthyHours { get; init; } = 50;

    //? HealthProbeJob tick — one snapshot row per component per tick.
    public int ProbeIntervalSeconds { get; init; } = 60;
}
