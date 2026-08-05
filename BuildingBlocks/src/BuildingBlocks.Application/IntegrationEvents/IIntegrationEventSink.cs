namespace BuildingBlocks.Application.IntegrationEvents;

public interface IIntegrationEventSink
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
