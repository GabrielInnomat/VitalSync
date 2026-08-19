using GaWeCodes.Application.DomainEvents;
using GaWeCodes.Application.IntegrationEvents;
using GaWeCodes.Core.Messaging.DomainEvents;
using GaWeCodes.Core.Messaging.IntegrationEvents;
using GaWeCodes.Domain.Events;
using GaWeCodes.Domain.Naming;
using GaWeCodes.Wolverine.DependencyInjection.Wiring;
using GaWeCodes.Wolverine.Messaging.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Tracking;
using BuildingBlockDefaults = GaWeCodes.Wolverine.DependencyInjection.Wiring.WolverineOptionsExtensions;

namespace GaWeCodes.Tests;

public sealed class IntegrationEventSinkDeliveryTests
{
    private static readonly TimeSpan TrackingTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task DeliveredEnvelope_PublishesIntegrationEventWithOriginCorrelation()
    {
        using var host = await StartHostAsync();
        var recorder = host.Services.GetRequiredService<SinkProbeRecorder>();

        var session = await host.TrackActivity()
            .Timeout(TrackingTimeout)
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
            .Timeout(TrackingTimeout)
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(WrapProbeEvent("crash"));

        var tripped = await Task.WhenAny(
                crashSwitch.Tripped,
                Task.Delay(TrackingTimeout, TestContext.Current.CancellationToken))
            .ConfigureAwait(true) == crashSwitch.Tripped;

        Assert.True(
            tripped,
            "The crashing mapper never ran, so the envelope was not handled at all and the assertion below would hold for the wrong reason.");

        crashSwitch.Enabled = false;

        await host.TrackActivity()
            .Timeout(TrackingTimeout)
            .WaitForMessageToBeReceivedAt<SinkProbeIntegrationEvent>(host)
            .PublishMessageAndWaitAsync(WrapProbeEvent("sentinel"));

        var delivered = recorder.Received
            .Select(envelope => Assert.IsType<SinkProbeIntegrationEvent>(envelope.Message).Name)
            .ToArray();

        Assert.Equal(["sentinel"], delivered);

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
                    ServiceDescriptor.Singleton<IIntegrationEventSinkFactory>(
                        new IntegrationEventSinkFactory(TestMessaging.ContextName)));
                services.AddSingleton<IIntegrationEventMapper<SinkProbeDomainEvent>, SinkProbeMapper>();
                services.AddSingleton<IIntegrationEventMapper<SinkProbeDomainEvent>, SinkProbeCrashingMapper>();
                services.AddSingleton<SinkProbeRecorder>();
                services.AddSingleton<SinkProbeCrashSwitch>();
            })
            .UseWolverine(options =>
            {
                options.Durability.Mode = DurabilityMode.Solo;

                options.ApplicationAssembly = typeof(DomainEventEnvelopeHandler).Assembly;

                options.ApplyBuildingBlocksDomainEventRouting();

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
    private readonly TaskCompletionSource _tripped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _enabled;

    public bool Enabled
    {
        get => Volatile.Read(ref _enabled) != 0;
        set => Volatile.Write(ref _enabled, value ? 1 : 0);
    }

    public Task Tripped => _tripped.Task;

    public void Trip() => _tripped.TrySetResult();
}

public sealed class SinkProbeMapper : IIntegrationEventMapper<SinkProbeDomainEvent>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(SinkProbeDomainEvent domainEvent, DomainEventMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);

        return [new SinkProbeIntegrationEvent(domainEvent.Name, metadata.EventId, metadata.OccurredAt)];
    }
}

public sealed class SinkProbeCrashingMapper(SinkProbeCrashSwitch crashSwitch) : IIntegrationEventMapper<SinkProbeDomainEvent>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(SinkProbeDomainEvent domainEvent, DomainEventMetadata metadata)
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

