using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants;

namespace HrmSystem.Application.Features.Tenants.RegisterTenant;

internal sealed class RegisterTenantCommandHandler(
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<RegisterTenantCommand, string>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<string>> Handle(
        RegisterTenantCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<Tenant> tenantResult = Tenant.Create(command.CompanyName, command.Slug);
        if (tenantResult.IsFailure)
        {
            return tenantResult.Errors.ToList();
        }

        //! Uniqueness check uses the VALIDATED slug (trimmed + lowercased by the VO).
        Tenant tenant = tenantResult.Value;
        Tenant? existing = await _tenantRepository.FindBySlugAsync(
            tenant.Slug.Value,
            cancellationToken
        );
        if (existing is not null)
        {
            return TenantErrors.DuplicateSlug(tenant.Slug.Value);
        }

        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tenant.Id!.Value;
    }
}
