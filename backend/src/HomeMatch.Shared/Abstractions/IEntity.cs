namespace HomeMatch.Shared.Abstractions;

public interface IEntity<TId>
{
    TId Id { get; }
}
