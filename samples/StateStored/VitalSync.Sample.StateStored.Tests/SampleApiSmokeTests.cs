using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using VitalSync.Sample.StateStored.Contracts;

namespace VitalSync.Sample.StateStored.Tests;

// Drives the running service through the whole chain: gRPC -> ISender -> aggregate -> EF commit with the
// outbox in one transaction -> projection into the read database -> query served from there.
//
// Skipped unless SAMPLE_STATESTORED_API_URL points at a running instance, because it needs the Aspire host up:
//
//   dotnet run --project samples/VitalSync.Samples.AppHost
//   SAMPLE_STATESTORED_API_URL=https://localhost:<port> dotnet run --project samples/StateStored/VitalSync.Sample.StateStored.Tests
//
// It doubles as the reference for how the BFF will consume the service: the same contract assembly, a
// channel, and CreateGrpcService<T>() - no generated stubs anywhere.
public sealed class SampleApiSmokeTests
{
    private static readonly TimeSpan ProjectionTimeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task CreateThenGet_TravelsFromWriteToReadDatabase()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

        var name = "widget-" + Guid.NewGuid().ToString("N")[..8];
        var created = await client!.CreateAsync(new CreateWidgetRequest { Name = name });

        Assert.False(string.IsNullOrWhiteSpace(created.WidgetId));

        // Reads are eventually consistent with writes (ADR-0022): the projection runs after the write
        // transaction commits, so polling is the honest way to assert it - not a fixed delay.
        var view = await WaitForProjectionAsync(client, created.WidgetId);

        Assert.Equal(created.WidgetId, view.WidgetId);
        Assert.Equal(name, view.Name);
        Assert.Equal(0, view.RenameCount);
    }

    [Fact]
    public async Task Rename_IsProjectedAndCounted()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

        var created = await client!.CreateAsync(new CreateWidgetRequest { Name = "before" });
        await WaitForProjectionAsync(client, created.WidgetId);

        await client.RenameAsync(new RenameWidgetRequest { WidgetId = created.WidgetId, Name = "after" });

        var view = await WaitForProjectionAsync(client, created.WidgetId, v => v.Name == "after");
        Assert.Equal(1, view.RenameCount);
    }

    [Fact]
    public async Task BlankName_IsRejectedAsInvalidArgument()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

        // The domain rule surfaces as DomainValidationException, is translated to
        // Result.Failure(Validation) by the pipeline, and the host maps that onto a gRPC status.
        var thrown = await Assert.ThrowsAsync<RpcException>(
            () => client!.CreateAsync(new CreateWidgetRequest { Name = "   " }).AsTask());

        Assert.Equal(StatusCode.InvalidArgument, thrown.StatusCode);
    }

    [Fact]
    public async Task UnknownWidget_IsReportedAsNotFound()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

        var thrown = await Assert.ThrowsAsync<RpcException>(
            () => client!.GetAsync(new GetWidgetRequest { WidgetId = Guid.NewGuid().ToString() }).AsTask());

        Assert.Equal(StatusCode.NotFound, thrown.StatusCode);
    }

    private static async Task<WidgetReply> WaitForProjectionAsync(
        IWidgetService client,
        string widgetId,
        Func<WidgetReply, bool>? until = null)
    {
        var deadline = DateTime.UtcNow + ProjectionTimeout;
        while (true)
        {
            try
            {
                var view = await client.GetAsync(new GetWidgetRequest { WidgetId = widgetId });
                if (until is null || until(view))
                {
                    return view;
                }
            }
            catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
            {
                // Not projected yet.
            }

            Assert.True(DateTime.UtcNow < deadline, $"The projection did not catch up within {ProjectionTimeout}.");
            await Task.Delay(250);
        }
    }

    private static IWidgetService? CreateClient(out string skipReason)
    {
        var url = Environment.GetEnvironmentVariable("SAMPLE_STATESTORED_API_URL");
        if (string.IsNullOrWhiteSpace(url))
        {
            skipReason = "SAMPLE_STATESTORED_API_URL is not set; start the Aspire host and point it at the API.";
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
        return channel.CreateGrpcService<IWidgetService>();
    }
}
