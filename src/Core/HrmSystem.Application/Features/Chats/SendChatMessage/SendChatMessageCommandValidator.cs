using FluentValidation;
using HrmSystem.Domain.Entities.Chats;

namespace HrmSystem.Application.Features.Chats.SendChatMessage;

internal sealed class SendChatMessageCommandValidator
    : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageCommandValidator()
    {
        RuleFor(c => c.RecipientUserId).NotEmpty();
        RuleFor(c => c.Content).NotEmpty().MaximumLength(ChatMessageErrors.Content.MaxLength);
    }
}
