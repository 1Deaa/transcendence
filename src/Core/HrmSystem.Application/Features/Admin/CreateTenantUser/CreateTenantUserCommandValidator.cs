using FluentValidation;
using HrmSystem.Application.Common.Validation;

namespace HrmSystem.Application.Features.Admin.CreateTenantUser;

internal sealed class CreateTenantUserCommandValidator
    : AbstractValidator<CreateTenantUserCommand>
{
    public CreateTenantUserCommandValidator()
    {
        RuleFor(c => c.UserName).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty();
        RuleFor(c => c.LastName).NotEmpty();
        RuleFor(c => c.Email).NotEmpty().StrictEmailAddress();
        RuleFor(c => c.Password).NotEmpty().StrongPassword();
        RuleFor(c => c.Role).NotEmpty();
    }
}
