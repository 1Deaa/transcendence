namespace HrmSystem.Application.Features.Chats.Shared;

//? Directory entry for starting a new conversation — every user of the caller's workspace.
public sealed record ChatUserSummary(
    string UserId,
    string UserName,
    string FirstName,
    string LastName
);
