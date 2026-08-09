using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Domain.Events;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;
using VitalSync.Sample.StateStored.Infrastructure.Read;

namespace VitalSync.Sample.StateStored.Tests;

public sealed class WidgetReadModelRebuildTests
{
    [Fact]
    public async Task RebuildFromCurrentState_MatchesWhatTheLiveProjectionsProduced()
    {
        var widget = BuildWidgetWithFullHistory();

        await using var projected = NewContext();
        await ProjectAsync(projected, widget);

        await using var rebuilt = NewContext();
        await new WidgetReadModelRebuilder(rebuilt).RebuildAsync(widget, TestContext.Current.CancellationToken);

        var fromEvents = await projected.Widgets.SingleAsync(TestContext.Current.CancellationToken);
        var fromState = await rebuilt.Widgets.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(fromEvents.Id, fromState.Id);
        Assert.Equal(fromEvents.Name, fromState.Name);
        Assert.Equal(fromEvents.RenameCount, fromState.RenameCount);
        Assert.Equal(fromEvents.PartCount, fromState.PartCount);
        Assert.Equal(fromEvents.TotalQuantity, fromState.TotalQuantity);
        Assert.Equal(fromEvents.Version, fromState.Version);
    }

    [Fact]
    public async Task RebuildFromCurrentState_DerivesEveryFieldAbsolutely()
    {
        var widget = BuildWidgetWithFullHistory();

        await using var context = NewContext();
        await new WidgetReadModelRebuilder(context).RebuildAsync(widget, TestContext.Current.CancellationToken);

        var row = await context.Widgets.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal("renamed", row.Name);
        Assert.Equal(1, row.RenameCount);
        Assert.Equal(1, row.PartCount);
        Assert.Equal(7, row.TotalQuantity);
        Assert.Equal(6, row.Version);
    }

    [Fact]
    public async Task RebuiltReadModel_LetsLaterEventsContinueIncrementally()
    {
        var widget = BuildWidgetWithFullHistory();

        await using var context = NewContext();
        await new WidgetReadModelRebuilder(context).RebuildAsync(widget, TestContext.Current.CancellationToken);

        widget.Rename("after-rebuild");
        var rename = (WidgetRenamed)widget.DomainEvents.Last();

        await new WidgetRenamedProjection(context).HandleAsync(
            rename,
            MetadataFor(widget.Id, 7),
            TestContext.Current.CancellationToken);

        var row = await context.Widgets.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal("after-rebuild", row.Name);
        Assert.Equal(7, row.Version);
    }

    private static Widget BuildWidgetWithFullHistory()
    {
        var widget = Widget.Create(WidgetId.New(), "first");
        widget.Rename("renamed");

        var kept = widget.AddPart("bolt", 3);
        var removed = widget.AddPart("nut", 1);

        widget.ChangePartQuantity(kept, 7);
        widget.RemovePart(removed);

        return widget;
    }

    private static async Task ProjectAsync(WidgetReadDbContext context, Widget widget)
    {
        var version = 0L;

        foreach (var domainEvent in widget.DomainEvents)
        {
            version++;
            var metadata = MetadataFor(widget.Id, version);
            var token = TestContext.Current.CancellationToken;

            await DispatchAsync(context, domainEvent, metadata, token);
        }
    }

    private static Task DispatchAsync(
        WidgetReadDbContext context,
        IDomainEvent domainEvent,
        DomainEventMetadata metadata,
        CancellationToken cancellationToken) => domainEvent switch
        {
            WidgetCreated created => new WidgetCreatedProjection(context).HandleAsync(created, metadata, cancellationToken),
            WidgetRenamed renamed => new WidgetRenamedProjection(context).HandleAsync(renamed, metadata, cancellationToken),
            WidgetPartAdded added => new WidgetPartAddedProjection(context).HandleAsync(added, metadata, cancellationToken),
            WidgetPartQuantityChanged changed =>
                new WidgetPartQuantityChangedProjection(context).HandleAsync(changed, metadata, cancellationToken),
            WidgetPartRemoved removed => new WidgetPartRemovedProjection(context).HandleAsync(removed, metadata, cancellationToken),
            _ => throw new InvalidOperationException($"No projection is wired for '{domainEvent.GetType()}'."),
        };

    private static DomainEventMetadata MetadataFor(WidgetId id, long version) =>
        new(Guid.NewGuid(), "widget", id.Value.ToString(), version, DateTimeOffset.UnixEpoch);

    private static WidgetReadDbContext NewContext() =>
        new(new DbContextOptionsBuilder<WidgetReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
