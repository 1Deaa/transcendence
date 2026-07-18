namespace HrmSystem.Application.Common.Authorization;

/*
    //?     Single source of truth for every Identity role name.
    //?     The names must match [RolesAndPermissionsSeeder] exactly — a mismatch silently
    //?     produces a user with zero permissions.
    //!     Renaming a role after tokens are issued is a breaking change (role claims are
    //!     baked into live JWTs until they expire).
*/
public static class RoleNames
{
    public const string HostAdmin = "HostAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    //? Roles a tenant admin may assign inside their own workspace — HostAdmin is platform-only.
    public static IReadOnlyList<string> TenantAssignable => [TenantAdmin, Manager, Employee];
}
