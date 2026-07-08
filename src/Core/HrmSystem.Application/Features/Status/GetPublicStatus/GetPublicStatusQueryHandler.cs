using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Status.Shared;
using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Application.Features.Status.GetPublicStatus;

internal sealed class GetPublicStatusQueryHandler(IStatusQueries statusQueries)
    : IQueryHandler<GetPublicStatusQuery, PublicStatusResponse>
{
    private readonly IStatusQueries _statusQueries = statusQueries;

    public async Task<Result<PublicStatusResponse>> Handle(
        GetPublicStatusQuery query,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<ComponentHealthResponse> components =
            await _statusQueries.GetLatestComponentStatesAsync(cancellationToken);

        double uptime30 = await _statusQueries.GetUptimePercentAsync(30, cancellationToken);
        double uptime90 = await _statusQueries.GetUptimePercentAsync(90, cancellationToken);
        LastBackupSummary? lastBackup = await _statusQueries.GetLastBackupAsync(cancellationToken);

        return PublicStatusBuilder.Build(components, uptime30, uptime90, lastBackup);
    }
}
