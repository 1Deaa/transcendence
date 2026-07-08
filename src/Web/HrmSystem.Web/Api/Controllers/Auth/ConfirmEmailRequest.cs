using HrmSystem.Application.Features.Users.ConfirmEmail;

namespace HrmSystem.Web.Api.Controllers.Auth;

public sealed record ConfirmEmailRequest(string UserId, string Token)
{
    public ConfirmEmailCommand ToCommand() => new(UserId, Token);
}
