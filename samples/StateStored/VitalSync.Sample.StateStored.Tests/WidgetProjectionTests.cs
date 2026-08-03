using BuildingBlocks.Application;
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

        await new WidgetCreatedProjection(context).Handle(created, MetadataFor(id, 1), TestContext.Current.CancellationToken);
        await new WidgetRenamedProjection(context).Handle(
            new WidgetRenamed(id, "second", 1), MetadataFor(id, 2), TestContext.Current.CancellationToken);
        await new WidgetCreatedProjection(context).Handle(created, MetadataFor(id, 1), TestContext.Current.CancellationToken);

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

        await new WidgetRenamedProjection(context).Handle(
            new WidgetRenamed(id, "second", 1), MetadataFor(id, 2), TestContext.Current.CancellationToken);
        await new WidgetCreatedProjection(context).Handle(
            new WidgetCreated(id, "first"), MetadataFor(id, 1), TestContext.Current.CancellationToken);

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

        await projection.Handle(new WidgetRenamed(id, "third", 2), MetadataFor(id, 3), TestContext.Current.CancellationToken);
        await projection.Handle(new WidgetRenamed(id, "second", 1), MetadataFor(id, 2), TestContext.Current.CancellationToken);

        var row = await context.Widgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("third", row.Name);
        Assert.Equal(2, row.RenameCount);
        Assert.Equal(3, row.Version);
    }

    private static DomainEventMetadata MetadataFor(WidgetId id, long version) =>
        new(Guid.NewGuid(), "widget", id.Value.ToString(), version, DateTimeOffset.UnixEpoch);

    private static WidgetReadDbContext NewContext() =>
        new(new DbContextOptionsBuilder<WidgetReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
