using HrmSystem.Application.Features.Chats.Shared;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Data.Queries;

/*
    //?     Dapper read queries behind the chat sidebar.
    //!     ABSOLUTE RULE (CLAUDE.md): every method takes an explicit TenantId and every SQL
    //!     statement includes WHERE TenantId = @TenantId — Dapper bypasses the EF query
    //!     filters, so this parameter IS the tenant isolation.
*/
public interface IChatQueries
{
    //? Conversation list: latest message + unread count per chat partner, newest first.
    Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        TenantId tenantId,
        UserId userId,
        CancellationToken ct
    );

    //? Workspace user directory (excluding the caller) — the "new message" picker.
    Task<IReadOnlyList<ChatUserSummary>> GetChatUsersAsync(
        TenantId tenantId,
        UserId currentUserId,
        CancellationToken ct
    );
}
