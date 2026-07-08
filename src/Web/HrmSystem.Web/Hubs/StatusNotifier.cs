using HrmSystem.Application.Common.Interfaces.RealTime;
using HrmSystem.Application.Features.Status.Shared;
using Microsoft.AspNetCore.SignalR;

namespace HrmSystem.Web.Hubs;

//? Web-side implementation of the Application notifier port — SignalR stays in this layer.
internal sealed class StatusNotifier(IHubContext<StatusHub> hubContext) : IStatusNotifier
{
    public async Task PublishStatusChangedAsync(PublicStatusResponse status, CancellationToken ct)
    {
        await hubContext.Clients.All.SendAsync("StatusChanged", status, ct);
    }
}
