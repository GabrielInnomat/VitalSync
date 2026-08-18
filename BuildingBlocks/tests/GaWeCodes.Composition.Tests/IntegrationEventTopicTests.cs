using GaWeCodes.Application.IntegrationEvents;
using GaWeCodes.Messaging.IntegrationEvents;

namespace GaWeCodes.Tests;

public class TopicResolverTests
{
    [Fact]
    public void For_AnEventDeclaringItsTopic_ReturnsTheDeclaredTopic()
    {
        Assert.Equal("probe.topic-declared", TopicResolver.For(typeof(TopicDeclaredIntegrationEvent)));
    }

    [Fact]
    public void For_AnEventWithoutTheAttribute_ThrowsNamingTheEventAndTheFix()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => TopicResolver.For(typeof(TopicMissingIntegrationEvent)));

        Assert.Contains(nameof(TopicMissingIntegrationEvent), exception.Message, StringComparison.Ordinal);
        Assert.Contains("IntegrationEventTopic", exception.Message, StringComparison.Ordinal);
    }

    [IntegrationEventTopic("probe.topic-declared")]
    private sealed record TopicDeclaredIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;

    private sealed record TopicMissingIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;
}
