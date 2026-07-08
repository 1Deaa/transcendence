using FluentValidation;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Departments.ValueObjects;

namespace HrmSystem.Application.Features.Departments.AddDepartment;

/*
    //?     First line of defence — rejects malformed input before the handler runs.
    //!     The domain Create factory re-validates authoritatively; error codes are shared
    //!     so both layers emit identical codes for the same rule.
*/
internal sealed class AddDepartmentCommandValidator : AbstractValidator<AddDepartmentCommand>
{
    public AddDepartmentCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithErrorCode(DepartmentErrors.Name.Required.Code)
            .WithMessage(DepartmentErrors.Name.Required.Description)
            .MaximumLength(DepartmentName.MaxLength)
            .WithErrorCode(DepartmentErrors.Name.TooLong.Code)
            .WithMessage(DepartmentErrors.Name.TooLong.Description);

        RuleFor(c => c.Code)
            .NotEmpty()
            .WithErrorCode(DepartmentErrors.Code.Required.Code)
            .WithMessage(DepartmentErrors.Code.Required.Description);
    }
}
