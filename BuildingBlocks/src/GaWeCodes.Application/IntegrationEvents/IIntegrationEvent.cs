namespace GaWeCodes.Application.IntegrationEvents;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}
