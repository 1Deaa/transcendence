using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Domain.Entities.Employees;

/*
    //?     Error catalog for the Employee aggregate — one nested class per Value Object.
    //>     Codes: "Employee.<Reason>" / "Employee.<ValueObject>.<Reason>" — stable once shipped.
*/
public static class EmployeeErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Employee.NotFound",
        "The employee was not found."
    );

    public static readonly Error AlreadyTerminated = Error.Conflict(
        "Employee.AlreadyTerminated",
        "The employee's employment has already been terminated."
    );

    public static readonly Error TerminationBeforeHire = Error.Validation(
        "Employee.TerminationBeforeHire",
        "The termination date cannot be earlier than the hire date."
    );

    public static readonly Error NotActive = Error.Conflict(
        "Employee.NotActive",
        "The operation requires an active employee."
    );

    public static readonly Error NotOnLeave = Error.Conflict(
        "Employee.NotOnLeave",
        "The employee is not currently on leave."
    );

    public static Error DuplicateEmail(string email) =>
        Error.Conflict(
            "Employee.DuplicateEmail",
            $"An employee with the email '{email}' already exists."
        );

    public static class Id
    {
        public static readonly Error Invalid = Error.Validation(
            "Employee.Id.Invalid",
            "The employee identifier must be a non-empty string."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "Employee.Id.InvalidFormat",
            $"The employee identifier must start with the '{EmployeeId.Prefix}' prefix."
        );
    }

    public static class Name
    {
        public static readonly Error FirstNameRequired = Error.Validation(
            "Employee.Name.FirstNameRequired",
            "The first name is required."
        );

        public static readonly Error FirstNameTooLong = Error.Validation(
            "Employee.Name.FirstNameTooLong",
            $"The first name cannot exceed {PersonName.PartMaxLength} characters."
        );

        public static readonly Error LastNameRequired = Error.Validation(
            "Employee.Name.LastNameRequired",
            "The last name is required."
        );

        public static readonly Error LastNameTooLong = Error.Validation(
            "Employee.Name.LastNameTooLong",
            $"The last name cannot exceed {PersonName.PartMaxLength} characters."
        );
    }

    public static class Email
    {
        public static readonly Error Required = Error.Validation(
            "Employee.Email.Required",
            "The email address is required."
        );

        public static readonly Error TooLong = Error.Validation(
            "Employee.Email.TooLong",
            $"The email address cannot exceed {ValueObjects.Email.MaxLength} characters."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "Employee.Email.InvalidFormat",
            "The email address is not in a valid format."
        );
    }

    public static class JobTitle
    {
        public static readonly Error Required = Error.Validation(
            "Employee.JobTitle.Required",
            "The job title is required."
        );

        public static readonly Error TooLong = Error.Validation(
            "Employee.JobTitle.TooLong",
            $"The job title cannot exceed {ValueObjects.JobTitle.MaxLength} characters."
        );
    }
}
