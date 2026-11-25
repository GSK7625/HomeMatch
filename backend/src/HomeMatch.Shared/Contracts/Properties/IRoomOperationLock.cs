namespace HomeMatch.Shared.Contracts.Properties;

public interface IRoomOperationLock
{
    Task<IAsyncDisposable> AcquireAsync(long roomId, CancellationToken cancellationToken = default);
}
