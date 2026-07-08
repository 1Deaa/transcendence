using System.Linq.Expressions;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

/*
 //! 1. What does "Repository" actually mean?
//?     - In Domain-Driven Design (DDD) and Clean Architecture, a Repository is an abstraction that makes your database look like an in-memory collection of Domain Entities.
//?     - Its sole purpose (غرضها الوحيد) is to encapsulate the logic required to access data sources, allowing your core Application/Domain logic to remain completely ignorant of whether you are using SQL Server, PostgreSQL, or a simple text file.
//* When using Dapper; EF Core for Commands (Writes/Updates/Deletes) While Dapper for Queries
 */
public interface IBaseRepository<TEntity, TId>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct);
    Task AddAsync(TEntity entity, CancellationToken ct);
    Task UpdateAsync(TEntity entity);
    Task DeleteByIdAsync(TId id, CancellationToken ct); // Soft Delete
    Task DeleteEntityAsync(TEntity entity); // Soft Delete
    Task<TEntity?> FindByIdAsync(TId id, CancellationToken ct);
    Task<IEnumerable<TEntity>> FindUsingPredicate(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct
    );
}
