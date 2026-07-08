namespace HrmSystem.Infrastructure.Persistence.Configurations;

/*
    //*     Central registry of every database identifier used in EF Core configuration.
    //*     All schema names, table names, and (when needed) column names live here.
    //
    //?     Configuration classes must reference these constants — never inline a raw string.
*/
internal static class DbConfigurationSettings
{
    internal static class Schemas
    {
        internal const string Application = "HrmSystem";
        internal const string Identity = "Identity";
    }

    internal static class Tables
    {
        internal const string Users = "Users";
        internal const string EmailTemplates = "EmailTemplates";
        internal const string RefreshTokens = "RefreshTokens";

        // ── HRM aggregates ────────────────────────────────────────────────────
        internal const string Tenants = "Tenants";
        internal const string Departments = "Departments";
        internal const string Employees = "Employees";
        internal const string Attendances = "Attendances";
        internal const string LeaveRequests = "LeaveRequests";
    }
}
