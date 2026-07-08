using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Authentication;

[Obsolete(
    "No need -- as i implemented: [CurrentUserContext] inside [HrmSystem.Infrastructure.Services.Identity]"
)]
public interface IUserContext
{
    UserId UserId { get; }
    string IdentityId { get; }
}
