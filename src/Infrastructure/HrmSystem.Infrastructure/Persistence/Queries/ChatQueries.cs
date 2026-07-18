using System.Data;
using Dapper;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Features.Chats.Shared;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Infrastructure.Persistence.Queries;

/*
    //*     Handles ONLY chat sidebar reads using Dapper — aggregate SQL over the pair table.
    //!     ABSOLUTE RULE: every statement filters WHERE TenantId = @TenantId — Dapper
    //!     bypasses the EF query filters, so this clause IS the tenant isolation.
    //!     Soft delete respected the same way: AND IsDeleted = 0 on every tenant table.
*/
public class ChatQueries(ISqlConnectionFactory connectionFactory) : IChatQueries
{
    public async Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        TenantId tenantId,
        UserId userId,
        CancellationToken ct
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        /*
            //?     [Conversation] normalizes both directions of a pair onto OtherUserId;
            //?     [Latest] finds the newest exchange per partner; OUTER APPLY grabs that
            //?     message's text; the LEFT JOIN counts incoming rows without a read receipt.
        */
        const string sql = """
            WITH Conversation AS (
                SELECT
                    CASE WHEN m.SenderUserId = @UserId THEN m.RecipientUserId ELSE m.SenderUserId END AS OtherUserId,
                    m.Content,
                    m.SentAtUtc,
                    m.SenderUserId,
                    m.ReadAtUtc
                FROM [HrmSystem].[ChatMessages] m
                WHERE m.TenantId = @TenantId AND m.IsDeleted = 0
                  AND (m.SenderUserId = @UserId OR m.RecipientUserId = @UserId)
            ),
            Latest AS (
                SELECT OtherUserId, MAX(SentAtUtc) AS LastMessageAtUtc
                FROM Conversation
                GROUP BY OtherUserId
            )
            SELECT
                l.OtherUserId,
                u.UserName        AS OtherUserName,
                u.FirstName       AS OtherFirstName,
                u.LastName        AS OtherLastName,
                lastMsg.Content   AS LastMessage,
                l.LastMessageAtUtc,
                ISNULL(unread.UnreadCount, 0) AS UnreadCount
            FROM Latest l
            INNER JOIN [HrmSystem].[Users] u ON u.Id = l.OtherUserId AND u.IsDeleted = 0
            OUTER APPLY (
                SELECT TOP 1 c.Content
                FROM Conversation c
                WHERE c.OtherUserId = l.OtherUserId
                ORDER BY c.SentAtUtc DESC
            ) lastMsg
            LEFT JOIN (
                SELECT c.OtherUserId, COUNT(*) AS UnreadCount
                FROM Conversation c
                WHERE c.SenderUserId = c.OtherUserId AND c.ReadAtUtc IS NULL
                GROUP BY c.OtherUserId
            ) unread ON unread.OtherUserId = l.OtherUserId
            ORDER BY l.LastMessageAtUtc DESC
            """;

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new { TenantId = tenantId.Value, UserId = userId.Value },
            cancellationToken: ct
        );

        IEnumerable<ConversationSummary> rows =
            await connection.QueryAsync<ConversationSummary>(command);

        return rows.ToList();
    }

    public async Task<IReadOnlyList<ChatUserSummary>> GetChatUsersAsync(
        TenantId tenantId,
        UserId currentUserId,
        CancellationToken ct
    )
    {
        using IDbConnection connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT u.Id AS UserId,
                   u.UserName,
                   u.FirstName,
                   u.LastName
            FROM [HrmSystem].[Users] u
            WHERE u.TenantId = @TenantId AND u.IsDeleted = 0 AND u.IsActive = 1
              AND u.Id <> @CurrentUserId
            ORDER BY u.FirstName, u.LastName
            """;

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new { TenantId = tenantId.Value, CurrentUserId = currentUserId.Value },
            cancellationToken: ct
        );

        IEnumerable<ChatUserSummary> rows = await connection.QueryAsync<ChatUserSummary>(command);

        return rows.ToList();
    }
}
