using HrmSystem.Application.Features.Chats.Shared;
using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.RealTime;

/*
    //?     Application port for real-time chat delivery (NotificationsHub in Web).
    //?     Both participants are notified: the recipient sees the incoming message, the
    //?     sender's OTHER open tabs/devices stay in sync with what they just sent.
*/
public interface IChatNotifier
{
    Task MessageSentAsync(
        UserId senderUserId,
        UserId recipientUserId,
        ChatMessageResponse message,
        CancellationToken ct
    );
}
