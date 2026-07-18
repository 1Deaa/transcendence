using HrmSystem.Application.Common.Exceptions;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

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

        //? Pass 1 — aggregate roots: stamp fresh Added entities, reject mismatches.
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

            ThrowOnMismatch(entry.Metadata.ClrType.Name, stampedTenant, currentTenant);
        }

        /*
            //!     Pass 2 — OWNED members (name/email/... value objects mapped as owned types):
            //!     mutating one marks ONLY the owned entry Modified — the aggregate root stays
            //!     Unchanged and pass 1 never sees the write. Without this pass, a handler
            //!     holding a foreign tenant's aggregate could rewrite its owned columns
            //!     unguarded. Each dirty owned entry is walked up its ownership chain to the
            //!     root, whose TenantId is then checked exactly like a direct root write.
            //?     Runs AFTER pass 1 so roots added in this save are already stamped.
        */
        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (!entry.Metadata.IsOwned())
            {
                continue;
            }

            ITenantOwned? root = ResolveOwnedRoot(context, entry);
            if (root is not null)
            {
                ThrowOnMismatch(entry.Metadata.ClrType.Name, root.TenantId, currentTenant);
            }
        }
    }

    private static void ThrowOnMismatch(
        string entityName,
        TenantId? stampedTenant,
        TenantId? currentTenant
    )
    {
        if (stampedTenant is not null && currentTenant is not null && stampedTenant != currentTenant)
        {
            throw new CrossTenantWriteException(
                $"Cannot write '{entityName}' owned by '{stampedTenant}' under tenant context '{currentTenant}'."
            );
        }

        if (stampedTenant is not null && currentTenant is null)
        {
            throw new CrossTenantWriteException(
                $"Cannot write tenant-owned '{entityName}' (owner '{stampedTenant}') from a host-level context with no tenant."
            );
        }
    }

    /*
        //?     Walks an owned entry's ownership chain (owned → ... → aggregate root) and
        //?     returns the root when it is tenant-owned. Owned types rarely expose a
        //?     dependent-to-principal navigation, so the principal is located by matching
        //?     the ownership FK values against tracked principal keys.
    */
    private static ITenantOwned? ResolveOwnedRoot(DbContext context, EntityEntry entry)
    {
        EntityEntry current = entry;

        while (current.Metadata.IsOwned())
        {
            IForeignKey? ownership = current.Metadata.FindOwnership();
            if (ownership is null)
            {
                return null;
            }

            object?[] foreignKeyValues = ownership
                .Properties.Select(p => current.Property(p.Name).CurrentValue)
                .ToArray();

            EntityEntry? principal = context
                .ChangeTracker.Entries()
                .FirstOrDefault(candidate =>
                    ownership.PrincipalEntityType.ClrType.IsInstanceOfType(candidate.Entity)
                    && ownership
                        .PrincipalKey.Properties.Select(p =>
                            candidate.Property(p.Name).CurrentValue
                        )
                        .SequenceEqual(foreignKeyValues)
                );

            if (principal is null)
            {
                return null;
            }

            current = principal;
        }

        return current.Entity as ITenantOwned;
    }
}
