using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Users.ConfirmEmail;

/*
    //?     Carries the [userId] and [token] embedded in the confirmation link sent to the user.
    //
    //>     Flow: user registers → receives confirmation email → clicks the link →
    //>       GET /api/auth/confirm-email?userId=...&token=...
    //
    //!     [IdentityUserId] is the ASP.NET Identity PK (a string GUID), NOT the domain [UserId].
    //!     The confirmation link carries the Identity ID so the handler can skip the domain
    //!     repository lookup and call [UserManager.ConfirmEmailAsync] directly.
*/
public sealed record ConfirmEmailCommand(string IdentityUserId, string Token) : ICommand;
