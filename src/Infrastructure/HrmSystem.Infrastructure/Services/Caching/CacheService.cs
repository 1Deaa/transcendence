using System.Buffers;
using System.Text.Json;
using HrmSystem.Application.Common.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace HrmSystem.Infrastructure.Services.Caching;

internal sealed class CacheService : ICacheService
{
    #region Implementation

    //! [[IDistributedCache]] is a Built-in abstraction in ASP.NET Core; it allows me to: Get, Set, Remove cache values from the Underlying persistence store. The default implementation is a memory cache But there is also an implementation that allows me to talk with [[Redis]]
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IDistributedCache distributedCache, ILogger<CacheService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken ct = default)
    {
        try
        {
            //! Returning (( `bytes[]` )) and this means That we need to take care of:
            //?     De-serializing bytes[] => Into Object in Memory
            byte[]? cachedResultBytes = await _distributedCache.GetAsync(cacheKey, ct);

            return cachedResultBytes is null ? default : Deserialize<T>(cachedResultBytes);
        }
        catch (Exception ex)
        {
            /*
                //!     Redis is unavailable — treat as a cache miss so the request still succeeds.
                //!     The real query runs, the result is NOT cached (SetAsync also swallows errors).
                //*     This keeps the app functional when Redis is down (dev with no Docker, etc.).
            */
            _logger.LogWarning(ex, "Cache GET failed for key '{CacheKey}' — falling through to query.", cacheKey);
            return default;
        }
    }

    public async Task RemoveAsync(string cacheKey, CancellationToken ct = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(cacheKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key '{CacheKey}'.", cacheKey);
        }
    }

    public async Task SetAsync<T>(
        string cacheKey,
        T Value,
        TimeSpan? expiration = null,
        CancellationToken ct = default
    )
    {
        try
        {
            byte[] bytes = Serialize(Value);
            await _distributedCache.SetAsync(cacheKey, bytes, CacheOptions.Create(expiration), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key '{CacheKey}'.", cacheKey);
        }
    }

    #endregion Implementation


    #region Helpers

    private T Deserialize<T>(byte[] cachedResultBytes)
    {
        //! I can use NewtonSoft Json which Has a [[string]] based [[API]] to work with strings instead of working with [bytes] which could be easier To implement.
        return JsonSerializer.Deserialize<T>(cachedResultBytes)!;
    }

    private byte[] Serialize<T>(T? value)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using var writer = new Utf8JsonWriter(buffer);

        JsonSerializer.Serialize(writer, value);

        return buffer.WrittenSpan.ToArray();
    }

    #endregion Helpers
}
