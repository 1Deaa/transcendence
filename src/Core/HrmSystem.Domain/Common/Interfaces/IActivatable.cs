namespace HrmSystem.Domain.Common.Interfaces;

/*
//? Marks an entity whose operational state can be toggled between active and inactive
//?  without removing it from the database.
//?
//? Common use-cases:
//?  - A User account suspended by an admin — deactivated, not deleted.
//?  - A subscription plan toggled off-sale without removing its purchase history.
//?  - A feature-flag entity switched on/off at runtime.
//?
//> Unlike ISoftDeletable, an inactive entity is NOT excluded from queries by default —
//>  it is simply blocked from active business flows (login, ordering, etc.).
//>
//> Behaviour lives in AActivatableAuditableEntity<TId> (or AActivatableEntity<TId>
//>  for non-auditable entities) — state is owned there with a private setter.
//
//! Do NOT add DIMs or setters to this interface — same reason as ISoftDeletable.
//!  State + behaviour belong in the abstract base class, not the contract.
*/
public interface IActivatable
{
    bool IsActive { get; }
    void Activate();
    void Deactivate();
}
