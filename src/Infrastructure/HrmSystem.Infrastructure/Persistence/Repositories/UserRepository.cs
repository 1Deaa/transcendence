using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Entities.Users;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : ABaseRepository<User, UserId>, IUserRepository
{
    public UserRepository(ApplicationDbContext applicationDbContext)
        : base(applicationDbContext) { }

    public async Task<User?> FindByAnyIdentifierAsync(string identifier, CancellationToken ct)
    {
        string normalizedIdentifier = identifier.Trim();

        /*
            //?     Single query — EF Core translates the three OR branches into one SQL WHERE clause.
            //?     All three columns (Email, Name, PhoneNumber) are indexed with unique constraints,
            //?     so SQL Server can use an index seek on whichever branch matches.
            //
            //!     [IgnoreQueryFilters] — bypasses soft-delete and IsActive filters so the caller
            //!     can distinguish "account deleted" from "wrong credentials" and return the right response.
            //!     Never expose this method in application code outside the authentication flow.
        */
        return await DbContext
            .Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u =>
                    u.Email.Value == normalizedIdentifier
                    || u.UserName.Value == normalizedIdentifier
                    || u.PhoneNumber != null && u.PhoneNumber.Value == normalizedIdentifier,
                ct
            );
    }
    public async Task DeletePermanentlyByIdAsync(UserId id, CancellationToken ct)
    {
        User? user = await DbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is not null)
        {
            DbContext.Users.Remove(user);
        }
    }
}
