using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Application.Common.Errors;

public static class AnalyticsErrors
{
    //! Host-level admins have no workspace — analytics is inherently tenant-scoped.
    public static readonly Error TenantRequired = Error.Forbidden(
        "Analytics.TenantRequired",
        "Analytics requires a tenant context — the caller does not belong to a workspace."
    );

    public static readonly Error InvalidDateRange = Error.Validation(
        "Analytics.InvalidDateRange",
        "The 'from' date must not be after the 'to' date, and the range cannot exceed 366 days."
    );

    public static Error UnsupportedFormat(string format) =>
        Error.Validation(
            "Analytics.UnsupportedFormat",
            $"'{format}' is not a supported export format (expected: csv, pdf)."
        );
}
