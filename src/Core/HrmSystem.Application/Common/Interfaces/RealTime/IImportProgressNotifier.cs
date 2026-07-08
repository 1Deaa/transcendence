using HrmSystem.Application.Features.Imports.Shared;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.RealTime;

/*
    //?     Pushes live import progress to the uploading tenant's dashboards.
    //!     SignalR is the ENHANCEMENT path — polling GET /api/imports/{id} is the
    //!     guaranteed path (DevHabit pattern); a missed push never loses information.
*/
public interface IImportProgressNotifier
{
    Task PublishProgressAsync(
        TenantId tenantId,
        ImportProgressUpdate progress,
        CancellationToken ct
    );
}
