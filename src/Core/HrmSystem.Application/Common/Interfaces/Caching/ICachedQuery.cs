using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Common.Interfaces.Caching;

public interface ICachedQuery<TResponse> : IQuery<TResponse>, ICachedQuery { }

public interface ICachedQuery
{
    string CacheKey { get; }

    TimeSpan? Expiration { get; }
}
