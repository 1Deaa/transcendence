using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Employees.ValueObjects;

/*
    //?     A non-empty, trimmed job title (max MaxLength characters).
    //!     The constructor is [private] — use Create() to get a validated instance.
*/
public sealed record JobTitle
{
    #region Constants

    public const int MaxLength = 100;

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private JobTitle(string value)
    {
        Value = value;
    }

    #endregion

    #region Factory

    //? Creates a validated JobTitle — collects all errors before returning.
    public static Result<JobTitle> Create(string? value)
    {
        var errors = new List<Error>();
        value = value?.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(EmployeeErrors.JobTitle.Required);
        }
        else if (value.Length > MaxLength)
        {
            errors.Add(EmployeeErrors.JobTitle.TooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new JobTitle(value!);
    }

    #endregion
}
