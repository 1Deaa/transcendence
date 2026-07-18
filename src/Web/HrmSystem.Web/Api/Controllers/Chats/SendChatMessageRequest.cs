namespace HrmSystem.Web.Api.Controllers.Chats;

public sealed record SendChatMessageRequest(string RecipientUserId, string Content);
