using BuildingBlocks.Domain.Aggregates;
using BuildingBlocks.Domain.Entities;

namespace BuildingBlocks.Application.Persistence;

public interface IRepository<TAggregate, in TKey>
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken);

    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken);
}
