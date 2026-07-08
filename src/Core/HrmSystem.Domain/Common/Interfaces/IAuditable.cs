namespace HrmSystem.Domain.Common.Interfaces;

/*
    //?     Non-generic view over AAuditableEntity<TId> so infrastructure code (the
    //?     AuditableEntityInterceptor) can stamp audit fields without knowing TId.
    //?     ChangeTracker.Entries<IAuditable>() works; Entries<AAuditableEntity<?>>() cannot.
    //
    //!     State lives in AAuditableEntity<TId> with private setters — this contract only
    //!     exposes the two mutation methods the base class already declares publicly.
    //!     Do NOT add setters or DIMs here (same encapsulation rule as ISoftDeletable).
*/
public interface IAuditable
{
    DateTime CreatedAt { get; }
    string? CreatedBy { get; }
    DateTime? LastModifiedAt { get; }
    string? LastModifiedBy { get; }

    void UpdateCreation(DateTime createdAt, string? createdBy);

    void UpdateModification(DateTime lastModifiedAt, string? lastModifiedBy);
}
