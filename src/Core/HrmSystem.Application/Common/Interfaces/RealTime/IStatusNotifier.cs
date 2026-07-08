using HrmSystem.Application.Features.Status.Shared;

namespace HrmSystem.Application.Common.Interfaces.RealTime;

/*
    //?     Pushes status-page updates to connected clients when a component's health
    //?     TRANSITIONS (healthy→unhealthy or back). Implemented in Web over SignalR
    //?     (StatusHub) — Application publishes, the transport stays out of this layer.
*/
public interface IStatusNotifier
{
    Task PublishStatusChangedAsync(PublicStatusResponse status, CancellationToken ct);
}
