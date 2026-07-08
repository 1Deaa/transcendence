using FluentValidation;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Features.Tenants.RegisterTenant;

internal sealed class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        RuleFor(c => c.CompanyName)
            .NotEmpty()
            .WithErrorCode(TenantErrors.CompanyName.Required.Code)
            .WithMessage(TenantErrors.CompanyName.Required.Description)
            .MaximumLength(CompanyName.MaxLength)
            .WithErrorCode(TenantErrors.CompanyName.TooLong.Code)
            .WithMessage(TenantErrors.CompanyName.TooLong.Description);

        RuleFor(c => c.Slug)
            .NotEmpty()
            .WithErrorCode(TenantErrors.Slug.Required.Code)
            .WithMessage(TenantErrors.Slug.Required.Description);
    }
}
