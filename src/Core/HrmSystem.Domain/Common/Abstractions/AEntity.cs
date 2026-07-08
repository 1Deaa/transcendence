using HrmSystem.Domain.Common.Interfaces;

namespace HrmSystem.Domain.Common.Abstractions;

// In DDD; (Entity) is a Concept that is identified by its Id (Identity)
//          While a (Value Object) is identified by its properties (Attributes) and has no Id (Identity); which means it has (Structural Equality) (Which is a feature that [[records]] supports);; Another Quality needed in (Value Objects) are Immutability which is satisfied by (records) which is making all properties [get]-only and only allowing values to be set through the constructor (or factory methods) without any setters or [init]s, so once a Value Object is created, it cannot be changed; if you want a different value, you create a new instance with the new values. In contrast, (Entities) typically have mutable state and are compared based on their identity rather than their attributes. In DDD, (Entities) are often used to represent concepts that have a distinct identity and lifecycle, such as a User, Order, or Booking, while (Value Objects) are used to represent concepts that are defined solely by their attributes and do not have a distinct identity, such as an Address, Money, or DateRange.
public abstract class AEntity<TIdType> : IEquatable<AEntity<TIdType>>, IEntity
{
    public TIdType? Id { get; init; }

    protected AEntity(TIdType id)
    {
        Id = id;
    }

    //! For EF (Entities will inherit from this abstract class) => then we need to make protected for inheritance
    protected AEntity() { }

    /// <summary>
    /// ❗❗ - It is a private readonly field So it will be hidden Inside of the [[Entity]] class; And it will be
    /// containing the domain events that we have raised On this [[Entity]] instance. ❗❗ - We will publish these events
    /// after (RaiseDomainEvent(...)) is performed on the entity And the publishing of These [[Events]] Will be using
    /// ((Entity Framework Core) && (MediatR)). ❗❗ - When we persist the data in the database (Habit, User, etc.); We
    /// are going to publish the events for example (UserCreatedDomainEvent) that we have raised on the entity (User)
    /// after we save the changes in the database; So we will be using (EF Core Interceptors) to intercept the saving of
    /// the changes in the database and then we will be using (MediatR) to publish the events that we have raised on the
    /// entity (User). And someone can subscribe to this (UserCreatedDomainEvent) event And execute some behavior
    /// asynchronously! For example: Sending a Welcome Email or SMS to the user who registered; Or sending a
    /// notification to the User after an attemping to login to his account from a new device; Or logging the event for
    /// auditing purposes; Or any other behavior that we want to execute asynchronously after the user is created. - The
    /// reference of [_domainEvents] cannot be changed after initialization. - The list is mutable. You can add and
    /// remove items from it. - 🚨 [_domainEvents.Add(domainEvent)], [_domainEvents.Remove(domainEvent)],
    /// [_domainEvents.Clear()] are allowed. - 🚨🤺⚔️ [_domainEvents = new List<IDomainEvent>()] is not allowed. -
    /// 🚨🤺⚔️ `_domainEvents = [];` 👉👉 This is a collection expression (C# 12+) and it is equivalent to: ⏬👇👇👇 -
    /// 🟰🟰🟰🟰 `_domainEvents = new List<IDomainEvent>()`
    /// </summary>
    private readonly List<IDomainEvent> _domainEvents = [];

    /*
     * ## Implemented By Interface (Interfaces Forces Public; to use it in EF Core:
     *    - EF Core interceptors and MediatR dispatchers need to:
     *      - Read events: GetDomainEvents() (public -- infrastructure needs this)
     *      - Clear events after dispatch: ClearDomainEvents() (public -- infrastructure needs this)
     *      - They never need to write / raise events -- that is the entity's job
    */
    public IReadOnlyList<IDomainEvent> GetDomainEvents() => [.. _domainEvents];

    /*
     * ## Implemented By Interface (Interfaces Forces Public; to use it in EF Core:
     *    - EF Core interceptors and MediatR dispatchers need to:
     *      - Read events: GetDomainEvents() (public -- infrastructure needs this)
     *      - Clear events after dispatch: ClearDomainEvents() (public -- infrastructure needs this)
     *      - They never need to write / raise events -- that is the entity's job
    */
    public void ClearDomainEvents() => _domainEvents.Clear();

    /*
     * [RaiseDomainEvent] is [protected] on purpose -- only the entity itself (from inside its own methods) should decide when a domain event occurs.
     * For example, [Booking.Confirm()] internally calls [RaiseDomainEvent(new BookingConfirmedDomainEvent(Id))] which is a domain event.
     * No external code should ever be able to call [booking.RaiseDomainEvent(...)] directly.
     * 🚨 This is a way to encapsulate the domain events and only allow the entity itself to raise domain events.
     */
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    #region Equality

    public override bool Equals(object? obj)
    {
        /*
         - This is c# syntax for type pattern matching
         - It is a way to check if the [object obj] is of type [Entity<TIdType>] & if it is, it assigns it to the [other] variable which is in our case called [other] and we can use it in the rest of the method
        */
        if (obj is not AEntity<TIdType> other)
        {
            return false;
        }

        return Equals(other);
    }

    public bool Equals(AEntity<TIdType>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        /*
         If either entity is transient (i.e., has not been assigned a valid Id), they are not considered equal.
         This is standard in DDD entity equality comparisons.
         Example:
           if (EqualityComparer<TIdType>.Default.Equals(Id, default)) return false;
           if (EqualityComparer<TIdType>.Default.Equals(other.Id, default)) return false;
        */
        if (EqualityComparer<TIdType>.Default.Equals(Id, default))
        {
            return false;
        }

        /*
         If either entity is transient (i.e., has not been assigned a valid Id), they are not considered equal.
         This is standard in DDD entity equality comparisons.
         Example:
           if (EqualityComparer<TIdType>.Default.Equals(Id, default)) return false;
           if (EqualityComparer<TIdType>.Default.Equals(other.Id, default)) return false;
        */
        if (EqualityComparer<TIdType>.Default.Equals(other.Id, default))
        {
            return false;
        }

        return EqualityComparer<TIdType>.Default.Equals(Id, other.Id);
    }

    public static bool operator ==(AEntity<TIdType>? left, AEntity<TIdType>? right)
    {
        if (left is null && right is null)
        {
            return true;
        }
        if (left is null || right is null)
        {
            return false;
        }
        return left.Equals(right);
    }

    public static bool operator !=(AEntity<TIdType>? left, AEntity<TIdType>? right) =>
        !(left == right);

    public override int GetHashCode() => Id is not null ? Id.GetHashCode() * 41 : 0;

    #endregion
}
