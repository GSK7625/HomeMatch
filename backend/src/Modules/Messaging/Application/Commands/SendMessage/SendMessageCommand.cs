using HomeMatch.Modules.Messaging.Domain.Enums;

namespace HomeMatch.Modules.Messaging.Application.Commands.SendMessage;

public record SendMessageCommand(
    long ConversationId,
    long SenderUserId,
    string Content,
    string MessageType = MessageType.Text);
