namespace HomeMatch.Shared.Abstractions;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

public interface IEntity<TId>
{
    TId Id { get; }
}
