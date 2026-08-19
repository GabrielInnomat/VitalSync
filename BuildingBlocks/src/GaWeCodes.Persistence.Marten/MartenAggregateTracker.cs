using GaWeCodes.Core.Persistence;
using GaWeCodes.Domain.Events;

namespace GaWeCodes.Persistence.Marten;

internal sealed class MartenAggregateTracker : AggregateTracker<TrackedAggregate>
{
    public void Track(IDomainEventOwner aggregate, string aggregateName, string aggregateId, Func<long> version)
    {
        ArgumentNullException.ThrowIfNull(version);

        Add(new TrackedAggregate(aggregate, aggregateName, aggregateId, version));
    }
}
