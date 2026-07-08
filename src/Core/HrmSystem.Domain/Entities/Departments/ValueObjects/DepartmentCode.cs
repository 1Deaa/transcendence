using System.Text.RegularExpressions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Departments.ValueObjects;

/*
    //?     Short human-readable department code used in reports and payroll exports ("HR", "ENG-2").
    //?     2–10 characters: uppercase letters/digits with optional single hyphen separators.
    //?     Input is trimmed and uppercased before validation.
*/
public sealed partial record DepartmentCode
{
    #region Constants

    public const int MinLength = 2;
    public const int MaxLength = 10;

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private DepartmentCode(string value)
    {
        Value = value;
    }

    #endregion

    #region Factory

    //? Creates a validated DepartmentCode — collects all errors before returning.
    public static Result<DepartmentCode> Create(string? value)
    {
        var errors = new List<Error>();
        value = value?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(DepartmentErrors.Code.Required);
        }
        else
        {
            if (value.Length is < MinLength or > MaxLength)
            {
                errors.Add(DepartmentErrors.Code.InvalidLength);
            }

            if (!CodeRegex().IsMatch(value))
            {
                errors.Add(DepartmentErrors.Code.InvalidFormat);
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new DepartmentCode(value!);
    }

    #endregion

    [GeneratedRegex("^[A-Z0-9]+(-[A-Z0-9]+)*$")]
    private static partial Regex CodeRegex();
}
