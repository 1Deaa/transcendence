using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Entities.Users.RefreshTokens;
using HrmSystem.Domain.Entities.Users.RefreshTokens.ValueObjects;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository
    : ABaseRepository<RefreshToken, RefreshTokenId>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext dbContext)
        : base(dbContext) { }

    public async Task<RefreshToken?> FindByTokenValueAsync(string tokenValue, CancellationToken ct)
    {
        /*
            //?     EF Core translates the owned-type predicate [rt.Token.Value == tokenValue]
            //?     into a WHERE clause on the [Token] column — the unique index makes this an
            //?     index seek rather than a full scan.
            //!     Global query filter ([!IsDeleted]) applies — soft-deleted tokens are invisible.
            //!     The caller checks [token.IsValid] for expired / revoked state.
        */
        return await DbContext
            .Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token.Value == tokenValue, ct);
    }

    public async Task RevokeAllForUserAsync(UserId userId, CancellationToken ct)
    {
        /*
            //?     Load only active tokens — no point calling [Revoke()] on already-revoked rows.
            //?     [IgnoreQueryFilters] is NOT used: soft-deleted tokens are already out of play.
            //
            //*     Each token is revoked through the domain method so [ModifiedAt] is updated
            //*     by the EF Core audit interceptor on [SaveChanges] — one audit row per token.
            //!     The caller MUST call [IUnitOfWork.SaveChangesAsync] after this returns.
        */
        List<RefreshToken> activeTokens = await DbContext
            .Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync(ct);

        foreach (RefreshToken token in activeTokens)
        {
            token.Revoke();
        }
    }
}
