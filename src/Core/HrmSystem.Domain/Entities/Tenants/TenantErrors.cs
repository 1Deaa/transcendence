using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Domain.Entities.Tenants;

/*
    //?     Error catalog for the Tenant aggregate — one nested class per Value Object,
    //?     plus aggregate-level errors at the root.
    //>     Convention: codes are "Tenant.<Reason>" or "Tenant.<ValueObject>.<Reason>",
    //>     stable forever once shipped (clients and logs depend on them).
*/
public static class TenantErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Tenant.NotFound",
        "The tenant was not found."
    );

    public static readonly Error AlreadyActive = Error.Conflict(
        "Tenant.AlreadyActive",
        "The tenant subscription is already active."
    );

    public static readonly Error AlreadySuspended = Error.Conflict(
        "Tenant.AlreadySuspended",
        "The tenant is already suspended."
    );

    public static Error DuplicateSlug(string slug) =>
        Error.Conflict("Tenant.DuplicateSlug", $"The workspace slug '{slug}' is already taken.");

    public static class Id
    {
        public static readonly Error Invalid = Error.Validation(
            "Tenant.Id.Invalid",
            "The tenant identifier must be a non-empty string."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "Tenant.Id.InvalidFormat",
            $"The tenant identifier must start with the '{TenantId.Prefix}' prefix."
        );
    }

    public static class CompanyName
    {
        public static readonly Error Required = Error.Validation(
            "Tenant.CompanyName.Required",
            "The company name is required."
        );

        public static readonly Error TooLong = Error.Validation(
            "Tenant.CompanyName.TooLong",
            $"The company name cannot exceed {ValueObjects.CompanyName.MaxLength} characters."
        );
    }

    public static class Slug
    {
        public static readonly Error Required = Error.Validation(
            "Tenant.Slug.Required",
            "The workspace slug is required."
        );

        public static readonly Error InvalidLength = Error.Validation(
            "Tenant.Slug.InvalidLength",
            $"The workspace slug must be between {TenantSlug.MinLength} and {TenantSlug.MaxLength} characters."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "Tenant.Slug.InvalidFormat",
            "The workspace slug may only contain lowercase letters and digits separated by single hyphens."
        );
    }
}
