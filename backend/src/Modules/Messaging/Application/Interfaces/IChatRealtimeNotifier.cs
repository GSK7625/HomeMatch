using HomeMatch.Modules.Messaging.Application.DTOs;

namespace HomeMatch.Modules.Messaging.Application.Interfaces;

public interface IChatRealtimeNotifier
{
    Task NotifyMessageSentAsync(
        long initiatorId,
        long recipientId,
        MessageDto message,
        CancellationToken cancellationToken = default);

    Task NotifyMessagesReadAsync(
        long conversationId,
        long readerUserId,
        long partnerUserId,
        int markedReadCount,
        CancellationToken cancellationToken = default);
}
