namespace HrmSystem.Application.Common.Authentication;

/*
    //?     Single source of truth for every claim name used across the system.
    //?     Shared by all three layers that touch claims:
    //?       - Infrastructure  ([JwtTokenProvider] stamps these into JWT access tokens)
    //?       - Application     ([MvcLoginCommandHandler], [MvcRegisterCommandHandler] stamp cookies)
    //?       - Web             ([UserContext] reads these from [HttpContext.User])
    //
    //*     Standard OIDC/JWT names are defined as string literals matching the spec exactly.
    //*     Keeping them here (instead of referencing Microsoft.IdentityModel.*) means the
    //*     Application layer stays free of JWT library dependencies.
    //
    //!     Custom claim names (prefixed "domain-") are our own convention.
    //!     Once a claim name is in a live token or a signed cookie, renaming it is a
    //!     breaking change — all active sessions lose the claim until they re-authenticate.
*/
public static class CustomClaimTypes
{
    // ── Standard OIDC / JWT ── (values are spec-defined; do not change) ───────

    //*     Subject — ASP.NET Identity PK.
    //*     Used by [UserManager] for token operations; must be the Identity user's string ID.
    public const string Sub = "sub";

    //*     User's primary email address.
    public const string Email = "email";

    //*     Display / login username (OIDC "preferred_username" claim).
    public const string PreferredUsername = "preferred_username";

    // ── Custom ── (our own convention; stable once issued) ────────────────────

    /*
        //*     Domain [UserId] value (e.g. "user-019750ab-…").
        //*     Read by [UserContext] to resolve the domain entity without a DB lookup per request.
        //!     The "domain_" prefix separates it visually from standard OIDC claims in token debuggers.
    */
    public const string DomainUserId = "domain_sub";

    //*     User's phone number — omitted from the token / cookie when null.
    public const string PhoneNumber = "phone_number";

    /*
        //*     TenantId of the company workspace the user belongs to (e.g. "tenant-019750ab-…").
        //*     Read by [CurrentTenantContext] to scope every query/write to the caller's tenant.
        //!     Host-level admins (platform staff) have NO tenant claim — their TenantId resolves null.
    */
    public const string TenantId = "tenant_id";

    /*
        //*     Permission claim type — stored on the Identity user via [UserManager.AddClaimAsync].
        //*     Permission values (e.g. "habits:write", "admin:read") are the claim VALUE.
        //!     Always use this constant when reading or writing permission claims — never hardcode "permission".
    */
    public const string Permission = "permission";
}
