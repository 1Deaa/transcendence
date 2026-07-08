using System.Security.Claims;
using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HrmSystem.Infrastructure.Persistence.Seeding.Seeders;

/*
    //*     Seeds the three default roles and their permission claims into the Identity store.
    //*     Idempotent — safe to call on every startup; skips already-seeded roles and claims.
    //
    //*     Role → Permission mapping (the RBAC configuration):
    //*
    //*       HostAdmin   → all permissions (platform staff: tenants, health detail, backups)
    //*       TenantAdmin → everything inside one company workspace (no tenants:*, no backups:manage)
    //*       Manager     → team operations: reads + attendance + leave approval + analytics
    //*       Employee    → self-service: own attendance + own leave requests
    //
    //!     [RunInProduction = true] — roles and permissions are required in every environment.
    //!     This seeder is the ONLY one that runs in Production; all others are dev-only test data.
    //
    //!     Changing this mapping in production requires a seeder re-run AND re-issuing
    //!     active tokens (they cache the old permission set until expiry).
*/
internal sealed class RolesAndPermissionsSeeder(
    RoleManager<IdentityRole> roleManager,
    ILogger<RolesAndPermissionsSeeder> logger
) : ISeeder
{
    //? Order: 5 — must run before UserSeeder (Order: 10) so roles exist when users are assigned.
    public int Order => 5;

    //! Override default [false] — roles belong in Production, unlike test data seeders.
    public bool RunInProduction => true;

    private static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        ["HostAdmin"] = [.. Permissions.All],

        ["TenantAdmin"] =
        [
            Permissions.Users.Read,
            Permissions.Users.Write,
            Permissions.Users.Modify,
            Permissions.Users.Delete,
            Permissions.Departments.Read,
            Permissions.Departments.Write,
            Permissions.Departments.Modify,
            Permissions.Departments.Delete,
            Permissions.Employees.Read,
            Permissions.Employees.Write,
            Permissions.Employees.Modify,
            Permissions.Employees.Delete,
            Permissions.Attendance.Read,
            Permissions.Attendance.Write,
            Permissions.Leaves.Read,
            Permissions.Leaves.Write,
            Permissions.Leaves.Approve,
            Permissions.Analytics.Read,
            Permissions.Analytics.Export,
            Permissions.Imports.Read,
            Permissions.Imports.Write,
            Permissions.Exports.Read,
            Permissions.Admin.Read,
            Permissions.Admin.ManageUsers,
            Permissions.Admin.ManageRoles,
        ],

        ["Manager"] =
        [
            Permissions.Users.Read,
            Permissions.Departments.Read,
            Permissions.Employees.Read,
            Permissions.Attendance.Read,
            Permissions.Attendance.Write,
            Permissions.Leaves.Read,
            Permissions.Leaves.Write,
            Permissions.Leaves.Approve,
            Permissions.Analytics.Read,
            Permissions.Analytics.Export,
            Permissions.Exports.Read,
        ],

        ["Employee"] =
        [
            Permissions.Users.Read,
            Permissions.Departments.Read,
            Permissions.Attendance.Read,
            Permissions.Attendance.Write,
            Permissions.Leaves.Read,
            Permissions.Leaves.Write,
        ],
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach ((string roleName, string[] permissions) in RolePermissions)
        {
            await EnsureRoleExistsAsync(roleName);
            await EnsurePermissionsAsync(roleName, permissions);
        }
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        IdentityResult result = await roleManager.CreateAsync(new IdentityRole(roleName));

        if (result.Succeeded)
        {
            logger.LogInformation("Created Identity role '{Role}'.", roleName);
        }
        else
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create role '{Role}': {Errors}", roleName, errors);
        }
    }

    private async Task EnsurePermissionsAsync(string roleName, string[] permissions)
    {
        IdentityRole? role = await roleManager.FindByNameAsync(roleName);

        if (role is null)
        {
            return;
        }

        IList<Claim> existing = await roleManager.GetClaimsAsync(role);

        foreach (string permission in permissions)
        {
            bool alreadySeeded = existing.Any(c =>
                c.Type == CustomClaimTypes.Permission && c.Value == permission
            );

            if (alreadySeeded)
            {
                continue;
            }

            IdentityResult result = await roleManager.AddClaimAsync(
                role,
                new Claim(CustomClaimTypes.Permission, permission)
            );

            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError(
                    "Failed to add permission '{Permission}' to role '{Role}': {Errors}",
                    permission,
                    roleName,
                    errors
                );
            }
        }

        logger.LogInformation(
            "Role '{Role}' has {Count} permission(s) seeded.",
            roleName,
            permissions.Length
        );
    }
}

//? Marker class — generic type parameter for ILogger<T> to give a clean log category.
//? Used by DatabaseInitialiser when calling legacy static seeder methods during transition.
public sealed class IdentitySeederLogCategory;
