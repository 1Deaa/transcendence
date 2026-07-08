using HrmSystem.Application.Common.Interfaces.RealTime;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace HrmSystem.Web.Hubs;

//? Web-side implementation of the Application notifier port — SignalR stays in this layer.
internal sealed class AnalyticsNotifier(IHubContext<AnalyticsHub> hubContext) : IAnalyticsNotifier
{
    public async Task PublishMetricsChangedAsync(
        TenantId tenantId,
        IReadOnlyList<string> metricKeys,
        CancellationToken ct
    )
    {
        await hubContext
            .Clients.Group(AnalyticsHub.TenantGroupName(tenantId.Value))
            .SendAsync("MetricsChanged", metricKeys, ct);
    }
}
