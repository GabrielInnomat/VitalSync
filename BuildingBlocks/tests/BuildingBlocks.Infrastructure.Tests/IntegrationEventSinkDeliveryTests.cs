using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Tracking;

namespace BuildingBlocks.Infrastructure.Tests;

// Regression tests for IMP-04: the integration-event sink must be bound to the context of the
// DomainEventEnvelope being handled. Runs a real in-memory Wolverine host under the DEFAULT
// ServiceLocationPolicy, so these tests also pin that the first delivered envelope does not fail
// with an InvalidServiceLocationException (the internal Publisher forces service location).
public sealed class IntegrationEventSinkDeliveryTests
{
    [Fact]
    public async Task DeliveredEnvelope_PublishesIntegrationEventWithOriginCorrelation()
    {
        using var host = await StartHostAsync();
        var recorder = host.Services.GetRequiredService<SinkProbeRecorder>();

        var session = await host.TrackActivity()
            .WaitForMessageToBeReceivedAt<SinkProbeIntegrationEvent>(host)
            .PublishMessageAndWaitAsync(WrapProbeEvent("happy"));

        var origin = session.Sent.SingleEnvelope<DomainEventEnvelope>();
        var received = Assert.Single(recorder.Received);
        Assert.Equal(origin.CorrelationId, received.CorrelationId);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MapperFailingAfterSinkPublish_HoldsTheIntegrationEventBack()
    {
        using var host = await StartHostAsync();
        var recorder = host.Services.GetRequiredService<SinkProbeRecorder>();
        host.Services.GetRequiredService<SinkProbeCrashSwitch>().Enabled = true;

        await host.TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(WrapProbeEvent("crash"));

        // A leaked (context-unbound) publish would deliver despite the handler failure.
        await Task.Delay(250, TestContext.Current.CancellationToken);
        Assert.Empty(recorder.Received);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private static DomainEventEnvelope WrapProbeEvent(string name)
        => DomainEventEnvelopeSerializer.Wrap(
            new SinkProbeDomainEvent(name) { EventId = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow });

    private static async Task<IHost> StartHostAsync()
        => await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBuildingBlocks(_ => { });
                services.Replace(
                    ServiceDescriptor.Singleton<IIntegrationEventSinkFactory, WolverineIntegrationEventSinkFactory>());
                services.AddSingleton<IIntegrationEventMapper, SinkProbeMapper>();
                services.AddSingleton<IIntegrationEventMapper, SinkProbeCrashingMapper>();
                services.AddSingleton<SinkProbeRecorder>();
                services.AddSingleton<SinkProbeCrashSwitch>();
            })
            .UseWolverine(options =>
            {
                options.Durability.Mode = DurabilityMode.Solo;

                // Mirrors ApplyBuildingBlockDomainEventRouting without UseDurableInbox (which needs a
                // message store); explicit type inclusion keeps Wolverine from scanning the test
                // assembly's unrelated *Handler fixtures.
                options.Discovery.DisableConventionalDiscovery();
                options.Discovery.IncludeType<DomainEventEnvelopeHandler>();
                options.Discovery.IncludeType(typeof(SinkProbeIntegrationEventHandler));
                options.CodeGeneration.AlwaysUseServiceLocationFor<IDomainEventPublisher>();
                options.CodeGeneration.AlwaysUseServiceLocationFor<IIntegrationEventSinkFactory>();
                options.PublishMessage<DomainEventEnvelope>()
                    .ToLocalQueue("building-blocks-domain-events")
                    .Sequential();
                options.PublishMessage<SinkProbeIntegrationEvent>().ToLocalQueue("sink-probe-integration");
            })
            .StartAsync();
}

public sealed record SinkProbeDomainEvent(string Name) : DomainEvent;

public sealed record SinkProbeIntegrationEvent(string Name) : IIntegrationEvent;

public sealed class SinkProbeRecorder
{
    private readonly List<Envelope> _received = [];

    public IReadOnlyList<Envelope> Received
    {
        get
        {
            lock (_received)
            {
                return [.. _received];
            }
        }
    }

    public void Record(Envelope envelope)
    {
        lock (_received)
        {
            _received.Add(envelope);
        }
    }
}

public sealed class SinkProbeCrashSwitch
{
    public bool Enabled { get; set; }
}

public sealed class SinkProbeMapper : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent)
        => domainEvent is SinkProbeDomainEvent probe ? [new SinkProbeIntegrationEvent(probe.Name)] : [];
}

// Registered after SinkProbeMapper, so it throws only after the integration event was already
// handed to the sink — the exact failure mode IMP-04 protects against.
public sealed class SinkProbeCrashingMapper(SinkProbeCrashSwitch crashSwitch) : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent)
        => crashSwitch.Enabled
            ? throw new InvalidOperationException("Simulated failure after the sink publish.")
            : [];
}

public static class SinkProbeIntegrationEventHandler
{
    public static void Handle(SinkProbeIntegrationEvent message, Envelope envelope, SinkProbeRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        _ = message;
        recorder.Record(envelope);
    }
}
