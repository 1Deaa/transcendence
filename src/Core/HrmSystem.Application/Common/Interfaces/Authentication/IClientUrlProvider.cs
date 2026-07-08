namespace HrmSystem.Application.Common.Interfaces.Authentication;

/*
    //?     Builds front-end URLs that are embedded in transactional emails (password reset,
    //?     email confirmation). Abstracted here so Application handlers never hard-code
    //?     client base URLs — those live in [ClientSettings] in Infrastructure/appsettings.
    //
    //!     URL parameters are percent-encoded by the implementation — never double-encode
    //!     on the caller side.
*/
public interface IClientUrlProvider
{
    //>     Example: https://localhost:7001/auth/reset-password?email=alice%40example.com&token=CfDJ8...
    Uri BuildPasswordResetUrl(string email, string token);

    //>     Example: https://localhost:7001/auth/confirm-email?userId=a1b2c3&token=CfDJ8...
    Uri BuildEmailConfirmationUrl(string identityUserId, string token);
}
