using HrmSystem.Application.Common.Interfaces.Caching;
using HrmSystem.Domain.Common.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HrmSystem.Application.Common.Behaviors;

internal sealed class QueryCachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
    where TResponse : Result
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<QueryCachingBehavior<TRequest, TResponse>> _logger;

    public QueryCachingBehavior(
        ICacheService cacheService,
        ILogger<QueryCachingBehavior<TRequest, TResponse>> logger
    )
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        TResponse? cachedResult = await _cacheService.GetAsync<TResponse>(
            request.CacheKey,
            cancellationToken
        );

        string queryName = typeof(TRequest).Name;

        if (cachedResult is not null)
        {
            _logger.LogInformation("Cache Hit for {Query}", queryName);

            return cachedResult;
        }

        _logger.LogInformation("Cache Miss for {Query}", queryName);

        TResponse responseResult = await next(cancellationToken);

        if (responseResult.IsSuccess)
        {
            await _cacheService.SetAsync(
                request.CacheKey,
                responseResult,
                request.Expiration,
                cancellationToken
            );
        }

        return responseResult;
    }
}
