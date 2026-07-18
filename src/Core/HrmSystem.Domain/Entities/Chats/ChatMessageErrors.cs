using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Chats;

/*
    //?     Error catalog for the ChatMessage aggregate — nested classes per value/field
    //?     (same convention as AnnouncementErrors / EmployeeErrors).
*/
public static class ChatMessageErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "ChatMessage.NotFound",
        "The chat message was not found."
    );

    public static class Id
    {
        public static readonly Error Invalid = Error.Validation(
            "ChatMessage.Id.Invalid",
            "The chat message identifier is required."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "ChatMessage.Id.InvalidFormat",
            $"The chat message identifier must start with the '{ValueObjects.ChatMessageId.Prefix}' prefix."
        );
    }

    public static class Content
    {
        public const int MaxLength = 2000;

        public static readonly Error Required = Error.Validation(
            "ChatMessage.Content.Required",
            "The message text is required."
        );

        public static readonly Error TooLong = Error.Validation(
            "ChatMessage.Content.TooLong",
            $"The message text cannot exceed {MaxLength} characters."
        );
    }

    public static class Recipient
    {
        public static readonly Error Required = Error.Validation(
            "ChatMessage.Recipient.Required",
            "The message recipient is required."
        );

        public static readonly Error NotInWorkspace = Error.NotFound(
            "ChatMessage.Recipient.NotFound",
            "The message recipient was not found in your workspace."
        );

        public static readonly Error SameAsSender = Error.Validation(
            "ChatMessage.Recipient.SameAsSender",
            "A message cannot be sent to yourself."
        );
    }

    public static class Sender
    {
        public static readonly Error Unresolved = Error.Unauthorized(
            "ChatMessage.Sender.Unresolved",
            "The sending user could not be resolved from the current session."
        );
    }
}
