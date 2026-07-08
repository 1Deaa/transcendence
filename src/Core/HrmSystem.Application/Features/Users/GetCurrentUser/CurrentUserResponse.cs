namespace HrmSystem.Application.Features.Users.GetCurrentUser;

/*
    //?     Snapshot of the authenticated user's full profile and authorization state.
    //?     Used by both the API [GET /api/users/me] and the MVC profile page.
    //
    //*     [Roles]       — Identity roles assigned via [UserManager.AddToRoleAsync].
    //*     [Permissions] — fine-grained permission strings stored as Identity claims
    //*                     of type [CustomClaimTypes.Permission] (e.g. "habits:write").
    //
    //!     Never include password hashes, security stamps, or raw Identity internals here.
*/
public sealed record CurrentUserResponse(
    string DomainUserId,
    string IdentityId,
    string UserName,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? SecondaryEmail,
    string? SecondaryPhoneNumber,
    bool IsActive,
    DateTime MemberSince,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);
