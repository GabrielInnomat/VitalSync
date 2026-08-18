namespace GaWeCodes.Messaging.IntegrationEvents;

public sealed class IntegrationEventSourceContext(string name)
{
    public const string HeaderName = "gawecodes.source-context";

    public string Name { get; } = name;
}
