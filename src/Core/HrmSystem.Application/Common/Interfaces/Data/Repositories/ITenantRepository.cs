using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface ITenantRepository : IBaseRepository<Tenant, TenantId>
{
    //? Slug lookup for registration duplicate checks and workspace resolution.
    //! Slug comparison is case-insensitive by construction — TenantSlug lowercases on Create.
    Task<Tenant?> FindBySlugAsync(string slug, CancellationToken ct);
}
