using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Application.Features.Admin;

/*
    //?     Error catalog for the admin panel use cases (Application-level — these rules
    //?     concern account administration, not a domain aggregate).
*/
public static class AdminErrors
{
    public static readonly Error UserNotFound = Error.NotFound(
        "Admin.UserNotFound",
        "The user was not found in your workspace."
    );

    public static readonly Error RoleNotAssignable = Error.Validation(
        "Admin.RoleNotAssignable",
        "Only the TenantAdmin, Manager, and Employee roles can be assigned inside a workspace."
    );

    public static readonly Error CannotModifySelf = Error.Validation(
        "Admin.CannotModifySelf",
        "You cannot change your own role or deactivate your own account."
    );

    public static readonly Error TenantUnresolved = Error.Unauthorized(
        "Admin.TenantUnresolved",
        "The workspace could not be resolved from the current session."
    );
}
