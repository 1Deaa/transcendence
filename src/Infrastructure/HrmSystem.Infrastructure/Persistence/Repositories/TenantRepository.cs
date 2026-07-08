using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class TenantRepository : ABaseRepository<Tenant, TenantId>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext dbContext)
        : base(dbContext) { }

    public async Task<Tenant?> FindBySlugAsync(string slug, CancellationToken ct)
    {
        //? Owned-type predicate translates to a WHERE on the unique Slug column (index seek).
        return await DbContext
            .Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Slug.Value == slug, ct);
    }
}
