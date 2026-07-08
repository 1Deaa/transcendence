namespace HrmSystem.Domain.Common.Interfaces;

/*
//? Marks an entity that can be logically deleted without a physical DELETE.
//? EF Core global query filters use IsDeleted to exclude soft-deleted rows from all normal queries.
//?
//> Behaviour lives in ASoftDeletableAuditableEntity<TId> (or ASoftDeletableEntity<TId>
//>  for non-auditable entities) — state is owned there with a private setter.
//
//! Do NOT add DIMs or setters to this interface. A DIM can only access interface-declared
//!  members, which forces the setter to be at least protected — defeating encapsulation.
//!  State + behaviour belong in the abstract base class, not the contract.
*/
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    void MarkAsDeleted();
}
