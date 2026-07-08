using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Users.ResetPassword;

/*
    //?     Carries the data submitted from the "reset password" form.
    //
    //>     Flow: user receives the reset email → clicks the link → the link pre-fills
    //>       Email and Token → user types a new password → POST /api/auth/reset-password.
*/
public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
) : ICommand;
