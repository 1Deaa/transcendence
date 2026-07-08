using System.Security.Claims;
using HrmSystem.Application.Common.Authentication;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Authentication;

/*
    //?     Single Identity abstraction — covers both the auth flow (register, login, credential
    //?     validation) and the Identity-store queries (roles, permissions, tokens).
    //?     The Application layer references only this interface; [UserManager<AppUser>],
    //?     [RoleManager<IdentityRole>], and both DbContexts are Infrastructure-only details.
    //
    //*     Separated from [IJwtTokenProvider] intentionally:
    //*       - [IIdentityService]  — everything that touches the Identity store.
    //*       - [IJwtTokenProvider] — pure cryptographic token creation; no DB access.
*/
public interface IIdentityService
{
    // ── Registration & Login ─────────────────────────────────────────────────

    //TODO: THIS IS LIKE APPROACH OF KEYCLOACK AUTHENTICATION SERVICE! CHECK IT IN THE FUTURE AND SEARCH WHAT SHOULD YOU DO.

    /*
        //?     Accepts an already-validated domain [User] — domain rules are enforced in the
        //?     handler before this is called, so the service can trust the User is valid.
        //
        //*     The implementation owns the cross-context transaction:
        //*       1. Creates the ASP.NET Identity user (AppUser)
        //*       2. Links the identity ID back onto the domain User
        //*       3. Persists the domain User
        //*       4. Commits both in a single atomic transaction
        //
        //!     Returns [Result<UserId>] so Identity failures (duplicate email, password policy, …)
        //!     are carried back as typed [Error] values — never thrown as exceptions.
    */
    Task<Result<UserId>> RegisterAsync(User user, string password, CancellationToken ct);

    /*
        //?     Validates credentials and generates a token pair if successful.
        //?     [identifier] can be email or username — implementation tries both.
        //
        //!     Returns the same [InvalidCredentials] error for both "user not found" and
        //!     "wrong password" — prevents user enumeration attacks.
        //!     Never reveal whether an email exists in the system.
    */
    Task<Result<AccessTokensResponse>> LoginAsync(
        string identifier,
        string password,
        CancellationToken ct
    );

    /*
        //?     Validates credentials and returns the domain [User] — no tokens generated.
        //?     Used by MVC controllers that need the user identity to build a [ClaimsPrincipal]
        //?     and sign in with a cookie. The caller decides what to do after validation.
        //
        //*     Reuses the same lookup + password check as [LoginAsync] but stops before
        //*     token generation — keeping JWT concerns out of the MVC authentication flow.
        //
        //!     Same [InvalidCredentials] error for "not found" and "wrong password" —
        //!     prevents user enumeration attacks, identical to [LoginAsync].
    */
    Task<Result<User>> ValidateCredentialsAsync(
        string identifier,
        string password,
        CancellationToken ct
    );

    /*
        //?     Builds a ready [ClaimsPrincipal] from a domain [User]:
        //?       - stamps identity claims (sub, domain_sub, email, preferred_username, phone_number)
        //?       - loads the user's roles and their permission claims from the Identity store
        //?       - returns a principal stamped with [ClaimTypes.Role] and [CustomClaimTypes.Permission]
        //
        //*     Used by MVC login and MVC register so neither handler duplicates claims-building logic.
        //*     The returned principal is handed directly to [HttpContext.SignInAsync].
        //
        //!     Cookie size limit is ~4 KB. Roles are small strings; permissions are short
        //!     "resource:action" strings. For typical role sets (2-3 roles, 10-15 permissions)
        //!     this stays well within the limit. If the cookie grows over 4 KB, switch to a
        //!     server-side session store.
    */
    Task<ClaimsPrincipal> BuildClaimsPrincipalAsync(
        User user,
        string authScheme,
        CancellationToken ct
    );

    // ── Roles & Permissions ──────────────────────────────────────────────────

    /*
        //?     Returns the role names assigned to the Identity user.
        //?     Empty list when the user exists but has no roles.
    */
    Task<IReadOnlyList<string>> GetRolesAsync(string identityUserId, CancellationToken ct);

    /*
        //?     Derives the user's effective permissions from their roles.
        //?     For each role the user belongs to, fetches all claims of type
        //?     [CustomClaimTypes.Permission] ("permission") from the role.
        //?     Returns a deduplicated union across all roles.
        //!     Permissions are never stored directly on the user — RBAC only.
        //!     Admin tooling must use [roleManager.AddClaimAsync] to assign permissions to roles.
    */
    Task<IReadOnlyList<string>> GetPermissionsAsync(string identityUserId, CancellationToken ct);

    // ── Password reset ────────────────────────────────────────────────────────

    /*
        //?     Generates a time-limited, purpose-scoped token via ASP.NET Data Protection.
        //?     Token is NOT stored in the database — Identity validates it cryptographically.
        //!     Caller MUST verify the user exists before calling this.
        //!     If the Identity record is missing (data inconsistency), an exception is thrown.
    */
    Task<string> GeneratePasswordResetTokenAsync(string identityUserId, CancellationToken ct);

    /*
        //!     Takes [identityUserId], not email — handler already resolved the user from email.
        //!     Identity's PasswordValidator runs here — same policy as registration.
        //?     Returns [UserErrors.InvalidToken] when the token is expired, malformed, or mismatched.
    */
    Task<Result> ResetPasswordAsync(
        string identityUserId,
        string token,
        string newPassword,
        CancellationToken ct
    );

    // ── Account status ───────────────────────────────────────────────────────

    /*
        //?     Checks the domain [User.IsActive] flag — not Identity lockout.
        //?     Uses [IgnoreQueryFilters] so deactivated accounts are still found
        //?     and evaluated; without it a deactivated user silently returns [true].
        //!     Returns [true] (active) when the Identity record has no matching domain User
        //!     — avoids blocking anonymous / missing-record edge cases that the permission
        //!     handler will already deny independently.
    */
    Task<bool> IsActiveAsync(string identityUserId, CancellationToken ct);

    // ── Email confirmation ────────────────────────────────────────────────────

    /*
        //?     Generates a confirmation token with a different purpose scope than password reset.
        //?     ASP.NET Identity cannot swap the two token types — they are not interchangeable.
        //!     Caller MUST verify the user exists before calling this.
    */
    Task<string> GenerateEmailConfirmationTokenAsync(
        string identityUserId,
        CancellationToken ct
    );

    /*
        //?     Marks the Identity user's email as confirmed.
        //?     Returns [UserErrors.InvalidToken] when the token is invalid or already used.
    */
    Task<Result> ConfirmEmailAsync(string identityUserId, string token, CancellationToken ct);
}
