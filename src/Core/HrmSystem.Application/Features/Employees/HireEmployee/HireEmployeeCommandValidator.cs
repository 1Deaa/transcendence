using FluentValidation;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Application.Features.Employees.HireEmployee;

/*
    //?     First line of defence — rejects malformed input before the handler runs.
    //!     The domain Hire factory re-validates authoritatively with the same error codes.
*/
internal sealed class HireEmployeeCommandValidator : AbstractValidator<HireEmployeeCommand>
{
    public HireEmployeeCommandValidator()
    {
        RuleFor(c => c.FirstName)
            .NotEmpty()
            .WithErrorCode(EmployeeErrors.Name.FirstNameRequired.Code)
            .WithMessage(EmployeeErrors.Name.FirstNameRequired.Description)
            .MaximumLength(PersonName.PartMaxLength)
            .WithErrorCode(EmployeeErrors.Name.FirstNameTooLong.Code)
            .WithMessage(EmployeeErrors.Name.FirstNameTooLong.Description);

        RuleFor(c => c.LastName)
            .NotEmpty()
            .WithErrorCode(EmployeeErrors.Name.LastNameRequired.Code)
            .WithMessage(EmployeeErrors.Name.LastNameRequired.Description)
            .MaximumLength(PersonName.PartMaxLength)
            .WithErrorCode(EmployeeErrors.Name.LastNameTooLong.Code)
            .WithMessage(EmployeeErrors.Name.LastNameTooLong.Description);

        RuleFor(c => c.Email)
            .NotEmpty()
            .WithErrorCode(EmployeeErrors.Email.Required.Code)
            .WithMessage(EmployeeErrors.Email.Required.Description)
            .EmailAddress()
            .WithErrorCode(EmployeeErrors.Email.InvalidFormat.Code)
            .WithMessage(EmployeeErrors.Email.InvalidFormat.Description);

        RuleFor(c => c.JobTitle)
            .NotEmpty()
            .WithErrorCode(EmployeeErrors.JobTitle.Required.Code)
            .WithMessage(EmployeeErrors.JobTitle.Required.Description)
            .MaximumLength(JobTitle.MaxLength)
            .WithErrorCode(EmployeeErrors.JobTitle.TooLong.Code)
            .WithMessage(EmployeeErrors.JobTitle.TooLong.Description);

        RuleFor(c => c.DepartmentId).NotEmpty();
    }
}
