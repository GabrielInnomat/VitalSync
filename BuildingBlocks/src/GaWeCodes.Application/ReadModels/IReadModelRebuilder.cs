using GaWeCodes.Domain.Aggregates;
using GaWeCodes.Domain.Entities;

namespace GaWeCodes.Application.ReadModels;

public interface IReadModelRebuilder<in TAggregate, TKey>
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
{
    Task ClearAsync(CancellationToken cancellationToken);

    Task RebuildAsync(TAggregate aggregate, CancellationToken cancellationToken);
}
