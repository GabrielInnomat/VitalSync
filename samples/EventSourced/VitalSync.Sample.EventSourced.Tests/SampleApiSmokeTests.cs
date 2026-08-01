using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using VitalSync.Sample.EventSourced.Contracts;

namespace VitalSync.Sample.EventSourced.Tests;

// Drives the running service through the whole chain: gRPC -> ISender -> aggregate -> Marten stream append
// with the outbox in one transaction -> projection into the read database -> query served from there.
//
// Skipped unless SAMPLE_EVENTSOURCED_API_URL points at a running instance:
//
//   dotnet run --project samples/VitalSync.Samples.AppHost
//   SAMPLE_EVENTSOURCED_API_URL=https://localhost:<port> dotnet test samples/EventSourced/VitalSync.Sample.EventSourced.Tests
public sealed class SampleApiSmokeTests
{
    private static readonly TimeSpan ProjectionTimeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task CreateThenGet_TravelsFromEventStoreToReadDatabase()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

        var name = "gadget-" + Guid.NewGuid().ToString("N")[..8];
        var created = await client!.CreateAsync(new CreateGadgetRequest { Name = name });

        Assert.False(string.IsNullOrWhiteSpace(created.GadgetId));

        var view = await WaitForProjectionAsync(client, created.GadgetId);

        Assert.Equal(created.GadgetId, view.GadgetId);
        Assert.Equal(name, view.Name);
        Assert.Equal(0, view.RenameCount);
        Assert.False(view.IsRetired);
    }

    [Fact]
    public async Task Rename_ReplaysTheStreamAndIsProjected()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

        var created = await client!.CreateAsync(new CreateGadgetRequest { Name = "before" });
        await WaitForProjectionAsync(client, created.GadgetId);

        // Renaming twice matters here: the second command has to load a two-event stream, fold it, and append
        // at the right expected version. A broken version would surface as a concurrency failure.
        await client.RenameAsync(new RenameGadgetRequest { GadgetId = created.GadgetId, Name = "after" });
        await client.RenameAsync(new RenameGadgetRequest { GadgetId = created.GadgetId, Name = "final" });

        var view = await WaitForProjectionAsync(client, created.GadgetId, v => v.Name == "final");
        Assert.Equal(2, view.RenameCount);
    }

    [Fact]
    public async Task RetiringTwice_IsReportedAsAFailedPrecondition()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

        var created = await client!.CreateAsync(new CreateGadgetRequest { Name = "doomed" });
        await client.RetireAsync(new RetireGadgetRequest { GadgetId = created.GadgetId, Reason = "obsolete" });

        var view = await WaitForProjectionAsync(client, created.GadgetId, v => v.IsRetired);
        Assert.True(view.IsRetired);

        // A broken business rule, translated to FailureCategory.BusinessRule by the pipeline and mapped onto
        // a different status than a validation error - the distinction the state-stored sample never exercised.
        var thrown = await Assert.ThrowsAsync<RpcException>(
            () => client.RetireAsync(
                new RetireGadgetRequest { GadgetId = created.GadgetId, Reason = "again" }).AsTask());

        Assert.Equal(StatusCode.FailedPrecondition, thrown.StatusCode);
    }

    [Fact]
    public async Task BlankName_IsRejectedAsInvalidArgument()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

        var thrown = await Assert.ThrowsAsync<RpcException>(
            () => client!.CreateAsync(new CreateGadgetRequest { Name = "   " }).AsTask());

        Assert.Equal(StatusCode.InvalidArgument, thrown.StatusCode);
    }

    [Fact]
    public async Task UnknownGadget_IsReportedAsNotFound()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

        var thrown = await Assert.ThrowsAsync<RpcException>(
            () => client!.GetAsync(new GetGadgetRequest { GadgetId = Guid.NewGuid().ToString() }).AsTask());

        Assert.Equal(StatusCode.NotFound, thrown.StatusCode);
    }

    private static async Task<GadgetReply> WaitForProjectionAsync(
        IGadgetService client,
        string gadgetId,
        Func<GadgetReply, bool>? until = null)
    {
        var deadline = DateTime.UtcNow + ProjectionTimeout;
        while (true)
        {
            try
            {
                var view = await client.GetAsync(new GetGadgetRequest { GadgetId = gadgetId });
                if (until is null || until(view))
                {
                    return view;
                }
            }
            catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
            {
                // Not projected yet - reads are eventually consistent with writes (ADR-0022), so polling is
                // the honest assertion, not a fixed delay.
            }

            Assert.True(DateTime.UtcNow < deadline, $"The projection did not catch up within {ProjectionTimeout}.");
            await Task.Delay(250);
        }
    }

    private static IGadgetService? CreateClient(out string skipReason)
    {
        var url = Environment.GetEnvironmentVariable("SAMPLE_EVENTSOURCED_API_URL");
        if (string.IsNullOrWhiteSpace(url))
        {
            skipReason = "SAMPLE_EVENTSOURCED_API_URL is not set; start the Aspire host and point it at the API.";
            return null;
        }

        skipReason = string.Empty;

        // The Aspire host serves HTTPS with the ASP.NET development certificate, which this process has no
        // reason to trust.
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

        var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = handler });
        return channel.CreateGrpcService<IGadgetService>();
    }
}
