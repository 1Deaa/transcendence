using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Departments.ValueObjects;

namespace HrmSystem.Domain.Entities.Departments;

/*
    //?     Error catalog for the Department aggregate — one nested class per Value Object.
    //>     Codes: "Department.<Reason>" / "Department.<ValueObject>.<Reason>" — stable once shipped.
*/
public static class DepartmentErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Department.NotFound",
        "The department was not found."
    );

    public static readonly Error HasEmployees = Error.Conflict(
        "Department.HasEmployees",
        "The department cannot be removed while employees are assigned to it."
    );

    public static Error DuplicateCode(string code) =>
        Error.Conflict(
            "Department.DuplicateCode",
            $"A department with the code '{code}' already exists."
        );

    public static class Id
    {
        public static readonly Error Invalid = Error.Validation(
            "Department.Id.Invalid",
            "The department identifier must be a non-empty string."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "Department.Id.InvalidFormat",
            $"The department identifier must start with the '{DepartmentId.Prefix}' prefix."
        );
    }

    public static class Name
    {
        public static readonly Error Required = Error.Validation(
            "Department.Name.Required",
            "The department name is required."
        );

        public static readonly Error TooLong = Error.Validation(
            "Department.Name.TooLong",
            $"The department name cannot exceed {DepartmentName.MaxLength} characters."
        );
    }

    public static class Code
    {
        public static readonly Error Required = Error.Validation(
            "Department.Code.Required",
            "The department code is required."
        );

        public static readonly Error InvalidLength = Error.Validation(
            "Department.Code.InvalidLength",
            $"The department code must be between {DepartmentCode.MinLength} and {DepartmentCode.MaxLength} characters."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "Department.Code.InvalidFormat",
            "The department code may only contain uppercase letters and digits separated by single hyphens."
        );
    }
}
