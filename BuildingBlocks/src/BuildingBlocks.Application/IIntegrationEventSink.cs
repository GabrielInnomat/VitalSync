namespace BuildingBlocks.Application;

public interface IIntegrationEventSink
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
