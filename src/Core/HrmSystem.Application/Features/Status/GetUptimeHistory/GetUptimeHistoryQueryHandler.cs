using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Status.Shared;
using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Application.Features.Status.GetUptimeHistory;

internal sealed class GetUptimeHistoryQueryHandler(IStatusQueries statusQueries)
    : IQueryHandler<GetUptimeHistoryQuery, IReadOnlyList<UptimePoint>>
{
    private readonly IStatusQueries _statusQueries = statusQueries;

    public async Task<Result<IReadOnlyList<UptimePoint>>> Handle(
        GetUptimeHistoryQuery query,
        CancellationToken cancellationToken
    )
    {
        //! Clamp instead of erroring — this endpoint is anonymous; garbage input gets a sane window.
        int days = Math.Clamp(query.Days, 1, 90);

        IReadOnlyList<UptimePoint> history = await _statusQueries.GetUptimeHistoryAsync(
            days,
            cancellationToken
        );

        return Result.Success(history);
    }
}
