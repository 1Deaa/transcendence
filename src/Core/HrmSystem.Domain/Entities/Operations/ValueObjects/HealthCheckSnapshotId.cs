using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Operations.ValueObjects;

/*
    //?     Strongly-typed identity of a HealthCheckSnapshot row.
    //?     Format: "hcs-{UUIDv7}" — New() always valid; From() validates untrusted input.
*/
public sealed record HealthCheckSnapshotId
{
    #region Constants

    public const string Prefix = "hcs-";

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private HealthCheckSnapshotId(string value)
    {
        Value = value;
    }

    #endregion

    #region Factories

    public static HealthCheckSnapshotId New() =>
        new(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    public static Result<HealthCheckSnapshotId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return OperationsErrors.SnapshotIdInvalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return OperationsErrors.SnapshotIdInvalidFormat;
        }

        return new HealthCheckSnapshotId(value);
    }

    #endregion

    #region Overrides

    public override string ToString() => Value;

    #endregion
}
