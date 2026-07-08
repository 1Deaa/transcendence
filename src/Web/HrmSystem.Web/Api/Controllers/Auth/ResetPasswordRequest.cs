using HrmSystem.Application.Features.Users.ResetPassword;

namespace HrmSystem.Web.Api.Controllers.Auth;

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword)
{
    public ResetPasswordCommand ToCommand() => new(Email, Token, NewPassword);
}
