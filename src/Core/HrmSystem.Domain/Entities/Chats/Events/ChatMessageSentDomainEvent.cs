using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Entities.Chats.ValueObjects;

namespace HrmSystem.Domain.Entities.Chats.Events;

//? Raised when a chat message is created — the Application handler fans it out over SignalR.
public sealed record ChatMessageSentDomainEvent(ChatMessageId ChatMessageId) : IDomainEvent;
