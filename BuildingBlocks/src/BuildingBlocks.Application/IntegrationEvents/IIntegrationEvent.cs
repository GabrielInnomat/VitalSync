namespace BuildingBlocks.Application.IntegrationEvents;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}
