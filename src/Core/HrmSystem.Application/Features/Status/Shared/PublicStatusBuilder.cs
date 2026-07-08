using HrmSystem.Domain.Entities.Operations.Enums;

namespace HrmSystem.Application.Features.Status.Shared;

/*
    //?     Builds the anonymous-safe status payload — shared by the pull path
    //?     (GetPublicStatusQueryHandler) and the push path (HealthProbeJob → StatusHub)
    //?     so both always agree on shape and redaction rules.
    //!     Overall = worst component state; "Unknown" before the first probe ever runs.
*/
public static class PublicStatusBuilder
{
    public static PublicStatusResponse Build(
        IReadOnlyList<ComponentHealthResponse> components,
        double uptime30,
        double uptime90,
        LastBackupSummary? lastBackup
    )
    {
        //? Worst-of pattern-match: any Unhealthy wins, then any Degraded, else Healthy.
        bool anyUnhealthy = components.Any(c => c.Status == nameof(HealthState.Unhealthy));
        bool anyDegraded = components.Any(c => c.Status == nameof(HealthState.Degraded));

        string overall = (components.Count, anyUnhealthy, anyDegraded) switch
        {
            (0, _, _) => "Unknown",
            (_, true, _) => nameof(HealthState.Unhealthy),
            (_, false, true) => nameof(HealthState.Degraded),
            _ => nameof(HealthState.Healthy),
        };

        //! Redaction boundary: only Name + Status cross into the anonymous response.
        return new PublicStatusResponse(
            overall,
            components.Select(c => new PublicComponentStatus(c.Name, c.Status)).ToList(),
            uptime30,
            uptime90,
            components.Count > 0 ? components.Max(c => c.CheckedAtUtc) : null,
            lastBackup
        );
    }
}
