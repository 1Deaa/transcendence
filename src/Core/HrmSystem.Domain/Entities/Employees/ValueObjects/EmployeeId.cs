using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Employees.ValueObjects;

/*
    //?     Strongly-typed, lexicographically-sortable identity of an Employee aggregate.
    //?     Format: "emp-{UUIDv7}" — New() always valid; From() validates untrusted input.
    //!     The constructor is [private] — use New() or From() to get an instance.
*/
public sealed record EmployeeId
{
    #region Constants

    public const string Prefix = "emp-";

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private EmployeeId(string value)
    {
        Value = value;
    }

    #endregion

    #region Factories

    public static EmployeeId New() => new(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    public static Result<EmployeeId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EmployeeErrors.Id.Invalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return EmployeeErrors.Id.InvalidFormat;
        }

        return new EmployeeId(value);
    }

    #endregion

    #region Overrides

    public override string ToString() => Value;

    #endregion
}
