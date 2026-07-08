using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Users.RegisterUser;

public sealed record RegisterUserCommand(
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
) : ICommand<AccessTokensResponse>;
