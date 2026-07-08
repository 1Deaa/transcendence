namespace HrmSystem.Application.Common.Authorization;

/*
    //?     Single source of truth for every permission string in the system.
    //?     Used in three places:
    //?       1. Seeder — assigns these strings as role claims to Identity roles.
    //?       2. [HasPermission] attribute — decorates controller actions.
    //?       3. Application handlers — when a handler needs to check a permission imperatively.
    //
    //*     Convention: "{resource}:{action}" — resource is plural, action is a verb.
    //*     All values are lowercase so the claim comparison is case-insensitive by convention.
    //
    //!     Once a permission string appears in a live token or a seeded role, renaming it
    //!     is a breaking change — all active tokens lose the permission and seeded role rows
    //!     must be migrated.  Treat these constants as stable public API.
*/
public static class Permissions
{
    // ── Users ────────────────────────────────────────────────────────────────

    public static class Users
    {
        public const string Read = "users:read";
        public const string Write = "users:write";
        public const string Modify = "users:modify";
        public const string Delete = "users:delete";
    }

    // ── Tenants (host-level administration) ─────────────────────────────────

    public static class Tenants
    {
        public const string Read = "tenants:read";
        public const string Manage = "tenants:manage";
    }

    // ── Departments ──────────────────────────────────────────────────────────

    public static class Departments
    {
        public const string Read = "departments:read";
        public const string Write = "departments:write";
        public const string Modify = "departments:modify";
        public const string Delete = "departments:delete";
    }

    // ── Employees ────────────────────────────────────────────────────────────

    public static class Employees
    {
        public const string Read = "employees:read";
        public const string Write = "employees:write";
        public const string Modify = "employees:modify";
        public const string Delete = "employees:delete";
    }

    // ── Attendance ───────────────────────────────────────────────────────────

    public static class Attendance
    {
        public const string Read = "attendance:read";
        public const string Write = "attendance:write";
    }

    // ── Leaves ───────────────────────────────────────────────────────────────

    public static class Leaves
    {
        public const string Read = "leaves:read";
        public const string Write = "leaves:write";
        public const string Approve = "leaves:approve";
    }

    // ── Analytics ────────────────────────────────────────────────────────────

    public static class Analytics
    {
        public const string Read = "analytics:read";
        public const string Export = "analytics:export";
    }

    // ── Health / status (detailed dashboard — the public page needs none) ───

    public static class Health
    {
        public const string Read = "health:read";
    }

    // ── Backups ──────────────────────────────────────────────────────────────

    public static class Backups
    {
        public const string Read = "backups:read";
        public const string Manage = "backups:manage";
    }

    // ── Imports / Exports ────────────────────────────────────────────────────

    public static class Imports
    {
        public const string Read = "imports:read";
        public const string Write = "imports:write";
    }

    public static class Exports
    {
        public const string Read = "exports:read";
    }

    // ── Admin ─────────────────────────────────────────────────────────────────

    public static class Admin
    {
        public const string Read = "admin:read";
        public const string ManageUsers = "admin:manage-users";
        public const string ManageRoles = "admin:manage-roles";
    }

    // ── Flat list ─────────────────────────────────────────────────────────────

    /*
        //?     All defined permissions as a flat list.
        //?     Used by [RolesAndPermissionsSeeder] to enumerate permissions when seeding roles.
    */
    public static IReadOnlyList<string> All =>
        [
            Users.Read,
            Users.Write,
            Users.Modify,
            Users.Delete,
            Tenants.Read,
            Tenants.Manage,
            Departments.Read,
            Departments.Write,
            Departments.Modify,
            Departments.Delete,
            Employees.Read,
            Employees.Write,
            Employees.Modify,
            Employees.Delete,
            Attendance.Read,
            Attendance.Write,
            Leaves.Read,
            Leaves.Write,
            Leaves.Approve,
            Analytics.Read,
            Analytics.Export,
            Health.Read,
            Backups.Read,
            Backups.Manage,
            Imports.Read,
            Imports.Write,
            Exports.Read,
            Admin.Read,
            Admin.ManageUsers,
            Admin.ManageRoles,
        ];
}
