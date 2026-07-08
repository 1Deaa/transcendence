using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Departments.ValueObjects;

/*
    //?     A non-empty, trimmed department display name (max MaxLength characters).
    //!     The constructor is [private] — use Create() to get a validated instance.
*/
public sealed record DepartmentName
{
    #region Constants

    public const int MaxLength = 100;

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private DepartmentName(string value)
    {
        Value = value;
    }

    #endregion

    #region Factory

    //? Creates a validated DepartmentName — collects all errors before returning.
    public static Result<DepartmentName> Create(string? value)
    {
        var errors = new List<Error>();
        value = value?.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(DepartmentErrors.Name.Required);
        }
        else if (value.Length > MaxLength)
        {
            errors.Add(DepartmentErrors.Name.TooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new DepartmentName(value!);
    }

    #endregion
}
