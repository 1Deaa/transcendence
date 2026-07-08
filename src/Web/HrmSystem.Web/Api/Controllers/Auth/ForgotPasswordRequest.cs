using HrmSystem.Application.Features.Users.ForgotPassword;

namespace HrmSystem.Web.Api.Controllers.Auth;

public sealed record ForgotPasswordRequest(string Email)
{
    public ForgotPasswordCommand ToCommand() => new(Email);
}
