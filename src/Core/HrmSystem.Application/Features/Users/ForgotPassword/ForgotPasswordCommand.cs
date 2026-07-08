using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Users.ForgotPassword;

/*
    //?     Triggers the password-reset email flow.
    //
    //!     Security invariant: the handler ALWAYS returns [Result.Success()] regardless of
    //!     whether the email address belongs to a registered user. This prevents an attacker
    //!     from probing the system to discover which email addresses are registered
    //!     (user enumeration attack). The email either arrives or it silently doesn't.
*/
public sealed record ForgotPasswordCommand(string Email) : ICommand;
