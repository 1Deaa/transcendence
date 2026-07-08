using HrmSystem.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;

namespace HrmSystem.Infrastructure.Services.Identity;

public sealed class AppUser : IdentityUser
{
    /*
        //?  Navigation back to the domain [User] aggregate.
        //?  Only populated when queried through [ApplicationDbContext] —
        //?  [ApplicationIdentityDbContext] ignores this navigation via [Ignore()].
        //!  Never set this manually — it is managed by EF Core via the 1:1 FK on [User.IdentityId].
    */
    public User? DomainUser { get; set; }
}
