using FluentValidation;

namespace HrmSystem.Application.Features.Tenants.RegisterCompany;

internal sealed class RegisterCompanyCommandValidator : AbstractValidator<RegisterCompanyCommand>
{
    public RegisterCompanyCommandValidator()
    {
        RuleFor(c => c.CompanyName).NotEmpty();
        RuleFor(c => c.Slug).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty();
        RuleFor(c => c.LastName).NotEmpty();
        RuleFor(c => c.UserName).NotEmpty();
        RuleFor(c => c.Email).NotEmpty().EmailAddress();

        //! Fast pre-check before any DB call — Identity enforces its full policy on top.
        RuleFor(c => c.Password).NotEmpty().MinimumLength(6);

        RuleFor(c => c.ConfirmPassword)
            .Equal(c => c.Password)
            .WithMessage("Password and confirmation password do not match.");
    }
}
