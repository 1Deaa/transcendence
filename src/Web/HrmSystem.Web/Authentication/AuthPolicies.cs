namespace HrmSystem.Web.Authentication;

internal static class AuthPolicies
{
    //*     Apply to API controllers: [Authorize(Policy = AuthPolicies.ApiUser)]
    //*     or just [Authorize] since JWT is the default scheme.
    internal const string ApiUser = "ApiUser";

    //*     Apply to MVC/Razor controllers: [Authorize(Policy = AuthPolicies.MvcUser)]
    //!     Browser cannot send JWT — MVC controllers MUST use this policy or specify
    //!     the cookie scheme explicitly, otherwise [Authorize] will challenge with JWT
    //!     and redirect the browser to a token endpoint instead of the login page.
    internal const string MvcUser = "MvcUser";
}
