using HrmSystem.Domain.Entities.Users.RefreshTokens;
using HrmSystem.Domain.Entities.Users.RefreshTokens.ValueObjects;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface IRefreshTokenRepository : IBaseRepository<RefreshToken, RefreshTokenId>
{
    /*
        //?     Finds an active, non-deleted refresh token by its opaque secret value.
        //?     Used by the token-refresh endpoint to validate the incoming bearer credential.
        //
        //!     Returns [null] for tokens that are soft-deleted or filtered by the global query filter.
        //!     The caller must check [token.IsValid] to distinguish expired from revoked.
        //!     Never return a token directly to the client without checking [IsValid] first.
    */
    Task<RefreshToken?> FindByTokenValueAsync(string tokenValue, CancellationToken ct);

    /*
        //?     Revokes every active (non-deleted, non-revoked) refresh token for the given user.
        //?     Call this on:
        //>       - Logout all devices
        //>       - Password change
        //>       - Security alert (suspected compromise)
        //
        //!     Calls [Revoke()] on each token through the domain method so [ModifiedAt] is
        //!     updated by the audit interceptor — full audit trail per token, not a bulk UPDATE.
        //!     The caller is responsible for [IUnitOfWork.SaveChangesAsync].
    */
    Task RevokeAllForUserAsync(UserId userId, CancellationToken ct);
}
