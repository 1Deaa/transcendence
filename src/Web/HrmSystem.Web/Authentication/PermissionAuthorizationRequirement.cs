using Microsoft.AspNetCore.Authorization;

namespace HrmSystem.Web.Authentication;

/*
    //! - [Requirements] are simple marker classes that implement [IAuthorizationRequirement].
    //!     - They hold any data needed to evaluate the authorization check.
    //!     - They define what conditions must be met.
    //!
    //?     Carries the (permission string) that must be present in the (principal's claims).
    //?     Created by [PermissionAuthorizationPolicyProvider] when ASP.NET Core asks for
    //?     a policy whose name matches a permission string (e.g. "users:read").
    //
    //!     [Permission] is immutable — one [PermissionAuthorizationRequirement] per permission string.
*/
public sealed class PermissionAuthorizationRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionAuthorizationRequirement(string permission)
    {
        Permission = permission;
    }
}
