using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace HrmSystem.Web.Authentication;

/*
    //*     Authentication scheme names and policy names used across the Web layer.
    //*     Centralized here so controllers and DI setup never use raw strings.
*/
internal static class AuthSchemes
{
    //*     JWT Bearer — default scheme; used by all API controllers.
    internal const string JwtBearer = JwtBearerDefaults.AuthenticationScheme;

    //*     Cookie — used by Razor View / MVC controllers only.
    //!     If you ever add a second cookie scheme (e.g. "ExternalCookie" for OAuth providers), having descriptive names rather than the generic "'Cookies' from 'CookieAuthenticationDefaults.AuthenticationScheme'" keeps it unambiguous
    //internal const string MvcCookie = CookieAuthenticationDefaults.AuthenticationScheme;
    internal const string MvcCookie = "MvcCookie";
}
