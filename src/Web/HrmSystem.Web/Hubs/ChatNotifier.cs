using HrmSystem.Application.Common.Interfaces.RealTime;
using HrmSystem.Application.Features.Chats.Shared;
using HrmSystem.Domain.Entities.Users.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace HrmSystem.Web.Hubs;

/*
    //?     Web-side implementation of the chat delivery port — SignalR stays in this layer.
    //?     Pushes to BOTH per-user groups: the recipient gets the incoming message; the
    //?     sender's other open tabs/devices see their own message appear in sync.
*/
internal sealed class ChatNotifier(IHubContext<NotificationsHub> hubContext) : IChatNotifier
{
    public async Task MessageSentAsync(
        UserId senderUserId,
        UserId recipientUserId,
        ChatMessageResponse message,
        CancellationToken ct
    )
    {
        await hubContext
            .Clients.Groups(
                NotificationsHub.UserGroupName(recipientUserId.Value),
                NotificationsHub.UserGroupName(senderUserId.Value)
            )
            .SendAsync("ChatMessageReceived", message, ct);
    }
}
