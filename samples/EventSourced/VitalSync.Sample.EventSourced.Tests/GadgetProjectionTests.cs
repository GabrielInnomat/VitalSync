using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Domain;
using VitalSync.Sample.EventSourced.Infrastructure.Read;

namespace VitalSync.Sample.EventSourced.Tests;

public sealed class GadgetProjectionTests
{
    [Fact]
    public async Task RedeliveredCreate_DoesNotUndoARename()
    {
        await using var context = NewContext();
        var id = GadgetId.New();
        var created = new GadgetCreated(id, "first");

        await new GadgetCreatedProjection(context).Handle(created, TestContext.Current.CancellationToken);
        await new GadgetRenamedProjection(context).Handle(
            new GadgetRenamed(id, "second", 1), TestContext.Current.CancellationToken);

        await new GadgetCreatedProjection(context).Handle(created, TestContext.Current.CancellationToken);

        var row = await context.Gadgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("second", row.Name);
        Assert.Equal(1, row.RenameCount);
    }

    [Fact]
    public async Task RenameArrivingBeforeCreate_SurvivesAndIsNotOverwritten()
    {
        await using var context = NewContext();
        var id = GadgetId.New();

        await new GadgetRenamedProjection(context).Handle(
            new GadgetRenamed(id, "second", 1), TestContext.Current.CancellationToken);
        await new GadgetCreatedProjection(context).Handle(
            new GadgetCreated(id, "first"), TestContext.Current.CancellationToken);

        var row = await context.Gadgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("second", row.Name);
        Assert.Equal(1, row.RenameCount);
    }

    [Fact]
    public async Task OlderRename_NeverOverwritesANewerOne()
    {
        await using var context = NewContext();
        var id = GadgetId.New();
        var projection = new GadgetRenamedProjection(context);

        await projection.Handle(new GadgetRenamed(id, "third", 2), TestContext.Current.CancellationToken);
        await projection.Handle(new GadgetRenamed(id, "second", 1), TestContext.Current.CancellationToken);

        var row = await context.Gadgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("third", row.Name);
        Assert.Equal(2, row.RenameCount);
    }

    [Fact]
    public async Task Retirement_IsTerminalAndOrderIndependent()
    {
        await using var context = NewContext();
        var id = GadgetId.New();
        var retired = new GadgetRetired(id, "obsolete");

        await new GadgetRetiredProjection(context).Handle(retired, TestContext.Current.CancellationToken);
        await new GadgetCreatedProjection(context).Handle(
            new GadgetCreated(id, "first"), TestContext.Current.CancellationToken);
        await new GadgetRetiredProjection(context).Handle(retired, TestContext.Current.CancellationToken);

        var row = await context.Gadgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.True(row.IsRetired);
        Assert.Equal("first", row.Name);
    }

    private static GadgetReadDbContext NewContext() =>
        new(new DbContextOptionsBuilder<GadgetReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
