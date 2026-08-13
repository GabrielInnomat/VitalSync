namespace BuildingBlocks.Infrastructure.Messaging.Transport;

public interface IMessagingTransportAdapter
{
    string Description { get; }

    string ContextName { get; }

    void Register(MessagingTransportRegistrationContext context);
}
