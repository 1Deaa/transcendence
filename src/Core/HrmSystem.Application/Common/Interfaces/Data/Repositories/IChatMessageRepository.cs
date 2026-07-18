using HrmSystem.Domain.Entities.Chats;
using HrmSystem.Domain.Entities.Chats.ValueObjects;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Repositories;

public interface IChatMessageRepository : IBaseRepository<ChatMessage, ChatMessageId>
{
    /*
        //?     One conversation = all messages exchanged between two users, newest first,
        //?     offset-paged. Returned as (Items, TotalCount) so the handler builds the
        //?     PaginationResult. Tenant isolation comes from the EF named query filter.
    */
    Task<(IReadOnlyList<ChatMessage> Items, int TotalCount)> GetConversationAsync(
        UserId userA,
        UserId userB,
        int page,
        int pageSize,
        CancellationToken ct
    );

    /*
        //?     Loads the unread messages the reader received from the other user
        //?     (tracked, so [MarkAsRead] mutations persist on the next SaveChanges).
    */
    Task<IReadOnlyList<ChatMessage>> GetUnreadFromAsync(
        UserId readerUserId,
        UserId otherUserId,
        CancellationToken ct
    );
}
