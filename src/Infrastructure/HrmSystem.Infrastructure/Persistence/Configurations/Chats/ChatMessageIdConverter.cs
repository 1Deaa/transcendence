using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Chats.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Chats;

internal sealed class ChatMessageIdConverter : ValueConverter<ChatMessageId, string>
{
    public ChatMessageIdConverter()
        : base(chatMessageId => chatMessageId.Value, rawStr => ConvertToChatMessageId(rawStr)) { }

    private static ChatMessageId ConvertToChatMessageId(string rawStr)
    {
        Result<ChatMessageId> result = ChatMessageId.From(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted ChatMessageId: '{rawStr}'."
                    + $" (Error: {result.FirstError.Description})"
            );
        }

        return result.Value;
    }
}
