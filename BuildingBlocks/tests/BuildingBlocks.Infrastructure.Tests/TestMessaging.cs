namespace BuildingBlocks.Infrastructure.Tests;

internal static class TestMessaging
{
    public const string ExchangeName = "test-platform.integration-events";

    public const string ContextName = "probe";

    public const string UpstreamContextName = "upstream";

    public static string UniqueQueueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
}
