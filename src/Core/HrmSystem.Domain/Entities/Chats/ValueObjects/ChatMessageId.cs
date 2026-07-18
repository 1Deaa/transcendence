using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Chats.ValueObjects;

/*
    //?     Strongly-typed, lexicographically-sortable identity of a ChatMessage.
    //?     Format: "chat-{UUIDv7}" — New() always valid; From() validates untrusted input.
    //!     The constructor is [private] — use New() or From() to get an instance.
*/
public sealed record ChatMessageId
{
    #region Constants

    public const string Prefix = "chat-";

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private ChatMessageId(string value)
    {
        Value = value;
    }

    #endregion

    #region Factories

    public static ChatMessageId New() =>
        new(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    public static Result<ChatMessageId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ChatMessageErrors.Id.Invalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return ChatMessageErrors.Id.InvalidFormat;
        }

        return new ChatMessageId(value);
    }

    #endregion

    #region Overrides

    public override string ToString() => Value;

    #endregion
}
