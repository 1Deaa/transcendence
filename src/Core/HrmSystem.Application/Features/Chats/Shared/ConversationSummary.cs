namespace HrmSystem.Application.Features.Chats.Shared;

/*
    //?     One row of the conversation list (Dapper projection): the other participant,
    //?     the latest message preview, and how many of their messages are still unread.
*/
public sealed record ConversationSummary(
    string OtherUserId,
    string OtherUserName,
    string OtherFirstName,
    string OtherLastName,
    string LastMessage,
    DateTime LastMessageAtUtc,
    int UnreadCount
);
