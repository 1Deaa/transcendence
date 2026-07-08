using HrmSystem.Application.Common.Exceptions;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HrmSystem.Infrastructure.Persistence.Interceptors;

/*
    //?     Write-side tenant isolation — the twin of the read-side tenant query filter.
    //
    //?      - Added ITenantOwned with no TenantId yet   → stamped from ITenantContext.Current.
    //?      - Added with no ambient tenant at all       → CrossTenantWriteException (fail loudly).
    //?      - Any write whose TenantId ≠ Current        → CrossTenantWriteException.
    //
    //*     Domain factories stay tenant-agnostic (no TenantId parameter) — ownership is an
    //*     infrastructure concern stamped here, exactly once, on first save.
    //
    //!     Must be registered BEFORE AuditableEntityInterceptor in AddInterceptors(...) so a
    //!     cross-tenant save aborts before any audit stamping happens.
    //
    //!     The TenantId property has a private setter — writes go through the change-tracker
    //!     entry (entry.Property(...).CurrentValue), which EF permits for private setters.
*/
public sealed class TenantWriteGuardInterceptor(ITenantContext tenantContext)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        EnforceTenantOwnership(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        EnforceTenantOwnership(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void EnforceTenantOwnership(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        TenantId? currentTenant = tenantContext.Current;

        foreach (EntityEntry<ITenantOwned> entry in context.ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            //! ATenantEntity declares TenantId as [null!] until first save — null means "not stamped yet".
            TenantId? stampedTenant = entry.Entity.TenantId;

            if (entry.State == EntityState.Added && stampedTenant is null)
            {
                entry.Property(nameof(ITenantOwned.TenantId)).CurrentValue =
                    currentTenant
                    ?? throw new CrossTenantWriteException(
                        $"Cannot create '{entry.Metadata.ClrType.Name}' — it is tenant-owned but no tenant context is resolved (no ambient scope and no tenant_id claim)."
                    );

                continue;
            }

            if (stampedTenant is not null && currentTenant is not null && stampedTenant != currentTenant)
            {
                throw new CrossTenantWriteException(
                    $"Cannot write '{entry.Metadata.ClrType.Name}' owned by '{stampedTenant}' under tenant context '{currentTenant}'."
                );
            }

            if (stampedTenant is not null && currentTenant is null)
            {
                throw new CrossTenantWriteException(
                    $"Cannot write tenant-owned '{entry.Metadata.ClrType.Name}' (owner '{stampedTenant}') from a host-level context with no tenant."
                );
            }
        }
    }
}
