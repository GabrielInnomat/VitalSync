using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using VitalSync.Sample.EventSourced.Contracts;
using StateStoredContracts = VitalSync.Sample.StateStored.Contracts;

namespace VitalSync.Sample.EventSourced.Tests;

// Stage 3 of the walking skeleton: the only test in the repository that crosses a service boundary.
//
//   StateStored: gRPC -> aggregate -> EF commit (aggregate + outbox, one transaction)
//                     -> integration event -> vitalsync.integration-events [sample.widget-created]
//   EventSourced: own queue bound with sample.* -> command via ISender -> Marten append + outbox
//                     -> projection -> eventsourced-read -> gRPC read
//
// Needs both services running, so it skips unless both URLs are set.
public sealed class CrossContextSmokeTests
{
    private static readonly TimeSpan MirrorTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task AWidgetCreatedInOneContext_AppearsAsAGadgetInTheOther()
    {
        var publisher = CreateStateStoredClient(out var publisherSkip);
        Assert.SkipWhen(publisher is null, publisherSkip);
        var consumer = CreateEventSourcedClient(out var consumerSkip);
        Assert.SkipWhen(consumer is null, consumerSkip);

        var name = "crossing-" + Guid.NewGuid().ToString("N")[..8];
        var created = await publisher!.CreateAsync(new StateStoredContracts.CreateWidgetRequest { Name = name });

        // The identity is shared on purpose - it is what makes the mirroring idempotent under at-least-once
        // delivery. The two services share nothing else: no database, no synchronous call.
        var mirrored = await WaitForMirrorAsync(consumer!, created.WidgetId);

        Assert.Equal(created.WidgetId, mirrored.GadgetId);
        Assert.Equal(name, mirrored.Name);
        Assert.Equal(0, mirrored.RenameCount);
        Assert.False(mirrored.IsRetired);
    }

    [Fact]
    public async Task RenamingTheMirroredGadget_DoesNotTravelBack()
    {
        var publisher = CreateStateStoredClient(out var publisherSkip);
        Assert.SkipWhen(publisher is null, publisherSkip);
        var consumer = CreateEventSourcedClient(out var consumerSkip);
        Assert.SkipWhen(consumer is null, consumerSkip);

        var created = await publisher!.CreateAsync(
            new StateStoredContracts.CreateWidgetRequest { Name = "one-way" });
        await WaitForMirrorAsync(consumer!, created.WidgetId);

        await consumer!.RenameAsync(new RenameGadgetRequest { GadgetId = created.WidgetId, Name = "diverged" });

        // The mirror is one-way by construction: the event-sourced context maps only GadgetRetired onto the
        // broker, so a rename here stays here. Asserting it keeps a future mapper change from quietly
        // introducing a feedback loop between the two contexts.
        var widget = await publisher.GetAsync(
            new StateStoredContracts.GetWidgetRequest { WidgetId = created.WidgetId });

        Assert.Equal("one-way", widget.Name);
        Assert.Equal(0, widget.RenameCount);
    }

    private static async Task<GadgetReply> WaitForMirrorAsync(IGadgetService consumer, string id)
    {
        var deadline = DateTime.UtcNow + MirrorTimeout;
        while (true)
        {
            try
            {
                return await consumer.GetAsync(new GetGadgetRequest { GadgetId = id });
            }
            catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
            {
                // Not mirrored yet. Two eventual-consistency hops now: the publisher's outbox to the broker,
                // and the consumer's own write-then-project cycle.
            }

            Assert.True(DateTime.UtcNow < deadline, $"The widget was not mirrored within {MirrorTimeout}.");
            await Task.Delay(500);
        }
    }

    private static StateStoredContracts.IWidgetService? CreateStateStoredClient(out string skipReason) =>
        CreateClient<StateStoredContracts.IWidgetService>("SAMPLE_STATESTORED_API_URL", out skipReason);

    private static IGadgetService? CreateEventSourcedClient(out string skipReason) =>
        CreateClient<IGadgetService>("SAMPLE_EVENTSOURCED_API_URL", out skipReason);

    private static TService? CreateClient<TService>(string variable, out string skipReason)
        where TService : class
    {
        var url = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(url))
        {
            SmokeRequirement.ThrowIfRequired(variable);
            skipReason = $"{variable} is not set; start the Aspire host and point it at the API.";
            return null;
        }

        skipReason = string.Empty;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

        var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = handler });
        return channel.CreateGrpcService<TService>();
    }
}
