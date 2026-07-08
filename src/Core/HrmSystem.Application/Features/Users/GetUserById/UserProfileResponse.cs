namespace HrmSystem.Application.Features.Users.GetUserById;

/*
    //?     Read model returned by [GET /api/users/{userId}] and the Admin MVC profile page.
    //?     Intentionally separate from [CurrentUserResponse]:
    //?       - [CurrentUserResponse] is scoped to "the caller looking at themselves".
    //?       - [UserProfileResponse] is scoped to "anyone authorised to inspect a specific user".
    //?     Keeping them separate allows them to diverge — e.g. hiding secondary contact
    //?     details from admin views, or adding audit fields later, without touching each other.
    //
    //*     [Roles] and [Permissions] are included because the primary consumer is the Admin UI,
    //*     where displaying a user's authorisation state is the whole point of the lookup.
    //
    //!     Never expose password hashes, security stamps, or raw Identity internals here.
    //!     [IdentityId] is included only to allow admin tooling to cross-reference Identity records.
*/
public sealed record UserProfileResponse(
    string DomainUserId,
    string IdentityId,
    string UserName,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    bool IsDeleted,
    DateTime MemberSince,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);
