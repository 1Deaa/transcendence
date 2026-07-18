using System.Data;
using System.Security.Claims;
using Dapper;
using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.RefreshTokens;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HrmSystem.Infrastructure.Services.Identity;

/*
    //*     Single Identity service — implements [IIdentityService] which covers
    //*     registration, login, credential validation, roles, permissions, and token operations.
    //*     Consolidates what was formerly [AuthenticationService] and [IdentityService].
    //
    //!     [GetPermissionsAsync] reads from ROLE CLAIMS, not user claims.
    //!     The RBAC model is: User → Roles → Role Claims (permissions).
    //!     Permissions are never stored directly on the user — always on the identityRole.
*/
internal sealed class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ApplicationIdentityDbContext _identityContext;
    private readonly ApplicationDbContext _applicationContext;
    private readonly IJwtTokenProvider _tokenProvider;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public IdentityService(
        UserManager<AppUser> userManager,
        ApplicationIdentityDbContext identityContext,
        ApplicationDbContext applicationContext,
        IJwtTokenProvider tokenProvider,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ISqlConnectionFactory sqlConnectionFactory
    )
    {
        _userManager = userManager;
        _identityContext = identityContext;
        _applicationContext = applicationContext;
        _tokenProvider = tokenProvider;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    // ── Registration ─────────────────────────────────────────────────────────

    public async Task<Result<UserId>> RegisterAsync(
        User user,
        string password,
        CancellationToken ct
    )
    {
        /*
            //!     Problem: [ApplicationDbContext] and [ApplicationIdentityDbContext] are two separate
            //!     EF Core contexts — each opens its own SQL connection and its own transaction by default.
            //!     Without coordination:
            //!       - [_userManager.CreateAsync] runs inside the Identity context's implicit transaction.
            //!       - [_applicationContext.SaveChangesAsync] runs inside the application context's own transaction.
            //!     If either succeeds and the other fails, the database is left in an inconsistent state:
            //!     an orphaned [AppUser] with no domain [User], or a domain [User] with no Identity record.
            //
            //?     Solution — share one SQL connection and one transaction across both contexts:
            //?       1. Begin a transaction on [ApplicationIdentityDbContext] — it becomes the "owner".
            //?       2. Hand its underlying [DbConnection] to [ApplicationDbContext] via SetDbConnection,
            //?          so both contexts speak through the same physical connection.
            //?       3. Enlist [ApplicationDbContext] in the same transaction via UseTransactionAsync.
            //?     From this point, every write from either context goes through the same transaction.
            //
            //>     Only ONE [CommitAsync] is needed — on the owning context's transaction.
            //>     If [CommitAsync] is never called (early return on error, or an exception),
            //>     the [using] block disposes the transaction and SQL Server rolls back ALL changes
            //>     from both contexts automatically. No partial writes, no orphaned data.
        */
        using IDbContextTransaction identityTransaction =
            await _identityContext.Database.BeginTransactionAsync(ct);

        /*
            //!     Captured BEFORE borrowing the Identity connection: [SetDbConnection] clears the
            //!     configured connection string, and [SetDbConnection(null)] after the commit does
            //!     NOT bring it back — without restoring it the next [SaveChangesAsync] on this
            //!     context dies with "The ConnectionString property has not been initialized".
        */
        string? originalConnectionString = _applicationContext.Database.GetConnectionString();

        //! Sets up shared connection
        _applicationContext.Database.SetDbConnection(_identityContext.Database.GetDbConnection());

        //! Enlists [_applicationContext] in shared transaction
        await _applicationContext.Database.UseTransactionAsync(
            identityTransaction.GetDbTransaction(),
            ct
        );

        var appUser = new AppUser
        {
            UserName = user.UserName.Value,
            Email = user.Email.Value,
            PhoneNumber = user.PhoneNumber?.Value,
        };

        /*
            //*     What [CreateAsync] does internally:
            //*       1. Runs all [IUserValidator<AppUser>]     → validates username, email uniqueness, etc.
            //*       2. Runs all [IPasswordValidator<AppUser>] → checks password policy
            //*       3. Hashes the password via [IPasswordHasher<AppUser>]
            //*       4. Calls [IUserStore.CreateAsync]         → EF Core Identity store
            //*       5. Calls [DbContext.SaveChangesAsync()]   → REAL INSERT fires here, inside the shared transaction
        */
        IdentityResult identityResult = await _userManager.CreateAsync(appUser, password);

        if (!identityResult.Succeeded)
        {
            /*
                //?     Map each [IdentityError] to our typed [Error] format so the caller receives
                //?     structured domain errors, not raw Identity objects.
                //!     [CommitAsync] is never reached here — the transaction disposes and rolls back.
            */
            var errors = identityResult
                .Errors.Select(e => Error.Validation($"Identity.{e.Code}", e.Description))
                .ToList();

            return errors;
        }

        /*
            //>     Link the Identity user ID to the domain [User] BEFORE persisting.
            //>     This is the bridge between the two stores: the domain [User] carries the
            //>     [IdentityId] so we can look up the [AppUser] from the domain side and vice versa.
            //>     The handler also reads [user.IdentityId] after this call to build the [GenerateTokenRequest].
        */
        user.SetIdentityId(appUser.Id);

        //* The key: [UserRepository] and [AuthenticationService] are both [Scoped] — ASP.NET DI gives them the ((same [ApplicationDbContext])) instance per request. So when you call repository; it's inside the same transaction.
        await _userRepository.AddAsync(user, ct);
        //// await _applicationContext.Users.AddAsync(user, ct); <-----> Using IUserRepository instead of this

        //!     [SaveChangesAsync] stages the write inside the shared transaction — does NOT commit yet.
        await _applicationContext.SaveChangesAsync(ct);

        //!     Single commit point — persists ALL changes from BOTH contexts atomically.
        await identityTransaction.CommitAsync(ct);

        /*
            //!     WHY THIS LINE EXISTS — do not remove it.
            //
            //?     At the top of this method we called [SetDbConnection] and [UseTransactionAsync] to
            //?     force [_applicationContext] to share the Identity context's SQL connection and transaction.
            //?     That sharing is what makes the two-context atomic write possible.
            //
            //!     The problem: [SetDbConnection] is STICKY.
            //!     After [CommitAsync] the transaction is gone, but [_applicationContext] still holds a
            //!     reference to the shared [SqlConnection] object. It does NOT know the transaction ended.
            //!     EF Core's SQL Server execution strategy wraps every [SaveChangesAsync] call in a
            //!     retry loop. At the start of each attempt it calls [CreateSavepointAsync("EFSavePoint")]
            //!     — which issues [SAVE TRANSACTION EFSavePoint] to SQL Server.
            //!     SQL Server rejects that with:
            //!         "Cannot issue SAVE TRANSACTION when there is no active transaction."  (Error 628)
            //!     because the shared transaction was already committed and is no longer open.
            //
            //*     THE FIX: call [SetDbConnection(null)] immediately after [CommitAsync].
            //*     This tells EF Core to stop using the borrowed connection and go back to
            //*     opening its own connection from the pool on the next operation.
            //*     The caller ([RegisterUserCommandHandler]) calls [_unitOfWork.SaveChangesAsync()]
            //*     right after this method returns — that save now gets a fresh connection,
            //*     starts its own independent transaction, and the savepoint machinery works correctly.
        */
        _applicationContext.Database.SetDbConnection(null);

        //! Completes the fix above: hand the context its own connection string back so the
        //! next operation opens a fresh pooled connection instead of finding an empty string.
        _applicationContext.Database.SetConnectionString(originalConnectionString);

        return user.Id;
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<Result<AccessTokensResponse>> LoginAsync(
        string identifier,
        string password,
        CancellationToken ct
    )
    {
        /*
            //?     One query across Email, UserName, and PhoneNumber — all three are indexed.
            //?     [FindByAnyIdentifierAsync] uses [IgnoreQueryFilters] so soft-deleted or inactive
            //?     accounts are still found here; we can return a specific error per state if needed.
            //
            //!     We return the same [InvalidCredentials] error for every failure branch —
            //!     "not found", "wrong password", "no Identity record".
            //!     Never reveal whether the identifier exists — that is a user enumeration vulnerability.
        */
        User? domainUser = await _userRepository.FindByAnyIdentifierAsync(identifier, ct);

        if (domainUser is null)
        {
            return UserErrors.InvalidCredentials;
        }

        /*
            //>     [IdentityId] is the primary key of [AppUser] — PK lookup, no scan.
            //>     This is why we store [IdentityId] on the domain [User]: it lets us bridge
            //>     the two stores without a second search-by-email/username on the Identity side.
        */
        AppUser? appUser = await _userManager.FindByIdAsync(domainUser.IdentityId);

        if (appUser is null)
        {
            //! Data inconsistency — domain User exists but Identity record was lost.
            //! Should never happen if registration always uses the shared transaction.
            return UserErrors.InvalidCredentials;
        }

        bool passwordValid = await _userManager.CheckPasswordAsync(appUser, password);

        if (!passwordValid)
        {
            return UserErrors.InvalidCredentials;
        }

        /*
            //?     Create and persist the domain [RefreshToken] entity so the token lifecycle
            //?     is tracked in the Application database (not the Identity store).
            //?     Expiry window comes from [IJwtTokenProvider.RefreshTokenLifetime] — single
            //?     source of truth in [JwtAuthOptions.RefreshTokenExpirationDays].
        */
        DateTimeOffset refreshExpiry = DateTimeOffset.UtcNow.Add(
            _tokenProvider.RefreshTokenLifetime
        );
        Result<RefreshToken> refreshTokenResult = RefreshToken.Create(
            domainUser.Id!,
            refreshExpiry
        );

        if (refreshTokenResult.IsFailure)
        {
            return refreshTokenResult.Errors;
        }

        await _refreshTokenRepository.AddAsync(refreshTokenResult.Value, ct);
        await _applicationContext.SaveChangesAsync(ct);

        /*
            //?     Load roles + their permission claims to embed in the JWT.
            //?     RBAC: User → Roles → Role Claims (permissions) → JWT permission claims.
            //?     Permission deduplication via [HashSet] — a user in two roles that share
            //?     "habits:read" only gets one permission roleClaim, not two.
        */
        (IReadOnlyList<string> roles, IReadOnlyList<string> permissions) =
            await LoadRolesAndPermissionsAsync(appUser.Id, ct);

        (string accessToken, DateTime expiresOnUtc) = _tokenProvider.CreateAccessToken(
            new GenerateTokenRequest(
                IdentityUserId: appUser.Id,
                DomainUserId: domainUser.Id!.Value,
                Email: appUser.Email!,
                UserName: domainUser.UserName.Value,
                PhoneNumber: domainUser.PhoneNumber?.Value,
                Roles: roles,
                Permissions: permissions,
                TenantId: domainUser.TenantId?.Value
            )
        );

        return new AccessTokensResponse(
            accessToken,
            refreshTokenResult.Value.Token.Value,
            expiresOnUtc
        );
    }

    public async Task<Result<User>> ValidateCredentialsAsync(
        string identifier,
        string password,
        CancellationToken ct
    )
    {
        /*
            //?     Same lookup + password check as [LoginAsync] — no token generation.
            //?     Returns the domain [User] so the caller (MVC controller) can build
            //?     a [ClaimsPrincipal] and sign in with whatever scheme it chooses.
            //
            //!     Same [InvalidCredentials] error for every failure — no enumeration.
        */
        User? domainUser = await _userRepository.FindByAnyIdentifierAsync(identifier, ct);

        if (domainUser is null)
        {
            return UserErrors.InvalidCredentials;
        }

        AppUser? appUser = await _userManager.FindByIdAsync(domainUser.IdentityId);

        if (appUser is null)
        {
            return UserErrors.InvalidCredentials;
        }

        bool passwordValid = await _userManager.CheckPasswordAsync(appUser, password);

        if (!passwordValid)
        {
            return UserErrors.InvalidCredentials;
        }

        return domainUser;
    }

    //! ── Cookie principal ──────────────────────────────────────────────────────
    public async Task<ClaimsPrincipal> BuildClaimsPrincipalAsync(
        User user,
        string authScheme,
        CancellationToken ct
    )
    {
        List<Claim> claims =
        [
            new Claim(CustomClaimTypes.Sub, user.IdentityId),
            new Claim(CustomClaimTypes.DomainUserId, user.Id!.Value),
            new Claim(CustomClaimTypes.Email, user.Email.Value),
            new Claim(CustomClaimTypes.PreferredUsername, user.UserName.Value),
        ];

        if (user.PhoneNumber is not null)
        {
            claims.Add(new Claim(CustomClaimTypes.PhoneNumber, user.PhoneNumber.Value));
        }

        /*
            //?     One JOIN query — UserRoles → Roles → RoleClaims — produces roles and permissions
            //?     in a single round-trip. No [FindByIdAsync] needed: [user.IdentityId] is the PK.
            //!     If the Identity record is missing, the JOIN returns no rows — empty lists,
            //!     same safe fallback as before but without an extra SELECT.
        */
        (IReadOnlyList<string> roles, IReadOnlyList<string> permissions) =
            await LoadRolesAndPermissionsAsync(user.IdentityId, ct);

        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (string permission in permissions)
        {
            claims.Add(new Claim(CustomClaimTypes.Permission, permission));
        }

        var identity = new ClaimsIdentity(claims, authScheme);
        return new ClaimsPrincipal(identity);
    }

    // ── Roles & Permissions ──────────────────────────────────────────────────

    public async Task<IReadOnlyList<string>> GetRolesAsync(
        string identityUserId,
        CancellationToken ct
    )
    {
        return await _identityContext
            .UserRoles.Where(ur => ur.UserId == identityUserId)
            .Join(_identityContext.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
            .ToListAsync(ct);
    }

    /*
        //?     Derives the user's effective permissions from their roleNames.
        //?     For each identityRole the user belongs to, fetch all claims of type
        //?     [CustomClaimTypes.Permission] ("permission") from the identityRole.
        //?     The result is a deduplicated union across all roleNames.
        //
        //!     Returns an empty list when the user has no roleNames or no permission claims exist.
        //!     Never returns permissions stored directly on the user — RBAC only.
        //
        //>     To assign a permission to a identityRole (admin tooling or seeder):
        //>       var identityRole = await roleManager.FindByNameAsync("Manager");
        //>       await roleManager.AddClaimAsync(identityRole, new Claim("permission", "users:read"));
    */
    public async Task<IReadOnlyList<string>> GetPermissionsAsync(
        string identityUserId,
        CancellationToken ct
    )
    {
        (_, IReadOnlyList<string> permissions) = await LoadRolesAndPermissionsAsync(
            identityUserId,
            ct
        );
        return permissions;
    }

    public async Task<Result> AddToRoleAsync(
        string identityUserId,
        string roleName,
        CancellationToken ct
    )
    {
        AppUser? appUser = await _userManager.FindByIdAsync(identityUserId);

        if (appUser is null)
        {
            return Error.NotFound("Identity.UserNotFound", "The user account was not found.");
        }

        if (await _userManager.IsInRoleAsync(appUser, roleName))
        {
            return Result.Success();
        }

        IdentityResult identityResult = await _userManager.AddToRoleAsync(appUser, roleName);

        if (!identityResult.Succeeded)
        {
            var errors = identityResult
                .Errors.Select(e => Error.Validation($"Identity.{e.Code}", e.Description))
                .ToList();

            return errors;
        }

        return Result.Success();
    }

    public async Task<Result> SetRoleAsync(
        string identityUserId,
        string roleName,
        CancellationToken ct
    )
    {
        AppUser? appUser = await _userManager.FindByIdAsync(identityUserId);

        if (appUser is null)
        {
            return Error.NotFound("Identity.UserNotFound", "The user account was not found.");
        }

        IList<string> currentRoles = await _userManager.GetRolesAsync(appUser);

        /*
            //?     Remove-then-add keeps exactly one role per user.
            //!     Removal happens first — if the add fails (unknown role), the user is left
            //!     role-less rather than double-roled; the admin panel surfaces the error and
            //!     the operation can simply be retried with a valid role.
        */
        if (currentRoles.Count > 0)
        {
            IdentityResult removeResult = await _userManager.RemoveFromRolesAsync(
                appUser,
                currentRoles
            );

            if (!removeResult.Succeeded)
            {
                var errors = removeResult
                    .Errors.Select(e => Error.Validation($"Identity.{e.Code}", e.Description))
                    .ToList();

                return errors;
            }
        }

        IdentityResult addResult = await _userManager.AddToRoleAsync(appUser, roleName);

        if (!addResult.Succeeded)
        {
            var errors = addResult
                .Errors.Select(e => Error.Validation($"Identity.{e.Code}", e.Description))
                .ToList();

            return errors;
        }

        return Result.Success();
    }

    // ── Account status ───────────────────────────────────────────────────────

    public async Task<bool> IsActiveAsync(string identityUserId, CancellationToken ct)
    {
        /*
            //?     Projects to a single [bool?] column — no full entity load.
            //*     The global query filter [!e.IsDeleted] stays active: a soft-deleted user
            //*     is excluded from the result and falls through to the [?? false] fallback,
            //*     which blocks them correctly.
            //!     [null] covers two cases: soft-deleted (filtered out) and missing record
            //!     (data inconsistency). Both should block — [?? false] handles both.
        */
        bool? isActive = await _applicationContext
            .Users.AsNoTracking()
            .Where(u => u.IdentityId == identityUserId)
            .Select(u => (bool?)u.IsActive)
            .FirstOrDefaultAsync(ct);

        return isActive ?? false;
    }

    // ── Password reset ────────────────────────────────────────────────────────

    public async Task<string> GeneratePasswordResetTokenAsync(
        string identityUserId,
        CancellationToken ct
    )
    {
        AppUser appUser =
            await _userManager.FindByIdAsync(identityUserId)
            ?? throw new InvalidOperationException(
                $"Identity user '{identityUserId}' not found. Domain user exists but Identity record is missing."
            );

        return await _userManager.GeneratePasswordResetTokenAsync(appUser);
    }

    public async Task<Result> ResetPasswordAsync(
        string identityUserId,
        string token,
        string newPassword,
        CancellationToken ct
    )
    {
        AppUser? appUser = await _userManager.FindByIdAsync(identityUserId);

        if (appUser is null)
        {
            return UserErrors.InvalidToken;
        }

        IdentityResult identityResult = await _userManager.ResetPasswordAsync(
            appUser,
            token,
            newPassword
        );

        if (!identityResult.Succeeded)
        {
            /*
                //?     Map each [IdentityError] to our typed [Error] format.
                //!     Identity distinguishes between "invalid token" and "password policy" errors —
                //!     both come back here mapped to Validation errors with their specific codes.
            */
            var errors = identityResult
                .Errors.Select(e => Error.Validation($"Identity.{e.Code}", e.Description))
                .ToList();

            return errors;
        }

        return Result.Success();
    }

    // ── Email confirmation ────────────────────────────────────────────────────

    public async Task<string> GenerateEmailConfirmationTokenAsync(
        string identityUserId,
        CancellationToken ct
    )
    {
        AppUser appUser =
            await _userManager.FindByIdAsync(identityUserId)
            ?? throw new InvalidOperationException(
                $"Identity user '{identityUserId}' not found. Domain user exists but Identity record is missing."
            );

        return await _userManager.GenerateEmailConfirmationTokenAsync(appUser);
    }

    public async Task<Result> ConfirmEmailAsync(
        string identityUserId,
        string token,
        CancellationToken ct
    )
    {
        AppUser? appUser = await _userManager.FindByIdAsync(identityUserId);

        if (appUser is null)
        {
            return UserErrors.InvalidToken;
        }

        IdentityResult identityResult = await _userManager.ConfirmEmailAsync(appUser, token);

        if (!identityResult.Succeeded)
        {
            var errors = identityResult
                .Errors.Select(e => Error.Validation($"Identity.{e.Code}", e.Description))
                .ToList();

            return errors;
        }

        return Result.Success();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /*
        //!     Superseded by [LoadRolesAndPermissionsAsync] (Dapper, raw SQL).
        //!     Kept for reference — shows the EF Core LINQ equivalent of the same query.
        //?     The Dapper version is preferred: it bypasses the EF change tracker, avoids
        //?     DbContext state concerns, and makes the JOIN structure explicit in SQL.
    */
    [Obsolete("Superseded by [LoadRolesAndPermissionsAsync] - USED Dapper, raw SQL more clear.")]
    private async Task<(
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions
    )> LoadRolesAndPermissionsWithLinqAsync(string identityUserId, CancellationToken ct)
    {
        /*
            //!         // INNER JOIN — roles with NO claims are dropped entirely
            //!        ``` join rc in _identityContext.RoleClaims on r.Id equals rc.RoleId ```
            //-------------------------------------------------------
            //!        // LEFT JOIN — roles with NO claims are still included (rc will be null)
            //!        ``` join rc in _identityContext.RoleClaims on r.Id equals rc.RoleId into claims
            //!             from rc in claims.DefaultIfEmpty() ```
         */
        var rows = await (
            from ur in _identityContext.UserRoles //* start: user-role junction table
            join r in _identityContext.Roles on ur.RoleId equals r.Id //* bring in the role
            join rc in _identityContext.RoleClaims on r.Id equals rc.RoleId into claims //* try to bring in claims for that role <--> [into claims]  ← GROUP the matching claims into a collection.
            from rc in claims.DefaultIfEmpty()
            where ur.UserId == identityUserId
            select new
            {
                RoleName = r.Name,
                rc.ClaimType,
                rc.ClaimValue,
            }
        ).ToListAsync(ct);

        IReadOnlyList<string> roles = rows.Select(x => x.RoleName!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        IReadOnlyList<string> permissions = rows.Where(x =>
                x.ClaimType == CustomClaimTypes.Permission && x.ClaimValue is not null
            )
            .Select(x => x.ClaimValue!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return (roles, permissions);
    }

    //* Shape of columns BEFORE splitOn
    private sealed record RoleRow(string Name);

    //* Shape of columns FROM splitOn onwards
    //* Nullable: LEFT JOIN means these come back NULL when a role has zero claims
    private sealed record RoleClaimRow(string? ClaimType, string? ClaimValue);

    private async Task<(
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions
    )> LoadRolesAndPermissionsAsync(string identityUserId, CancellationToken ct)
    {
        const string sqlQuery = """
                SELECT [Identity].Roles.Name, [Identity].RoleClaims.ClaimType, [Identity].RoleClaims.ClaimValue
                FROM   [Identity].UserRoles INNER JOIN
                            [Identity].Roles ON [Identity].UserRoles.RoleId = [Identity].Roles.Id LEFT OUTER JOIN
                            [Identity].RoleClaims ON [Identity].Roles.Id = [Identity].RoleClaims.RoleId
                WHERE ([Identity].UserRoles.UserId = @IdentityUserId)
            """;

        using IDbConnection connection = _sqlConnectionFactory.CreateConnection();

        IEnumerable<(RoleRow Role, RoleClaimRow? Claim)> rows = await connection.QueryAsync<
            RoleRow,
            RoleClaimRow,
            (RoleRow, RoleClaimRow?)
        >(
            new CommandDefinition(
                commandText: sqlQuery,
                parameters: new { IdentityUserId = identityUserId },
                cancellationToken: ct
            ),
            map: (role, claim) => (role, claim), //! just pair — don't group here; I don't want to composite both into 1 object.
            splitOn: "ClaimType"
        );

        IReadOnlyList<string> roles = rows.Select(row => row.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        IReadOnlyList<string> permissions = rows.Where(row =>
                row.Claim?.ClaimType == CustomClaimTypes.Permission
                && row.Claim?.ClaimValue is not null
            )
            .Select(row => row.Claim!.ClaimValue!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return (roles, permissions);
    }
}
