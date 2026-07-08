using System.Linq.Expressions;
using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

/*
    //?  Generic base repository — implements every method from IBaseRepository<TEntity, TId> once.
    //?  Concrete repositories (HabitRepository, UserRepository ...) inherit this and add
    //?  only the methods that are unique to that aggregate.
    //>
    //>  DbContext is protected so concrete repositories can use DbContext.Set<T>()
    //>  with Include(), custom LINQ, or FromSqlRaw for aggregate-specific queries.
    //
    //!  This class never calls SaveChangesAsync — that is the UnitOfWork's responsibility.
    //!  The repository only mutates the EF Core change tracker (Add, Update, Remove).
    //!  The caller (command handler) commits via IUnitOfWork.SaveChangesAsync().
*/
internal abstract class ABaseRepository<TEntity, TId>
    where TEntity : AEntity<TId>
    where TId : class
{
    protected readonly ApplicationDbContext DbContext;

    protected ABaseRepository(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }

    /*
        //? GetByIdAsync — standard lookup with global query filters applied.
        //?  Global filters add WHERE IsDeleted = 0 (and IsActive = 1 for User) automatically.
        //?  Use this in 99% of cases — it is the "normal user sees their active data" path.
    */
    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct) =>
        await DbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, ct);

    /*
        //? GetAllAsync — returns every row visible under the global query filters.
        //?  Soft-deleted entities are automatically excluded.
        //!  Avoid on large tables — prefer a Dapper query with pagination for reads.
    */
    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct) =>
        await DbContext.Set<TEntity>().ToListAsync(ct);

    /*
        //? AddAsync — stages the entity for INSERT in the EF change tracker.
        //?  The actual INSERT is sent to the DB only on IUnitOfWork.SaveChangesAsync().
    */
    public async Task AddAsync(TEntity entity, CancellationToken ct) =>
        await DbContext.Set<TEntity>().AddAsync(entity, ct);

    /*
        //? UpdateAsync — marks the entity as Modified in the EF change tracker.
        //>
        //>  For entities loaded within the SAME DbContext scope (same HTTP request):
        //>   Update() is optional — EF Core detects property changes automatically
        //>   via snapshot-based change tracking.
        //>
        //>  For DETACHED entities (loaded in a different scope or created manually):
        //>   Update() re-attaches them and marks ALL properties as Modified, producing
        //>   a full-row UPDATE on the next SaveChanges.
        //
        //!  Prefer loading the entity first and mutating it through domain methods —
        //!  EF then generates a targeted UPDATE for only the changed columns.
    */
    public Task UpdateAsync(TEntity entity)
    {
        DbContext.Set<TEntity>().Update(entity);
        return Task.CompletedTask;

        /*
            //!   DbContext.Set<T>().Update(entity) — change tracker approach:
            //!     - Stages the change; nothing hits the DB until SaveChangesAsync() is called
            //!     - That's the entire point of the UnitOfWork pattern — all changes in a request commit atomically
            //!     - Works with interceptors and domain event dispatch that inspect the change tracker on SaveChanges
            //!
            //!     ExecuteUpdateAsync() — bypasses the change tracker entirely:
            //!     - Sends UPDATE directly to the DB immediately, no SaveChangesAsync needed (or respected)
            //!     - Domain events on SaveChanges never fire
            //!     - You must explicitly tell it which columns to set via SetProperty(...) — a generic base can't know that
            //
            //?       // ExecuteUpdateAsync requires knowing what to update at call site:
            //?              await DbContext.Set<Habit>()
            //?                  .Where(h => h.Id == id)
            //?                  .ExecuteUpdateAsync(s => s
            //?                      .SetProperty(h => h.Status, newStatus)   // you must name every column
            //?                      .SetProperty(h => h.UpdatedAt, now));     // generic base can't do this
            //*       - A generic UpdateAsync(entity) can't use it — it doesn't know which properties changed.
            //
            //*       - ExecuteUpdateAsync belongs in bulk-operation scenarios: "set IsArchived = true on all habits older than 90 days for a background job." That's a future specific method on a concrete
            //*       - repository or a Dapper query, not the generic base. For the normal command-handler flow (load → mutate via domain method → commit), change tracking is correct.
         */
    }

    /*
        //? DeleteByIdAsync — loads the entity then delegates to DeleteEntityAsync.
        //?  Returns silently if the entity does not exist or is already soft-deleted.
        //
        //!  Uses GetByIdAsync so global filters apply — cannot "delete" an already-deleted row.
        //!  Use FindByIdAsync manually if you need to hard-delete a soft-deleted entity.
    */
    public async Task DeleteByIdAsync(TId id, CancellationToken ct)
    {
        TEntity? entity = await GetByIdAsync(id, ct);

        if (entity is null)
        {
            return;
        }

        await DeleteEntityAsync(entity);
    }

    /*
        //? DeleteEntityAsync — picks the correct delete strategy automatically:
        //>   ISoftDeletable (Habit, EmailTemplate) → calls MarkAsDeleted() on the entity
        //>   Not ISoftDeletable (Tag, HabitTag)    → calls DbContext.Remove() — physical DELETE
        //
        //!  MarkAsDeleted() is a DOMAIN METHOD — it runs through the entity's own logic
        //!  and may raise domain events. Never set IsDeleted = true from outside the entity.
    */
    public Task DeleteEntityAsync(TEntity entity)
    {
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.MarkAsDeleted();
        }
        else
        {
            DbContext.Set<TEntity>().Remove(entity);
        }

        return Task.CompletedTask;
    }

    /*
        //? FindByIdAsync — bypasses ALL global query filters (IgnoreQueryFilters).
        //?  Use this when you explicitly need to reach hidden entities:
        //>   - Admin panel restoring a soft-deleted habit
        //>   - Identity service loading a suspended (IsActive = false) user for login checks
        //>   - Background job processing all entities regardless of state
        //
        //!  Do NOT use this in normal application flows — it returns rows the user
        //!  should not see (soft-deleted entities, deactivated accounts).
    */
    public async Task<TEntity?> FindByIdAsync(TId id, CancellationToken ct) =>
        await DbContext
            .Set<TEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    /*
        //? FindUsingPredicate — custom LINQ filter; global query filters still apply.
        //?  Use for non-PK lookups:
        //>   - Check for a duplicate email before registration
        //>   - Find all active habits with a specific status for a given user
    */
    public async Task<IEnumerable<TEntity>> FindUsingPredicate(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct
    ) => await DbContext.Set<TEntity>().Where(predicate).ToListAsync(ct);
}
