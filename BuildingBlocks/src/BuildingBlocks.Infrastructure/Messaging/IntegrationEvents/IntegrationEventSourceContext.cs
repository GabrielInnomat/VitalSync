namespace BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;

public sealed class IntegrationEventSourceContext(string name)
{
    public const string HeaderName = "buildingblocks.source-context";

    public string Name { get; } = name;
}
