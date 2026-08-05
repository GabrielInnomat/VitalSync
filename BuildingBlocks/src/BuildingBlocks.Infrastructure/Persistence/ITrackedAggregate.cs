using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Infrastructure.Persistence;

internal interface ITrackedAggregate
{
    IDomainEventOwner Aggregate { get; }

    string AggregateName { get; }

    string AggregateId { get; }

    long CurrentVersion { get; }
}
