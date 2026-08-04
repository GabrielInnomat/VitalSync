using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence.EventSourced;

internal sealed record TrackedAggregate(
    IDomainEventOwner Aggregate,
    string AggregateName,
    string AggregateId,
    Func<long> Version);
