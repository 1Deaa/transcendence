using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HrmSystem.Web.Authentication;

/*
    //?     Intercepts every call to [IAuthorizationPolicyProvider.GetPolicyAsync].
    //?     Named policies (ApiUser, MvcUser) are resolved by the base implementation.
    //?     Any unknown permissionsAuthorizationPolicy name is treated as a permission string and a permissionsAuthorizationPolicy is
    //?     built on-the-fly — this is what makes [HasPermission("users:read")] work
    //?     without pre-registering every permission in AddAuthorization().
    //
    //*     The built permissionsAuthorizationPolicy is cached inside [AuthorizationOptions] on first access so
    //*     the construction cost is paid only once per permission per application lifetime.
    //*     [DefaultAuthorizationPolicyProvider] is still the fallback for fallback/default policies.
    //
    //!     Only permissionsAuthorizationPolicy names that contain ':' are treated as dynamic permission policies.
    //!     This prevents accidental permission-permissionsAuthorizationPolicy creation for typos in named permissionsAuthorizationPolicy names.
    //!     If a named permissionsAuthorizationPolicy is NOT found AND the name has no ':', [null] is returned and
    //!     ASP.NET Core will 403 the request (permissionsAuthorizationPolicy not found → forbidden).
*/
public sealed class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private readonly AuthorizationOptions _options;

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
        _options = options.Value;
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        //! Named policies (ApiUser, MvcUser, …) take priority.
        AuthorizationPolicy? existingPolicy = await base.GetPolicyAsync(policyName);
        if (existingPolicy is not null)
        {
            return existingPolicy;
        }

        // Treat "resource:action" names as permission strings.
        if (!policyName.Contains(':'))
        {
            return null;
        }

        AuthorizationPolicy permissionsAuthorizationPolicy = new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionAuthorizationRequirement(policyName))
            .Build();

        //? Cache Our permissionsAuthorizationPolicy value; so subsequent requests for the same permission skip this branch entirely.
        _options.AddPolicy(policyName, permissionsAuthorizationPolicy);

        return permissionsAuthorizationPolicy;
    }
}
