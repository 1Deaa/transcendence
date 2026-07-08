using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Users.LoginUser;

//! Identifier can be: Username / Email / PhoneNumber
public sealed record LogInUserCommand(string Identifier, string Password)
    : ICommand<AccessTokensResponse>;
