using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Chats;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Features.Chats.MarkConversationRead;

internal sealed class MarkConversationReadCommandHandler(
    IChatMessageRepository chatMessageRepository,
    ICurrentUserContext currentUserContext,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork
) : ICommandHandler<MarkConversationReadCommand>
{
    public async Task<Result> Handle(
        MarkConversationReadCommand command,
        CancellationToken cancellationToken
    )
    {
        UserId? currentUserId = currentUserContext.DomainUserId;
        if (currentUserId is null)
        {
            return ChatMessageErrors.Sender.Unresolved;
        }

        Result<UserId> otherIdResult = UserId.From(command.OtherUserId);
        if (otherIdResult.IsFailure)
        {
            return otherIdResult.Errors.ToList();
        }

        IReadOnlyList<ChatMessage> unread = await chatMessageRepository.GetUnreadFromAsync(
            currentUserId,
            otherIdResult.Value,
            cancellationToken
        );

        if (unread.Count == 0)
        {
            return Result.Success();
        }

        DateTime utcNow = dateTimeProvider.UtcNow;
        foreach (ChatMessage message in unread)
        {
            message.MarkAsRead(utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
