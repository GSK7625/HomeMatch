namespace HomeMatch.Shared.Contracts.Messaging;

public interface IMessagingStatsReader
{
    Task<int> GetTotalConversationsCountByUserAsync(long userId, CancellationToken cancellationToken = default);
}
