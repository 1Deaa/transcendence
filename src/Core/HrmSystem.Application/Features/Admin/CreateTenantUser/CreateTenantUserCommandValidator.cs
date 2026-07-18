using FluentValidation;

namespace HrmSystem.Application.Features.Admin.CreateTenantUser;

internal sealed class CreateTenantUserCommandValidator
    : AbstractValidator<CreateTenantUserCommand>
{
    public CreateTenantUserCommandValidator()
    {
        RuleFor(c => c.UserName).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty();
        RuleFor(c => c.LastName).NotEmpty();
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.Password).NotEmpty().MinimumLength(6);
        RuleFor(c => c.Role).NotEmpty();
    }
}
