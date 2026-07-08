using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.RealTime;

/*
    //?     Pushes "these metrics changed" hints to a tenant's connected dashboards.
    //?     Clients re-fetch only the affected charts (debounced) — the hub never carries
    //?     data itself, so a lost message costs one refresh, never correctness.
    //>     Metric keys: "summary", "attendance-trend", "headcount-trend", "leave-stats",
    //>                  "department-distribution".
*/
public interface IAnalyticsNotifier
{
    Task PublishMetricsChangedAsync(
        TenantId tenantId,
        IReadOnlyList<string> metricKeys,
        CancellationToken ct
    );
}
