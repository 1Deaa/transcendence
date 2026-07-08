using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;

/*
    //?     Strongly-typed, lexicographically-sortable identity of a LeaveRequest aggregate.
    //?     Format: "leave-{UUIDv7}" — New() always valid; From() validates untrusted input.
    //!     The constructor is [private] — use New() or From() to get an instance.
*/
public sealed record LeaveRequestId
{
    #region Constants

    public const string Prefix = "leave-";

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private LeaveRequestId(string value)
    {
        Value = value;
    }

    #endregion

    #region Factories

    public static LeaveRequestId New() =>
        new(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    public static Result<LeaveRequestId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return LeaveRequestErrors.Id.Invalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return LeaveRequestErrors.Id.InvalidFormat;
        }

        return new LeaveRequestId(value);
    }

    #endregion

    #region Overrides

    public override string ToString() => Value;

    #endregion
}
