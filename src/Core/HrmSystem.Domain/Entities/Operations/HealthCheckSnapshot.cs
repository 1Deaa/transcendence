using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Operations.Enums;
using HrmSystem.Domain.Entities.Operations.ValueObjects;

namespace HrmSystem.Domain.Entities.Operations;

/*
    //?     One row per component per health probe tick (HealthProbeJob, every 60s).
    //?     The raw material for uptime percentages and the status-page history chart.
    //!     HOST-LEVEL: not tenant-owned. Immutable once written (probe data is a fact).
    //!     Volume control: rows older than 90 days are purged by the nightly cleanup job.
*/
public class HealthCheckSnapshot : AEntity<HealthCheckSnapshotId>
{
    #region Constructor

    private HealthCheckSnapshot(
        HealthCheckSnapshotId id,
        string componentName,
        HealthState state,
        double durationMs,
        string? description,
        DateTime checkedAtUtc
    )
        : base(id)
    {
        ComponentName = componentName;
        State = state;
        DurationMs = durationMs;
        Description = description;
        CheckedAtUtc = checkedAtUtc;
    }

    //! Private Parameterless Constructor: for EF Core materialisation only — never use in domain or application code.
    private HealthCheckSnapshot()
        : base() { }

    #endregion

    #region Properties

    //? Registered health check name, e.g. "sqlserver", "redis", "backup-freshness".
    public string ComponentName { get; private set; } = string.Empty;

    public HealthState State { get; private set; }

    //? How long the probe took — spikes reveal degradation before outright failure.
    public double DurationMs { get; private set; }

    //? Probe description or exception message; kept short for the detail dashboard.
    public string? Description { get; private set; }

    public DateTime CheckedAtUtc { get; private set; }

    #endregion

    #region Static factory

    public static Result<HealthCheckSnapshot> Record(
        string componentName,
        HealthState state,
        double durationMs,
        string? description,
        DateTime checkedAtUtc
    )
    {
        return new HealthCheckSnapshot(
            HealthCheckSnapshotId.New(),
            componentName,
            state,
            durationMs,
            description,
            checkedAtUtc
        );
    }

    #endregion
}
