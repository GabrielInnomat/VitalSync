using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Persistence;

namespace BuildingBlocks.Infrastructure.Persistence.EventSourced;

internal sealed record TrackedAggregate(
    IDomainEventOwner Aggregate,
    string AggregateName,
    string AggregateId,
    Func<long> Version) : ITrackedAggregate
{
    public long CurrentVersion => Version();
}
