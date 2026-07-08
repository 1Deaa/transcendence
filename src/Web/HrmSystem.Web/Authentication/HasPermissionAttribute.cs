using Microsoft.AspNetCore.Authorization;

namespace HrmSystem.Web.Authentication;

/*
    //*     Drop-in replacement for [Authorize(Policy = "...")] that speaks the permission vocabulary.
    //*     Works identically on both API controllers (JWT) and MVC controllers (Cookie).
    //
    //>     Usage on API controllers:
    //>       [HasPermission(Permissions.Users.Read)]
    //>       public async Task<IActionResult> GetUserById(...)
    //
    //>     Usage on MVC Admin controllers:
    //>       [Authorize(Policy = AuthPolicies.MvcUser)]   ← class level: authenticates via cookie
    //>       public class UsersController : Controller
    //>       {
    //>           [HasPermission(Permissions.Users.Read)]  ← method level: checks permission claim
    //>           public async Task<IActionResult> Profile(...)
    //>       }
    //
    //!     [HasPermission("users:read")] is exactly equivalent to [Authorize(Policy = "users:read")].
    //!     [PermissionAuthorizationPolicyProvider] intercepts unknown policy names (like "users:read")
    //!     and builds a [PermissionAuthorizationRequirement] policy dynamically — no pre-registration needed.
    //!     Multiple [HasPermission] attributes on the same action create an AND condition
    //!     (ALL permissions must be present).
*/
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base(permission) { }
}
