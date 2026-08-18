using GaWeCodes.Application.IntegrationEvents;
using GaWeCodes.Domain.Events;

namespace SchemaGapFixture;

public sealed record UnnamedEvent(string Name) : DomainEvent;

public sealed record UntopicedIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;
