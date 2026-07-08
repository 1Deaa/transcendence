using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Tenancy;

/*
    //?     Resolves the tenant (company workspace) the current operation belongs to.
    //
    //?     Two resolution sources, in priority order:
    //?      1. An explicit ambient scope opened via BeginScope() — used by background jobs
    //?         (CSV imports, seeding) that have no HTTP request to read claims from.
    //?      2. The "tenant_id" claim on the authenticated principal (JWT), via IHttpContextAccessor.
    //
    //!     Current is [null] for host-level operations (platform admins, health probes,
    //!     backups) — those must never touch tenant-owned aggregates.
    //
    //>     using (tenantContext.BeginScope(job.TenantId))
    //>     {
    //>         await unitOfWork.SaveChangesAsync(ct); //? writes are stamped with job.TenantId
    //>     }
*/
public interface ITenantContext
{
    TenantId? Current { get; }

    IDisposable BeginScope(TenantId tenantId);
}
