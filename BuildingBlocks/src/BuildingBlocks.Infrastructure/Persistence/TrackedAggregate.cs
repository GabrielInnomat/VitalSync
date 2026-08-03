using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed record TrackedAggregate(
    IDomainEventOwner Aggregate,
    Func<string> StreamKey,
    Func<long> ExpectedVersion);
