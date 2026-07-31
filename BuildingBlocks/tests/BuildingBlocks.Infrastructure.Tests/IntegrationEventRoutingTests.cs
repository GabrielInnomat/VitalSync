using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.RabbitMQ;
using Wolverine.Runtime;

using BuildingBlockDefaults = BuildingBlocks.Infrastructure.Messaging.WolverineOptionsExtensions;

namespace BuildingBlocks.Infrastructure.Tests;

// Connecting the RabbitMQ transport routes nothing on its own: without a publishing rule Wolverine finds
// no subscriber for an integration event and PublishAsync drops it without a trace. These tests exercise
// the production wiring itself — AddBuildingBlocks/UseWolverineMessaging, never a hand-rolled copy of the
// rule — so deleting the rule from ApplyBuildingBlockMessagingDefaults fails them.
[Collection(RabbitMqCollection.Name)]
public sealed class IntegrationEventRoutingTests(RabbitMqFixture fixture)
{
    private const string ProbeQueueName = "integration-event-routing-probe";
    private static readonly TimeSpan DeliveryTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task PublishedIntegrationEvent_ReachesQueueBoundToThePlatformExchange()
    {
        Assert.SkipUnless(fixture.Available, fixture.SkipReason);

        using var host = await StartHostAsync();
        var name = Guid.NewGuid().ToString();

        await host.Services.GetRequiredService<IMessageBus>().PublishAsync(new RoutingProbeIntegrationEvent(name));

        var received = await host.Services.GetRequiredService<RoutingProbeSignal>()
            .Received.WaitAsync(DeliveryTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(name, Assert.IsType<RoutingProbeIntegrationEvent>(received.Message).Name);

        // Arrival alone proves nothing: without the routing rule Wolverine's local routing convention
        // hands the event straight to the in-process handler, and the test would pass having never
        // touched RabbitMQ. The destination is what distinguishes the two paths.
        Assert.Equal("rabbitmq", received.Destination?.Scheme);
        Assert.Contains(ProbeQueueName, received.Destination?.ToString(), StringComparison.Ordinal);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    // The rule matches the IIntegrationEvent marker rather than all messages, precisely so the envelope
    // carrying a context's raw domain events can never be routed onto the broker (ADR-0022).
    [Fact]
    public async Task DomainEventEnvelope_IsNotRoutedToThePlatformExchange()
    {
        Assert.SkipUnless(fixture.Available, fixture.SkipReason);

        using var host = await StartHostAsync();
        var runtime = host.Services.GetRequiredService<IWolverineRuntime>();

        var integrationEventRouting = runtime.ExplainRoutingFor(typeof(RoutingProbeIntegrationEvent)).ToText();
        var envelopeRouting = runtime.ExplainRoutingFor(typeof(DomainEventEnvelope)).ToText();

        Assert.Contains(BuildingBlockDefaults.IntegrationEventExchangeName, integrationEventRouting, StringComparison.Ordinal);
        Assert.DoesNotContain(BuildingBlockDefaults.IntegrationEventExchangeName, envelopeRouting, StringComparison.Ordinal);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private async Task<IHost> StartHostAsync()
        => await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBuildingBlocks(options => options.UseWolverineMessaging(fixture.ConnectionUri));
                services.AddSingleton<RoutingProbeSignal>();
            })
            .UseWolverine(options =>
            {
                options.Durability.Mode = DurabilityMode.Solo;

                // Keep Wolverine's conventional discovery away from this test assembly's unrelated
                // *Handler fixtures; only the probe handler is needed here.
                options.Discovery.DisableConventionalDiscovery();
                options.Discovery.IncludeType(typeof(RoutingProbeHandler));

                // The consumer side a subscribing service would own: a queue bound to the platform
                // exchange with a topic pattern. Nothing about it is configured by Building Blocks.
                options.ListenToRabbitQueue(ProbeQueueName)
                    .ConfigureQueue(queue => queue
                        .BindTopic("probe.*")
                        .ToExchange(BuildingBlockDefaults.IntegrationEventExchangeName));
            })
            .StartAsync(TestContext.Current.CancellationToken);
}

[Topic("probe.routing-probe")]
public sealed record RoutingProbeIntegrationEvent(string Name) : IIntegrationEvent;

public sealed class RoutingProbeSignal
{
    private readonly TaskCompletionSource<Envelope> _received =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<Envelope> Received => _received.Task;

    public void MarkReceived(Envelope envelope) => _received.TrySetResult(envelope);
}

public static class RoutingProbeHandler
{
    public static void Handle(RoutingProbeIntegrationEvent message, Envelope envelope, RoutingProbeSignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);
        _ = message;
        signal.MarkReceived(envelope);
    }
}
