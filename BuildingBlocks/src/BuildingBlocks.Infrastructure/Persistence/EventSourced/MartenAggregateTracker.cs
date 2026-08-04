using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Persistence;

namespace BuildingBlocks.Infrastructure.Persistence.EventSourced;

internal sealed class MartenAggregateTracker : AggregateTracker<TrackedAggregate>
{
    public void Track(IDomainEventOwner aggregate, string aggregateName, string aggregateId, Func<long> version)
    {
        ArgumentNullException.ThrowIfNull(version);

        Add(new TrackedAggregate(aggregate, aggregateName, aggregateId, version));
    }
}
