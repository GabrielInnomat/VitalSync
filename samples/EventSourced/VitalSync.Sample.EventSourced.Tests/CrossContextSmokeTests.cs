using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using VitalSync.Sample.EventSourced.Contracts;
using StateStoredContracts = VitalSync.Sample.StateStored.Contracts;

namespace VitalSync.Sample.EventSourced.Tests;

public sealed class CrossContextSmokeTests
{
    private static readonly TimeSpan MirrorTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan DivergenceWindow = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task AWidgetCreatedInOneContext_AppearsAsAGadgetInTheOther()
    {
        var publisher = CreateStateStoredClient(out var publisherSkip);
        Assert.SkipWhen(publisher is null, publisherSkip);
        var consumer = CreateEventSourcedClient(out var consumerSkip);
        Assert.SkipWhen(consumer is null, consumerSkip);

        var name = "crossing-" + Guid.NewGuid().ToString("N")[..8];
        var created = await publisher!.CreateAsync(new StateStoredContracts.CreateWidgetRequest { Name = name });

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

        var deadline = DateTime.UtcNow + DivergenceWindow;
        while (DateTime.UtcNow < deadline)
        {
            var polled = await publisher.GetAsync(
                new StateStoredContracts.GetWidgetRequest { WidgetId = created.WidgetId });

            Assert.Equal("one-way", polled.Name);
            Assert.Equal(0, polled.RenameCount);

            await Task.Delay(250, TestContext.Current.CancellationToken);
        }
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
