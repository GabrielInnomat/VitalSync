using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Marten;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class MartenEventSourcedRepository<TAggregate, TKey>(IDocumentSession session, MartenAggregateTracker tracker)
    : IRepository<TAggregate, TKey>
    where TAggregate : class, IEventSourcedAggregateRoot<TKey>, IReconstitutable<TAggregate>
    where TKey : struct, IEntityKey
{
    public async Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken)
    {
        var streamKey = EntityKeyFormatter.GetStreamKey(typeof(TAggregate), id);
        var stream = await session.Events.FetchStreamAsync(streamKey, token: cancellationToken).ConfigureAwait(false);

        if (stream is not { Count: > 0 })
        {
            return null;
        }

        var aggregate = TAggregate.CreateEmpty();
        ((IEventSourcedAggregateRoot<TKey>)aggregate).LoadFromHistory(stream.Select(@event => (IDomainEvent)@event.Data));
        Track(aggregate);
        return aggregate;
    }

    public Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        Track(aggregate);
        return Task.CompletedTask;
    }

    private void Track(TAggregate aggregate)
    {
        tracker.Track(
            (IDomainEventOwner)aggregate,
            () => EntityKeyFormatter.GetStreamKey(typeof(TAggregate), aggregate.Id),
            () => ((IEventSourcedAggregateRoot<TKey>)aggregate).Version);
    }
}
