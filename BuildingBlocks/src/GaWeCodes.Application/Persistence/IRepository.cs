using GaWeCodes.Domain.Aggregates;
using GaWeCodes.Domain.Entities;

namespace GaWeCodes.Application.Persistence;

public interface IRepository<TAggregate, TKey>
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken);

    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken);
}
