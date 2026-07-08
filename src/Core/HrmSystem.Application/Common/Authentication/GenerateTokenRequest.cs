namespace HrmSystem.Application.Common.Authentication;

/*
    //?     Input to token generation — carries everything needed to populate JWT claims.
    //
    //*     [IdentityUserId] → JWT [sub] claim — ASP.NET Identity PK, used by UserManager.
    //*     [DomainUserId]   → custom [domain_sub] claim — read by ICurrentUserContext.
    //*     [Roles]          → encoded as [ClaimTypes.Role] claims in the JWT.
    //*     [Permissions]    → encoded as [CustomClaimTypes.Permission] claims in the JWT.
    //*                        Derived from role claims — never stored directly on the user.
    //
    //!     Never pass a full domain entity here — this record is decoupled from the domain
    //!     model so token generation has no dependency on domain or EF Core types.
    //
    //!     New users at registration have no roles yet — pass empty lists.
    //!     Roles and permissions are assigned by an admin after the user account is created.
*/
public sealed record GenerateTokenRequest(
    string IdentityUserId,
    string DomainUserId,
    string Email,
    string UserName,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    //! Null for host-level platform admins — their tokens carry no tenant claim.
    string? TenantId = null
);
