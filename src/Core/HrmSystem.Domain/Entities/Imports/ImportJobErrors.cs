using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Imports.ValueObjects;

namespace HrmSystem.Domain.Entities.Imports;

/*
    //?     Error catalog for the ImportJob aggregate.
    //>     Codes: "ImportJob.<Reason>" — stable once shipped.
*/
public static class ImportJobErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "ImportJob.NotFound",
        "The import job was not found."
    );

    public static readonly Error NotPending = Error.Conflict(
        "ImportJob.NotPending",
        "Only a pending import job can start processing."
    );

    public static readonly Error NotProcessing = Error.Conflict(
        "ImportJob.NotProcessing",
        "Progress and completion can only be recorded while the job is processing."
    );

    public static readonly Error FileRequired = Error.Validation(
        "ImportJob.FileRequired",
        "A non-empty CSV file is required."
    );

    public static readonly Error InvalidEntityType = Error.Validation(
        "ImportJob.InvalidEntityType",
        "A valid import entity type must be specified."
    );

    public static class Id
    {
        public static readonly Error Invalid = Error.Validation(
            "ImportJob.Id.Invalid",
            "The import job identifier must be a non-empty string."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "ImportJob.Id.InvalidFormat",
            $"The import job identifier must start with the '{ImportJobId.Prefix}' prefix."
        );
    }
}
