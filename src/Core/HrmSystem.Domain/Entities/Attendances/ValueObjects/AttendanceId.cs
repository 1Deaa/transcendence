using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Attendances.ValueObjects;

/*
    //?     Strongly-typed, lexicographically-sortable identity of an Attendance aggregate.
    //?     Format: "att-{UUIDv7}" — New() always valid; From() validates untrusted input.
    //!     The constructor is [private] — use New() or From() to get an instance.
*/
public sealed record AttendanceId
{
    #region Constants

    public const string Prefix = "att-";

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private AttendanceId(string value)
    {
        Value = value;
    }

    #endregion

    #region Factories

    public static AttendanceId New() =>
        new(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    public static Result<AttendanceId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AttendanceErrors.Id.Invalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return AttendanceErrors.Id.InvalidFormat;
        }

        return new AttendanceId(value);
    }

    #endregion

    #region Overrides

    public override string ToString() => Value;

    #endregion
}
