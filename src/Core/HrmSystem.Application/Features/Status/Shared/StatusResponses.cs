namespace HrmSystem.Application.Features.Status.Shared;

/*
    //?     Read models for the status endpoints.
    //!     PublicStatusResponse is ANONYMOUS-facing: component names + coarse status only —
    //!     no descriptions, no exception text, no infrastructure details (statuspage.io rule).
    //?     ComponentHealthResponse is the authenticated detail view (health:read).
*/
public sealed record PublicComponentStatus(string Name, string Status);

public sealed record LastBackupSummary(DateTime? CompletedAtUtc, string? Status);

public sealed record PublicStatusResponse(
    string OverallStatus,
    IReadOnlyList<PublicComponentStatus> Components,
    double Uptime30dPercent,
    double Uptime90dPercent,
    DateTime? LastCheckedAtUtc,
    LastBackupSummary? LastBackup
);

public sealed record ComponentHealthResponse(
    string Name,
    string Status,
    double DurationMs,
    string? Description,
    DateTime CheckedAtUtc
);

public sealed record UptimePoint(DateOnly Date, double UptimePercent);
