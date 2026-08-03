using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed record TrackedAggregate(
    IDomainEventOwner Aggregate,
    string AggregateName,
    string AggregateId,
    Func<long> Version);
