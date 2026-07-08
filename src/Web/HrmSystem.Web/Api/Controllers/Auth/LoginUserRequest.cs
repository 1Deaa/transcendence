using HrmSystem.Application.Features.Users.LoginUser;

namespace HrmSystem.Web.Api.Controllers.Auth;

//! Identifier can be: Username / Email / PhoneNumber — matches LogInUserCommand contract
public sealed record LoginUserRequest(string Identifier, string Password)
{
    public LogInUserCommand ToCommand() => new(Identifier, Password);
}
