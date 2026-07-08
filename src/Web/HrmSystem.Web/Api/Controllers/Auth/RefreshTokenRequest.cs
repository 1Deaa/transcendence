using HrmSystem.Application.Features.Users.RefreshUserToken;

namespace HrmSystem.Web.Api.Controllers.Auth;

public sealed record RefreshTokenRequest(string RefreshToken)
{
    public RefreshUserTokenCommand ToCommand() => new(RefreshToken);
}
