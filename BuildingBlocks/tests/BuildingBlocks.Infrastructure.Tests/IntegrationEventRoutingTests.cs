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

        Assert.Equal("rabbitmq", received.Destination?.Scheme);
        Assert.Contains(ProbeQueueName, received.Destination?.ToString(), StringComparison.Ordinal);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

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

                options.Discovery.DisableConventionalDiscovery();
                options.Discovery.IncludeType(typeof(RoutingProbeHandler));

                options.ListenToRabbitQueue(ProbeQueueName)
                    .ConfigureQueue(queue => queue
                        .BindTopic("probe.*")
                        .ToExchange(BuildingBlockDefaults.IntegrationEventExchangeName));
            })
            .StartAsync(TestContext.Current.CancellationToken);
}

[Topic("probe.routing-probe")]
public sealed record RoutingProbeIntegrationEvent(string Name) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

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
