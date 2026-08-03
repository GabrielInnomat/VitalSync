using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

public interface IRepository<TAggregate, in TKey>
    where TAggregate : class, IAggregateRoot<TKey>, IReconstitutable<TAggregate>
    where TKey : struct, IEntityKey
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken);

    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken);
}
