using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Infrastructure.Persistence.Contexts;
using HrmSystem.Infrastructure.Services.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HrmSystem.Infrastructure.Persistence.Seeding.Seeders;

/*
    //*     Seeds KNOWN dev accounts — one host admin plus admin/manager/employee per tenant —
    //*     each backed by a real ASP.NET Identity account with a role assignment.
    //*     Every account can log in immediately; the JWT then carries role + permission +
    //*     tenant_id claims, which is exactly what the tenant-isolation verification needs.

    ?     Credentials (all with password [Seed1234]):
    ?       host.admin@seed.dev            → HostAdmin   (NO tenant claim)
    ?       admin@{slug}.seed.dev          → TenantAdmin (tenant-scoped)
    ?       manager@{slug}.seed.dev        → Manager     (tenant-scoped)
    ?       employee@{slug}.seed.dev       → Employee    (tenant-scoped)
    //
    //!     Two-store design: AppUser lives in ApplicationIdentityDbContext (Identity schema),
    //!     domain User lives in ApplicationDbContext (HrmSystem schema).
    //!     SetIdentityId bridges them — the domain User carries the AppUser PK.
    //
    //!     Domain User is HOST-LEVEL (no ATenantEntity) — tenant membership is plain data set
    //!     via AssignToTenant, so no ambient tenant scope is opened here.
*/
internal sealed class UserSeeder(
    ApplicationDbContext db,
    UserManager<AppUser> userManager,
    ILogger<UserSeeder> logger
) : ISeeder
{
    //? Order: 10 — after TenantSeeder (8) so accounts can be assigned to their workspaces.
    public int Order => 10;

    //! Dev-only default — intentionally weak so devs can log in easily.
    //! Never use this in any non-Development environment.
    private const string DevAccountPin = "Seed1234";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        List<Tenant> tenants = await db.Tenants.AsNoTracking().ToListAsync(cancellationToken);

        var accounts = new List<SeedAccount>
        {
            new("host-admin", "host.admin@seed.dev", "Hosty", "Admin", "HostAdmin", null),
        };

        foreach (Tenant tenant in tenants)
        {
            string slug = tenant.Slug.Value;
            accounts.Add(
                new SeedAccount(
                    $"admin-{slug}",
                    $"admin@{slug}.seed.dev",
                    "Tenant",
                    $"Admin {slug}",
                    "TenantAdmin",
                    tenant
                )
            );
            accounts.Add(
                new SeedAccount(
                    $"manager-{slug}",
                    $"manager@{slug}.seed.dev",
                    "Team",
                    $"Manager {slug}",
                    "Manager",
                    tenant
                )
            );
            accounts.Add(
                new SeedAccount(
                    $"employee-{slug}",
                    $"employee@{slug}.seed.dev",
                    "Staff",
                    $"Member {slug}",
                    "Employee",
                    tenant
                )
            );
        }

        var users = new List<User>(accounts.Count);

        foreach (SeedAccount account in accounts)
        {
            Result<User> userResult = User.Create(
                userName: account.UserName,
                firstName: account.FirstName,
                middleName: null,
                lastName: account.LastName,
                email: account.Email
            );

            if (userResult.IsFailure)
            {
                logger.LogWarning(
                    "Seed skipped — User.Create failed for '{Email}': {Errors}",
                    account.Email,
                    string.Join(", ", userResult.Errors.Select(e => e.Description))
                );
                continue;
            }

            /*
                //?     Check by email first — handles the partial-failure recovery case
                //?     where a previous run created the AppUser but crashed before SaveChangesAsync.
            */
            AppUser? appUser = await userManager.FindByEmailAsync(account.Email);

            if (appUser is null)
            {
                appUser = new AppUser { UserName = account.UserName, Email = account.Email };

                IdentityResult identityResult = await userManager.CreateAsync(
                    appUser,
                    DevAccountPin
                );

                if (!identityResult.Succeeded)
                {
                    logger.LogWarning(
                        "Seed skipped — UserManager.CreateAsync failed for '{Email}': {Errors}",
                        account.Email,
                        string.Join(", ", identityResult.Errors.Select(e => e.Description))
                    );
                    continue;
                }
            }

            if (!await userManager.IsInRoleAsync(appUser, account.Role))
            {
                IdentityResult roleResult = await userManager.AddToRoleAsync(
                    appUser,
                    account.Role
                );
                if (!roleResult.Succeeded)
                {
                    logger.LogWarning(
                        "Role '{Role}' could not be assigned to '{Email}': {Errors}",
                        account.Role,
                        account.Email,
                        string.Join(", ", roleResult.Errors.Select(e => e.Description))
                    );
                }
            }

            //! Bridge: domain User carries the AppUser PK so both stores stay linked.
            userResult.Value.SetIdentityId(appUser.Id);

            if (account.Tenant is not null)
            {
                userResult.Value.AssignToTenant(account.Tenant.Id!);
            }

            users.Add(userResult.Value);
        }

        if (users.Count == 0)
        {
            logger.LogWarning("UserSeeder produced no domain users — check warnings above.");
            return;
        }

        //! One SaveChangesAsync for the entire batch — never call it inside the loop.
        await db.Users.AddRangeAsync(users, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} users.", users.Count);
    }

    private sealed record SeedAccount(
        string UserName,
        string Email,
        string FirstName,
        string LastName,
        string Role,
        Tenant? Tenant
    );
}
