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
        var crashSwitch = host.Services.GetRequiredService<SinkProbeCrashSwitch>();
        crashSwitch.Enabled = true;

        await host.TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(WrapProbeEvent("crash"));

        Assert.True(
            crashSwitch.Tripped,
            "The crashing mapper never ran, so the envelope was not handled at all and the assertion below would hold for the wrong reason.");

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

                // Scope conventional discovery to the Building Blocks assembly so this test assembly's
                // unrelated *Handler fixtures stay out. Disabling discovery outright would also void the
                // IncludeAssembly call inside the production routing below, leaving the envelope
                // unhandled — and both assertions vacuously satisfied.
                options.ApplicationAssembly = typeof(DomainEventEnvelopeHandler).Assembly;

                // The production wiring verbatim, never a copy of it — including the codegen opt-ins
                // that the default ServiceLocationPolicy would otherwise reject on the first delivered
                // envelope. Restating them here would make this test pass even after they were deleted.
                options.ApplyBuildingBlockDomainEventRouting();

                // The one concession to running without a database: the production routing marks the
                // local queue as a durable inbox, which requires a message store. Overriding the same
                // endpoint afterwards leaves everything else from the production wiring intact.
                options.LocalQueue(BuildingBlockDefaults.DomainEventLocalQueueName).BufferedInMemory();

                options.Discovery.IncludeType(typeof(SinkProbeIntegrationEventHandler));
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
    private int _tripped;

    public bool Enabled { get; set; }

    // "Nothing was received" is also true when nothing ran at all — a handler that never got generated
    // would satisfy the assertion just as well as one that correctly held the event back. This records
    // that the failure path was actually reached, so the negative assertion cannot pass vacuously.
    public bool Tripped => Volatile.Read(ref _tripped) != 0;

    public void Trip() => Volatile.Write(ref _tripped, 1);
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
