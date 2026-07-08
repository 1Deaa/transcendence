namespace HrmSystem.Domain.Common.Interfaces;

/*
     //? Contract used by infrastructure — EF Core interceptors and MediatR dispatchers need to:
     //?  - Read events  : GetDomainEvents()   (public — infrastructure needs this)
     //?  - Clear events : ClearDomainEvents() (public — infrastructure needs this)
     //
     //! RaiseDomainEvent is NOT on IEntity — interfaces force all members to be public.
     //!  RaiseDomainEvent is [protected] on AEntity<T> so only the entity itself decides when
     //!  a domain event occurs. No external code can call booking.RaiseDomainEvent(...) directly.
     //
     //> | Member              | Access              | Where             | Who calls it                    |
     //> | GetDomainEvents()   | public (on IEntity) | Interface         | Infrastructure (interceptor)    |
     //> | ClearDomainEvents() | public (on IEntity) | Interface         | Infrastructure (after dispatch) |
     //> | RaiseDomainEvent()  | protected (AEntity) | Abstract class    | The entity itself               |
 */
public interface IEntity
{
    IReadOnlyList<IDomainEvent> GetDomainEvents();
    void ClearDomainEvents();
}
