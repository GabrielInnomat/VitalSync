using GaWeCodes.Testing;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;
using VitalSync.Sample.StateStored.Infrastructure.Read;

namespace VitalSync.Sample.StateStored.Tests;

public sealed class WidgetProjectionTests
{
    [Fact]
    public async Task RedeliveredCreate_DoesNotUndoARename()
    {
        await using var context = NewContext();
        var id = WidgetId.New();
        var created = new WidgetCreated(id, "first");

        await new WidgetCreatedProjection(context).HandleAsync(created, TestMetadata.For<Widget>(id, 1), TestContext.Current.CancellationToken);
        await new WidgetRenamedProjection(context).HandleAsync(
            new WidgetRenamed(id, "second", 1), TestMetadata.For<Widget>(id, 2), TestContext.Current.CancellationToken);
        await new WidgetCreatedProjection(context).HandleAsync(created, TestMetadata.For<Widget>(id, 1), TestContext.Current.CancellationToken);

        var row = await context.Widgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("second", row.Name);
        Assert.Equal(1, row.RenameCount);
        Assert.Equal(2, row.Version);
    }

    [Fact]
    public async Task EventBelowTheWatermark_IsIgnored()
    {
        await using var context = NewContext();
        var id = WidgetId.New();

        await new WidgetRenamedProjection(context).HandleAsync(
            new WidgetRenamed(id, "second", 1), TestMetadata.For<Widget>(id, 2), TestContext.Current.CancellationToken);
        await new WidgetCreatedProjection(context).HandleAsync(
            new WidgetCreated(id, "first"), TestMetadata.For<Widget>(id, 1), TestContext.Current.CancellationToken);

        var row = await context.Widgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("second", row.Name);
        Assert.Equal(1, row.RenameCount);
    }

    [Fact]
    public async Task StaleRedelivery_NeverOverwritesNewerState()
    {
        await using var context = NewContext();
        var id = WidgetId.New();
        var projection = new WidgetRenamedProjection(context);

        await projection.HandleAsync(new WidgetRenamed(id, "third", 2), TestMetadata.For<Widget>(id, 3), TestContext.Current.CancellationToken);
        await projection.HandleAsync(new WidgetRenamed(id, "second", 1), TestMetadata.For<Widget>(id, 2), TestContext.Current.CancellationToken);

        var row = await context.Widgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("third", row.Name);
        Assert.Equal(2, row.RenameCount);
        Assert.Equal(3, row.Version);
    }

    [Fact]
    public async Task PartEvents_MaintainTheCountersOnTheReadModel()
    {
        await using var context = NewContext();
        var id = WidgetId.New();
        var first = WidgetPartId.New();
        var second = WidgetPartId.New();
        var token = TestContext.Current.CancellationToken;

        await new WidgetCreatedProjection(context).HandleAsync(new WidgetCreated(id, "first"), TestMetadata.For<Widget>(id, 1), token);
        await new WidgetPartAddedProjection(context)
            .HandleAsync(new WidgetPartAdded(id, first, "bolt", 3), TestMetadata.For<Widget>(id, 2), token);
        await new WidgetPartAddedProjection(context)
            .HandleAsync(new WidgetPartAdded(id, second, "nut", 1), TestMetadata.For<Widget>(id, 3), token);
        await new WidgetPartQuantityChangedProjection(context)
            .HandleAsync(new WidgetPartQuantityChanged(id, first, 7, 3), TestMetadata.For<Widget>(id, 4), token);
        await new WidgetPartRemovedProjection(context)
            .HandleAsync(new WidgetPartRemoved(id, second, 1), TestMetadata.For<Widget>(id, 5), token);

        var row = await context.Widgets.SingleAsync(token);
        Assert.Equal(1, row.PartCount);
        Assert.Equal(7, row.TotalQuantity);
        Assert.Equal(5, row.Version);
    }

    [Fact]
    public async Task RedeliveredPartEvent_IsIgnoredByTheWatermark()
    {
        await using var context = NewContext();
        var id = WidgetId.New();
        var partId = WidgetPartId.New();
        var token = TestContext.Current.CancellationToken;
        var added = new WidgetPartAdded(id, partId, "bolt", 3);

        await new WidgetCreatedProjection(context).HandleAsync(new WidgetCreated(id, "first"), TestMetadata.For<Widget>(id, 1), token);
        await new WidgetPartAddedProjection(context).HandleAsync(added, TestMetadata.For<Widget>(id, 2), token);
        await new WidgetPartAddedProjection(context).HandleAsync(added, TestMetadata.For<Widget>(id, 2), token);

        var row = await context.Widgets.SingleAsync(token);
        Assert.Equal(1, row.PartCount);
        Assert.Equal(3, row.TotalQuantity);
    }

    private static WidgetReadDbContext NewContext() =>
        new(new DbContextOptionsBuilder<WidgetReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
