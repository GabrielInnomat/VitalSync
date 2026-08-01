using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;
using VitalSync.Sample.StateStored.Infrastructure.Read;

namespace VitalSync.Sample.StateStored.Tests;

// Found while building the event-sourced sample: the create handler used to write its name back on every
// delivery, so a redelivered WidgetCreated silently undid a rename in the read model. Nothing throws when a
// projection drifts, which is why the awkward deliveries are replayed here directly.
public sealed class WidgetProjectionTests
{
    [Fact]
    public async Task RedeliveredCreate_DoesNotUndoARename()
    {
        await using var context = NewContext();
        var id = WidgetId.New();
        var created = new WidgetCreated(id, "first");

        await new WidgetCreatedProjection(context).Handle(created, TestContext.Current.CancellationToken);
        await new WidgetRenamedProjection(context).Handle(
            new WidgetRenamed(id, "second", 1), TestContext.Current.CancellationToken);
        await new WidgetCreatedProjection(context).Handle(created, TestContext.Current.CancellationToken);

        var row = await context.Widgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("second", row.Name);
        Assert.Equal(1, row.RenameCount);
    }

    [Fact]
    public async Task RenameArrivingBeforeCreate_SurvivesAndIsNotOverwritten()
    {
        await using var context = NewContext();
        var id = WidgetId.New();

        await new WidgetRenamedProjection(context).Handle(
            new WidgetRenamed(id, "second", 1), TestContext.Current.CancellationToken);
        await new WidgetCreatedProjection(context).Handle(
            new WidgetCreated(id, "first"), TestContext.Current.CancellationToken);

        var row = await context.Widgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("second", row.Name);
        Assert.Equal(1, row.RenameCount);
    }

    [Fact]
    public async Task OlderRename_NeverOverwritesANewerOne()
    {
        await using var context = NewContext();
        var id = WidgetId.New();
        var projection = new WidgetRenamedProjection(context);

        await projection.Handle(new WidgetRenamed(id, "third", 2), TestContext.Current.CancellationToken);
        await projection.Handle(new WidgetRenamed(id, "second", 1), TestContext.Current.CancellationToken);

        var row = await context.Widgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("third", row.Name);
        Assert.Equal(2, row.RenameCount);
    }

    private static WidgetReadDbContext NewContext() =>
        new(new DbContextOptionsBuilder<WidgetReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
