namespace HomeMatch.Shared.Abstractions;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
