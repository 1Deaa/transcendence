using System.Security.Claims;

namespace HrmSystem.Client.Services;

/*
    //?     Claim helpers for UI decisions (menu visibility, button gating).
    //!     UX ONLY — the API re-checks every permission server-side on every call.
*/
public static class ClaimsPrincipalExtensions
{
    public const string PermissionClaim = "permission";
    public const string TenantClaim = "tenant_id";

    //? Host-level accounts (platform admins) carry no tenant claim by design.
    public static bool HasTenant(this ClaimsPrincipal user) =>
        !string.IsNullOrEmpty(user.FindFirst(TenantClaim)?.Value);

    public static bool Can(this ClaimsPrincipal user, string permission) =>
        user.HasClaim(PermissionClaim, permission);

    public static string DisplayName(this ClaimsPrincipal user) =>
        user.Identity?.Name
        ?? user.FindFirst("preferred_username")?.Value
        ?? user.FindFirst("email")?.Value
        ?? "User";

    public static string RoleName(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value ?? "Member";

    //? Domain UserId ("user-…") — the id the chat API and hub payloads speak in.
    public static string? DomainUserId(this ClaimsPrincipal user) =>
        user.FindFirst("domain_sub")?.Value;
}
