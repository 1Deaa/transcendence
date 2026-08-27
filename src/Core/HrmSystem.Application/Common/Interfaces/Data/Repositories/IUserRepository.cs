using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface IUserRepository : IBaseRepository<User, UserId>
{
    /*
        //?     Finds a user by any of the three login identifiers in a single query.
        //?     Searches Email, UserName, and PhoneNumber simultaneously.
        //
        //*     Uses [IgnoreQueryFilters] so suspended or soft-deleted accounts are still
        //*     found — the caller decides what error to return for each state, rather
        //*     than silently returning [InvalidCredentials] for a deleted account.
        //
        //!     SQL Server's default CI collation makes the comparison case-insensitive
        //!     at the database level — never call .ToLower() in the LINQ predicate, it
        //!     forces a LOWER() function call on every row and prevents index seeks.
    */
    Task<User?> FindByAnyIdentifierAsync(string identifier, CancellationToken ct);

    Task DeletePermanentlyByIdAsync(UserId id, CancellationToken ct);
}
