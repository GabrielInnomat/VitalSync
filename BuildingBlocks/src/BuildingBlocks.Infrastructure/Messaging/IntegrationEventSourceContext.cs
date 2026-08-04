namespace BuildingBlocks.Infrastructure.Messaging;

public sealed class IntegrationEventSourceContext(string name)
{
    public const string HeaderName = "buildingblocks.source-context";

    public string Name { get; } = name;
}
