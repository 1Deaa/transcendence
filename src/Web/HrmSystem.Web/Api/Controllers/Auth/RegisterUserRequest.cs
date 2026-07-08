using HrmSystem.Application.Features.Users.RegisterUser;

namespace HrmSystem.Web.Api.Controllers.Auth;

public sealed record RegisterUserRequest(
    string UserName,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber,
    string? SecondaryEmail,
    string? SecondaryPhoneNumber
)
{
    public RegisterUserCommand ToCommand() =>
        new(
            UserName,
            FirstName,
            MiddleName,
            LastName,
            Email,
            Password,
            ConfirmPassword,
            PhoneNumber,
            SecondaryEmail,
            SecondaryPhoneNumber
        );
}
