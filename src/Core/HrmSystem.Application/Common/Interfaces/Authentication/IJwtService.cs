using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Application.Common.Interfaces.Authentication;

internal interface IJwtService
{
    Task<Result<string>> GetAccessTokenAsync(
        string identifier,
        string password,
        CancellationToken ct = default
    );
}
