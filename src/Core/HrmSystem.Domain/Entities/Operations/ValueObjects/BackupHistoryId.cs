using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Operations.ValueObjects;

/*
    //?     Strongly-typed identity of a BackupHistory row.
    //?     Format: "backup-{UUIDv7}" — New() always valid; From() validates untrusted input.
*/
public sealed record BackupHistoryId
{
    #region Constants

    public const string Prefix = "backup-";

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private BackupHistoryId(string value)
    {
        Value = value;
    }

    #endregion

    #region Factories

    public static BackupHistoryId New() =>
        new(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    public static Result<BackupHistoryId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return OperationsErrors.BackupHistoryIdInvalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return OperationsErrors.BackupHistoryIdInvalidFormat;
        }

        return new BackupHistoryId(value);
    }

    #endregion

    #region Overrides

    public override string ToString() => Value;

    #endregion
}
