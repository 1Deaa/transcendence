using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Domain.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HrmSystem.Infrastructure.Persistence.Interceptors;

/*
    //?     Stamps audit fields on every save — the reference architecture declared the audit
    //?     columns (CreatedAt/By, LastModifiedAt/By) but left the Interceptors folder empty;
    //?     this fills that gap so the fields are populated automatically and consistently.
    //
    //?      - Added    → UpdateCreation(utcNow, currentUser)
    //?      - Modified → UpdateModification(utcNow, currentUser)
    //?      - Deleted + ISoftDeletable → converted to Modified + MarkAsDeleted() + modification stamp
    //?        (defense in depth: ABaseRepository already soft-deletes, but a raw Remove() on the
    //?         DbContext would otherwise hard-delete a soft-deletable row).
    //
    //!     CreatedBy/LastModifiedBy are null for unauthenticated flows (seeding, background jobs)
    //!     — the columns are nullable by design; the domain UserId string is used when present.
*/
public sealed class AuditableEntityInterceptor(
    ICurrentUserContext currentUserContext,
    IDateTimeProvider dateTimeProvider
) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        ApplyAuditStamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        ApplyAuditStamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditStamps(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTime utcNow = dateTimeProvider.UtcNow;
        string? currentUser =
            currentUserContext.DomainUserId?.Value ?? currentUserContext.IdentityId;

        foreach (EntityEntry<IAuditable> entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.UpdateCreation(utcNow, currentUser);
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdateModification(utcNow, currentUser);
                    break;

                case EntityState.Deleted when entry.Entity is ISoftDeletable softDeletable:
                    entry.State = EntityState.Modified;
                    softDeletable.MarkAsDeleted();
                    entry.Entity.UpdateModification(utcNow, currentUser);
                    break;

                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                default:
                    break;
            }
        }
    }
}
