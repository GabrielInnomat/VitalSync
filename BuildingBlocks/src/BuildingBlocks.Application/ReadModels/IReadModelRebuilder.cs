using BuildingBlocks.Domain.Aggregates;
using BuildingBlocks.Domain.Entities;

namespace BuildingBlocks.Application.ReadModels;

public interface IReadModelRebuilder<in TAggregate, TKey>
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
{
    Task ClearAsync(CancellationToken cancellationToken);

    Task RebuildAsync(TAggregate aggregate, CancellationToken cancellationToken);
}
