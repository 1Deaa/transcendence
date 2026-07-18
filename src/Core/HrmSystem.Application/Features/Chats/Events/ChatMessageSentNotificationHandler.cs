using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.RealTime;
using HrmSystem.Application.Features.Chats.Shared;
using HrmSystem.Domain.Entities.Chats;
using HrmSystem.Domain.Entities.Chats.Events;
using MediatR;

namespace HrmSystem.Application.Features.Chats.Events;

/*
    //?     Delivers a just-sent chat message over SignalR to both participants.
    //?     Runs AFTER SaveChangesAsync commits, so the pushed payload always exists
    //?     in the database — a delivered message can never be lost by a rollback.
*/
internal sealed class ChatMessageSentNotificationHandler(
    IChatMessageRepository chatMessageRepository,
    IChatNotifier chatNotifier
) : INotificationHandler<ChatMessageSentDomainEvent>
{
    public async Task Handle(
        ChatMessageSentDomainEvent notification,
        CancellationToken cancellationToken
    )
    {
        ChatMessage? message = await chatMessageRepository.GetByIdAsync(
            notification.ChatMessageId,
            cancellationToken
        );
        if (message is null)
        {
            return;
        }

        await chatNotifier.MessageSentAsync(
            message.SenderUserId,
            message.RecipientUserId,
            ChatMessageResponse.FromMessage(message),
            cancellationToken
        );
    }
}
