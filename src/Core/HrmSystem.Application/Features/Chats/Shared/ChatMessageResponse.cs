using HrmSystem.Domain.Entities.Chats;

namespace HrmSystem.Application.Features.Chats.Shared;

public sealed record ChatMessageResponse(
    string Id,
    string SenderUserId,
    string RecipientUserId,
    string Content,
    DateTime SentAtUtc,
    DateTime? ReadAtUtc
)
{
    public static ChatMessageResponse FromMessage(ChatMessage message) =>
        new(
            message.Id!.Value,
            message.SenderUserId.Value,
            message.RecipientUserId.Value,
            message.Content,
            message.SentAtUtc,
            message.ReadAtUtc
        );
}
