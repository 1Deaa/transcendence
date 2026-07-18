using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Entities.Chats;
using HrmSystem.Domain.Entities.Chats.ValueObjects;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class ChatMessageRepository
    : ABaseRepository<ChatMessage, ChatMessageId>, IChatMessageRepository
{
    public ChatMessageRepository(ApplicationDbContext dbContext)
        : base(dbContext) { }

    public async Task<(IReadOnlyList<ChatMessage> Items, int TotalCount)> GetConversationAsync(
        UserId userA,
        UserId userB,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        //? The named tenant query filter scopes both queries to the caller's workspace.
        IQueryable<ChatMessage> query = DbContext
            .Set<ChatMessage>()
            .AsNoTracking()
            .Where(m =>
                m.SenderUserId == userA && m.RecipientUserId == userB
                || m.SenderUserId == userB && m.RecipientUserId == userA
            )
            .OrderByDescending(m => m.SentAtUtc);

        int totalCount = await query.CountAsync(ct);

        List<ChatMessage> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetUnreadFromAsync(
        UserId readerUserId,
        UserId otherUserId,
        CancellationToken ct
    )
    {
        //! TRACKED on purpose — the handler mutates ReadAtUtc and saves via IUnitOfWork.
        return await DbContext
            .Set<ChatMessage>()
            .Where(m =>
                m.RecipientUserId == readerUserId
                && m.SenderUserId == otherUserId
                && m.ReadAtUtc == null
            )
            .ToListAsync(ct);
    }
}
