using HrmSystem.Infrastructure.Persistence.Configurations;
using HrmSystem.Infrastructure.Services.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Contexts;

public sealed class ApplicationIdentityDbContext : IdentityDbContext
{
    #region Exist Built-in DbSets Examples that I can use

    //>         _dbContext.Users           // IdentityUser
    //>         _dbContext.Roles           // IdentityRole
    //>         _dbContext.UserRoles       // IdentityUserRole<string>
    //>         _dbContext.UserClaims      // IdentityUserClaim<string>
    //>         _dbContext.RoleClaims      // IdentityRoleClaim<string>
    //>         _dbContext.UserLogins      // IdentityUserLogin<string>
    //>         _dbContext.UserTokens      // IdentityUserToken<string>

    #endregion Exist Built-in DbSets Examples that I can use


    public ApplicationIdentityDbContext(
        DbContextOptions<ApplicationIdentityDbContext> dbContextOptions
    )
        : base(dbContextOptions) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        /*
            //! This (OnModelCreating(...)) if for configuring [[Tables of Identity]] ;
            //!     - So calling the (base.OnModelCreating(builder)) is very important to wire up the [[built-in Tables]]
        */
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(DbConfigurationSettings.Schemas.Identity);

        /*
            //!  [AppUser.DomainUser] is a navigation to the domain [User] aggregate.
            //!  Ignoring it here prevents EF Core from auto-discovering [User] and all its
            //!  owned types / navigations ([Habits], [Description], etc.) into this context.
            //*  The 1:1 FK relationship is configured in [ApplicationDbContext] via [UserConfiguration]
            //*  where [AppUser] is registered with [ExcludeFromMigrations()].
        */
        builder.Entity<AppUser>().Ignore(au => au.DomainUser);

        /*
            //! - Custom Identity Tables Names
            //?     - Non-generic [IdentityUser] / [IdentityRole] use the default [string key]
            //?     - The junction/claim/token tables are always generic, so you must specify <string> explicitly
        */
        builder.Entity<IdentityUser>(e => e.ToTable("IdentityUsers"));

        /*
            //?     - [[IdentityRole]] has the following Columns in Db:
            //!         - [[Id]]: Unique identifier for each role (usually a GUID).
            //!         - [[Name]]: Human-readable name of the role (e.g., “Admin”).
            //!         - [[NormalizedName]]: Uppercase version of the role name for case-insensitive matching.
            //!         - [[ConcurrencyStamp]]: Value that changes on updates, used for concurrency control.
         */
        builder.Entity<IdentityRole>(e => e.ToTable("Roles"));

        /*
            //?     - [[IdentityRoleClaim]] has the following Columns in Db:
            //!         - [[Id]]: Unique integer identifier for the role claim.
            //!         - [[RoleId]]: The role ID associated with this claim. A foreign key that links to the Id column in the AspNetRoles table.
            //!         - [[ClaimType]]: This column stores the type of the claim, such as Permission, AccessLevel, or any other type that makes sense in the context of your application.
            //!         - [[ClaimValue]]: The actual value of the claim. For example, if the claim type is Permission, the claim value might be Edit_User or View_Reports, etc.
            //>     - (((Relationships))): Each claim entry is associated with a specific role. All users in that role will inherit the claims defined here.
         */
        builder.Entity<IdentityRoleClaim<string>>(e => e.ToTable("RoleClaims"));

        /*
            //?     - [[IdentityUserRole]] has the following Columns in Db:
            //!         - [[UserId]]: The user ID from the AspNetUsers table. Part of the composite primary key. It corresponds to the Id column in the AspNetUsers table. It acts as a foreign key linking to the AspNetUsers table.
            //!         - [[RoleId]]: The role ID from the AspNetRoles table. Part of the composite primary key. It corresponds to the Id column in the AspNetRoles table. It acts as a foreign key linking to the AspNetRoles table.
            //>         - (((Relationships))): This table connects (AspNetUsers) and (AspNetRoles), making it possible to look up all roles for a given user or all users for a given role.
         */
        builder.Entity<IdentityUserRole<string>>(e => e.ToTable("UserRoles"));

        /*
            //?     - [[IdentityUserClaim]] has the following Columns in Db:
            //!            - [[Id]]: The primary key for the user claim. It is an integer.
            //!            - [[UserId]]: The ID of the user associated with this claim. It acts as a foreign key linking to the Id column-  in the AspNetUsers table.
            //!            - [[ClaimType]]: The type of the claim (e.g., “birthdate”).
            //!            - [[ClaimValue]]: The value of the claim (e.g., “1980-01-01”).
            //>     - (((Relationships))): Each claim entry links to a user and defines additional properties or access rights unique to that user.
        */
        builder.Entity<IdentityUserClaim<string>>(e => e.ToTable("UserClaims"));

        /*
            //?     - [[IdentityUserLogin]] has the following Columns in Db:
            //!         - [[LoginProvider]]: This column stores the name of the external authentication provider (e.g., Google, Facebook, Microsoft, etc.). It is part of the composite primary key for the table.
            //!         - [[ProviderKey]]: This column stores the unique identifier provided by the external login provider for the user. For example, when a user logs in using Google, this field will store the unique ID assigned to the user by Google. It is also part of the composite primary key.
            //!         - [[ProviderDisplayName]]: This optional column can store the display name of the login provider (e.g., “Google” instead of “google.com”). It is mainly used for display purposes in the UI.
            //!         - [[UserId]]: The local user ID that is linked to this login. It acts as a foreign key linking to the Id column in the AspNetUsers table.
            //>         - (((Relationships))): Each entry in this table connects a user (UserId) to an external authentication provider, allowing users to log in using social logins.
        */
        builder.Entity<IdentityUserLogin<string>>(e => e.ToTable("UserLogins"));

        /*
            //?     - [[IdentityUserToken]] has the following Columns in Db:
            //!         - [[UserId]]: The User ID from the AspNetUsers table.
            //!         - [[LoginProvider]]: Name of the provider that generated the token.
            //!         - [[Name]]: This column stores the name of the token, such as an email confirmation token, password reset token, or two-factor authentication token. It could be something like PasswordReset, EmailConfirmation, AccessToken, etc.
            //!         - [[Value]]: The value of the token.
            //>         - (((Relationships))): Each token entry is tied to a user and is typically used for security operations or workflow steps that require a one-time token.
        */
        builder.Entity<IdentityUserToken<string>>(e => e.ToTable("UserTokens"));
    }
}
