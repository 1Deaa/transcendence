using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Departments.ValueObjects;

/*
    //?     Strongly-typed, lexicographically-sortable identity of a Department aggregate.
    //?     Format: "dept-{UUIDv7}" — New() always valid; From() validates untrusted input.
    //!     The constructor is [private] — use New() or From() to get an instance.
*/
public sealed record DepartmentId
{
    #region Constants

    public const string Prefix = "dept-";

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private DepartmentId(string value)
    {
        Value = value;
    }

    #endregion

    #region Factories

    public static DepartmentId New() =>
        new(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    public static Result<DepartmentId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DepartmentErrors.Id.Invalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return DepartmentErrors.Id.InvalidFormat;
        }

        return new DepartmentId(value);
    }

    #endregion

    #region Overrides

    public override string ToString() => Value;

    #endregion
}
