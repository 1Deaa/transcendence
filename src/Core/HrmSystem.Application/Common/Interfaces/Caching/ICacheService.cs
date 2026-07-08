namespace HrmSystem.Application.Common.Interfaces.Caching;

//! By this interface abstraction I don't really know that we are talking to [Redis] under the hood which is Memory Database
public interface ICacheService
{
    Task<T?> GetAsync<T>(string cacheKey, CancellationToken ct = default);

    Task SetAsync<T>(
        string cacheKey,
        T Value,
        TimeSpan? expiration = null,
        CancellationToken ct = default
    );

    Task RemoveAsync(string cacheKey, CancellationToken ct = default);
}
