using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Tracking;

using BuildingBlockDefaults = BuildingBlocks.Infrastructure.Messaging.WolverineOptionsExtensions;

namespace BuildingBlocks.Infrastructure.Tests;

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
        var crashSwitch = host.Services.GetRequiredService<SinkProbeCrashSwitch>();
        crashSwitch.Enabled = true;

        await host.TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(WrapProbeEvent("crash"));

        Assert.True(
            crashSwitch.Tripped,
            "The crashing mapper never ran, so the envelope was not handled at all and the assertion below would hold for the wrong reason.");

        await Task.Delay(250, TestContext.Current.CancellationToken);
        Assert.Empty(recorder.Received);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private static DomainEventEnvelope WrapProbeEvent(string name)
        => new DomainEventEnvelopeSerializer(new DomainEventTypeRegistry([typeof(SinkProbeDomainEvent).Assembly]))
            .Wrap(new SinkProbeDomainEvent(name), Guid.NewGuid(), "sink-probe", "1", 1, DateTimeOffset.UtcNow);

    private static async Task<IHost> StartHostAsync()
        => await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBuildingBlocks(options => options.AddDomainEventsFrom(typeof(SinkProbeDomainEvent).Assembly));
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

                options.ApplicationAssembly = typeof(DomainEventEnvelopeHandler).Assembly;

                options.ApplyBuildingBlockDomainEventRouting();

                options.LocalQueue(BuildingBlockDefaults.DomainEventLocalQueueName).BufferedInMemory();

                options.Discovery.IncludeType(typeof(SinkProbeIntegrationEventHandler));
                options.PublishMessage<SinkProbeIntegrationEvent>().ToLocalQueue("sink-probe-integration");
            })
            .StartAsync();
}

[EventName("sink-probe-v1")]
public sealed record SinkProbeDomainEvent(string Name) : DomainEvent;

public sealed record SinkProbeIntegrationEvent(string Name, Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;

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
    private int _tripped;

    public bool Enabled { get; set; }

    public bool Tripped => Volatile.Read(ref _tripped) != 0;

    public void Trip() => Volatile.Write(ref _tripped, 1);
}

public sealed class SinkProbeMapper : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent, DomainEventMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return domainEvent is SinkProbeDomainEvent probe
            ? [new SinkProbeIntegrationEvent(probe.Name, metadata.EventId, metadata.OccurredAt)]
            : [];
    }
}

public sealed class SinkProbeCrashingMapper(SinkProbeCrashSwitch crashSwitch) : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent, DomainEventMetadata metadata)
    {
        if (!crashSwitch.Enabled)
        {
            return [];
        }

        crashSwitch.Trip();
        throw new InvalidOperationException("Simulated failure after the sink publish.");
    }
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

