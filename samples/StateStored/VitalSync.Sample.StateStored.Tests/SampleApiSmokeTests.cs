using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using VitalSync.Sample.StateStored.Contracts;

namespace VitalSync.Sample.StateStored.Tests;

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
    public async Task Parts_SurviveTheRoundTripThroughTheWriteDatabase()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

        var created = await client!.CreateAsync(new CreateWidgetRequest { Name = "with-parts" });
        await WaitForProjectionAsync(client, created.WidgetId);

        var bolt = await client.AddPartAsync(
            new AddWidgetPartRequest { WidgetId = created.WidgetId, Label = "bolt", Quantity = 3 });
        var nut = await client.AddPartAsync(
            new AddWidgetPartRequest { WidgetId = created.WidgetId, Label = "nut", Quantity = 1 });

        var afterAdd = await WaitForProjectionAsync(client, created.WidgetId, view => view.PartCount == 2);
        Assert.Equal(4, afterAdd.TotalQuantity);

        await client.ChangePartQuantityAsync(new ChangeWidgetPartQuantityRequest
        {
            WidgetId = created.WidgetId,
            PartId = bolt.PartId,
            Quantity = 7,
        });

        var afterChange = await WaitForProjectionAsync(client, created.WidgetId, view => view.TotalQuantity == 8);
        Assert.Equal(2, afterChange.PartCount);

        var removed = await client.RemovePartAsync(
            new RemoveWidgetPartRequest { WidgetId = created.WidgetId, PartId = nut.PartId });

        Assert.Equal("nut", removed.Label);

        var afterRemove = await WaitForProjectionAsync(client, created.WidgetId, view => view.PartCount == 1);
        Assert.Equal(7, afterRemove.TotalQuantity);
    }

    [Fact]
    public async Task RemovingAPartTwice_IsRejectedAsAFailedPrecondition()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

        var created = await client!.CreateAsync(new CreateWidgetRequest { Name = "double-remove" });
        var part = await client.AddPartAsync(
            new AddWidgetPartRequest { WidgetId = created.WidgetId, Label = "washer", Quantity = 2 });

        await client.RemovePartAsync(new RemoveWidgetPartRequest
        {
            WidgetId = created.WidgetId,
            PartId = part.PartId,
        });

        var thrown = await Assert.ThrowsAsync<RpcException>(
            () => client.RemovePartAsync(new RemoveWidgetPartRequest
            {
                WidgetId = created.WidgetId,
                PartId = part.PartId,
            }).AsTask());

        Assert.Equal(StatusCode.FailedPrecondition, thrown.StatusCode);
    }

    [Fact]
    public async Task BlankName_IsRejectedAsInvalidArgument()
    {
        var client = CreateClient(out var skipReason);
        Assert.SkipWhen(client is null, skipReason);

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
            }

            Assert.True(DateTime.UtcNow < deadline, $"The projection did not catch up within {ProjectionTimeout}.");
            await Task.Delay(250);
        }
    }

    private static IWidgetService? CreateClient(out string skipReason)
    {
        const string variable = "SAMPLE_STATESTORED_API_URL";

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
        return channel.CreateGrpcService<IWidgetService>();
    }
}
