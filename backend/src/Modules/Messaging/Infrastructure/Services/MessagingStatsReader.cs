using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Shared.Contracts.Messaging;

namespace HomeMatch.Modules.Messaging.Infrastructure.Services;

internal sealed class MessagingStatsReader : IMessagingStatsReader
{
    private readonly IConversationRepository _repository;

    public MessagingStatsReader(IConversationRepository repository)
    {
        _repository = repository;
    }

    public Task<int> GetTotalConversationsCountByUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        return _repository.CountConversationsByUserAsync(userId, cancellationToken);
    }
}
