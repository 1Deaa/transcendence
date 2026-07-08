using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Operations.ValueObjects;

namespace HrmSystem.Domain.Entities.Operations;

/*
    //?     Error catalog for the host-level Operations entities (backups + health snapshots).
    //>     Codes: "Operations.<Reason>" — stable once shipped.
*/
public static class OperationsErrors
{
    public static readonly Error BackupNotFound = Error.NotFound(
        "Operations.BackupNotFound",
        "The backup history record was not found."
    );

    public static readonly Error BackupAlreadyFinished = Error.Conflict(
        "Operations.BackupAlreadyFinished",
        "The backup has already finished and cannot change state again."
    );

    public static readonly Error BackupHistoryIdInvalid = Error.Validation(
        "Operations.BackupHistoryId.Invalid",
        "The backup history identifier must be a non-empty string."
    );

    public static readonly Error BackupHistoryIdInvalidFormat = Error.Validation(
        "Operations.BackupHistoryId.InvalidFormat",
        $"The backup history identifier must start with the '{BackupHistoryId.Prefix}' prefix."
    );

    public static readonly Error SnapshotIdInvalid = Error.Validation(
        "Operations.SnapshotId.Invalid",
        "The health snapshot identifier must be a non-empty string."
    );

    public static readonly Error SnapshotIdInvalidFormat = Error.Validation(
        "Operations.SnapshotId.InvalidFormat",
        $"The health snapshot identifier must start with the '{HealthCheckSnapshotId.Prefix}' prefix."
    );
}
