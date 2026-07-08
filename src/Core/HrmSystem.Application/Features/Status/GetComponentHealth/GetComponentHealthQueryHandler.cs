using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Status.Shared;
using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Application.Features.Status.GetComponentHealth;

internal sealed class GetComponentHealthQueryHandler(IStatusQueries statusQueries)
    : IQueryHandler<GetComponentHealthQuery, IReadOnlyList<ComponentHealthResponse>>
{
    private readonly IStatusQueries _statusQueries = statusQueries;

    public async Task<Result<IReadOnlyList<ComponentHealthResponse>>> Handle(
        GetComponentHealthQuery query,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<ComponentHealthResponse> components =
            await _statusQueries.GetLatestComponentStatesAsync(cancellationToken);

        return Result.Success(components);
    }
}
