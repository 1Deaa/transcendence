using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Employees.ValueObjects;

/*
    //?     First + last name pair, each trimmed, non-empty and max PartMaxLength characters.
    //?     Structural equality via [sealed record] — two names match when both parts match.
*/
public sealed record PersonName
{
    #region Constants

    public const int PartMaxLength = 100;

    #endregion

    #region Properties

    public string FirstName { get; }
    public string LastName { get; }

    //? Convenience display form ("Jane Smith") — not persisted, computed on read.
    public string FullName => $"{FirstName} {LastName}";

    #endregion

    #region Constructor

    private PersonName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    #endregion

    #region Factory

    //? Creates a validated PersonName — collects all errors before returning.
    public static Result<PersonName> Create(string? firstName, string? lastName)
    {
        var errors = new List<Error>();
        firstName = firstName?.Trim();
        lastName = lastName?.Trim();

        if (string.IsNullOrWhiteSpace(firstName))
        {
            errors.Add(EmployeeErrors.Name.FirstNameRequired);
        }
        else if (firstName.Length > PartMaxLength)
        {
            errors.Add(EmployeeErrors.Name.FirstNameTooLong);
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            errors.Add(EmployeeErrors.Name.LastNameRequired);
        }
        else if (lastName.Length > PartMaxLength)
        {
            errors.Add(EmployeeErrors.Name.LastNameTooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new PersonName(firstName!, lastName!);
    }

    #endregion
}
