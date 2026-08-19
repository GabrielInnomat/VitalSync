using GaWeCodes.Domain.Naming;

namespace GaWeCodes.Application.DomainEvents;

public sealed record DomainEventMetadata(
    Guid EventId,
    string AggregateName,
    string AggregateId,
    long Version,
    DateTimeOffset OccurredAt);
