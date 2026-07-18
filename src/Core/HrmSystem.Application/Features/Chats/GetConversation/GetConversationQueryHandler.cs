using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Chats.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Chats;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Chats.GetConversation;

internal sealed class GetConversationQueryHandler(
    IChatMessageRepository chatMessageRepository,
    ICurrentUserContext currentUserContext
) : IQueryHandler<GetConversationQuery, PaginationResult<ChatMessageResponse>>
{
    public async Task<Result<PaginationResult<ChatMessageResponse>>> Handle(
        GetConversationQuery query,
        CancellationToken cancellationToken
    )
    {
        UserId? currentUserId = currentUserContext.DomainUserId;
        if (currentUserId is null)
        {
            return ChatMessageErrors.Sender.Unresolved;
        }

        Result<UserId> otherIdResult = UserId.From(query.OtherUserId);
        if (otherIdResult.IsFailure)
        {
            return otherIdResult.Errors;
        }

        (IReadOnlyList<ChatMessage> items, int totalCount) =
            await chatMessageRepository.GetConversationAsync(
                currentUserId,
                otherIdResult.Value,
                query.Page,
                query.PageSize,
                cancellationToken
            );

        return PaginationResult<ChatMessageResponse>.Create(
            items.Select(ChatMessageResponse.FromMessage).ToList(),
            query.Page,
            query.PageSize,
            totalCount
        );
    }
}
