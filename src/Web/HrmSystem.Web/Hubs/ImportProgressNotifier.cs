using HrmSystem.Application.Common.Interfaces.RealTime;
using HrmSystem.Application.Features.Imports.Shared;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace HrmSystem.Web.Hubs;

//? Web-side implementation of the Application notifier port — SignalR stays in this layer.
internal sealed class ImportProgressNotifier(IHubContext<ImportProgressHub> hubContext)
    : IImportProgressNotifier
{
    public async Task PublishProgressAsync(
        TenantId tenantId,
        ImportProgressUpdate progress,
        CancellationToken ct
    )
    {
        await hubContext
            .Clients.Group(ImportProgressHub.TenantGroupName(tenantId.Value))
            .SendAsync("ImportProgress", progress, ct);
    }
}
