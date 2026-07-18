using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Application.Features.Chats.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Chats;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Chats.GetConversations;

internal sealed class GetConversationsQueryHandler(
    IChatQueries chatQueries,
    ICurrentUserContext currentUserContext,
    ITenantContext tenantContext
) : IQueryHandler<GetConversationsQuery, IReadOnlyList<ConversationSummary>>
{
    public async Task<Result<IReadOnlyList<ConversationSummary>>> Handle(
        GetConversationsQuery query,
        CancellationToken cancellationToken
    )
    {
        UserId? currentUserId = currentUserContext.DomainUserId;
        TenantId? tenantId = tenantContext.Current;

        if (currentUserId is null || tenantId is null)
        {
            return ChatMessageErrors.Sender.Unresolved;
        }

        IReadOnlyList<ConversationSummary> conversations =
            await chatQueries.GetConversationsAsync(tenantId, currentUserId, cancellationToken);

        return Result.Success(conversations);
    }
}
