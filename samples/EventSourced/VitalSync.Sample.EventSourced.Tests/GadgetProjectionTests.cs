using GaWeCodes.Testing;
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

        await new GadgetCreatedProjection(context).HandleAsync(created, TestMetadata.For<Gadget>(id, 1), TestContext.Current.CancellationToken);
        await new GadgetRenamedProjection(context).HandleAsync(
            new GadgetRenamed(id, "second", 1), TestMetadata.For<Gadget>(id, 2), TestContext.Current.CancellationToken);

        await new GadgetCreatedProjection(context).HandleAsync(created, TestMetadata.For<Gadget>(id, 1), TestContext.Current.CancellationToken);

        var row = await context.Gadgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("second", row.Name);
        Assert.Equal(1, row.RenameCount);
        Assert.Equal(2, row.Version);
    }

    [Fact]
    public async Task EventAtOrBelowTheWatermark_IsIgnored()
    {
        await using var context = NewContext();
        var id = GadgetId.New();
        var projection = new GadgetRenamedProjection(context);

        await projection.HandleAsync(new GadgetRenamed(id, "third", 2), TestMetadata.For<Gadget>(id, 3), TestContext.Current.CancellationToken);
        await projection.HandleAsync(new GadgetRenamed(id, "second", 1), TestMetadata.For<Gadget>(id, 2), TestContext.Current.CancellationToken);

        var row = await context.Gadgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("third", row.Name);
        Assert.Equal(2, row.RenameCount);
        Assert.Equal(3, row.Version);
    }

    [Fact]
    public async Task Retirement_IsTerminalUnderRedelivery()
    {
        await using var context = NewContext();
        var id = GadgetId.New();
        var retired = new GadgetRetired(id, "obsolete");

        await new GadgetCreatedProjection(context).HandleAsync(
            new GadgetCreated(id, "first"), TestMetadata.For<Gadget>(id, 1), TestContext.Current.CancellationToken);
        await new GadgetRetiredProjection(context).HandleAsync(retired, TestMetadata.For<Gadget>(id, 2), TestContext.Current.CancellationToken);
        await new GadgetRetiredProjection(context).HandleAsync(retired, TestMetadata.For<Gadget>(id, 2), TestContext.Current.CancellationToken);

        var row = await context.Gadgets.SingleAsync(TestContext.Current.CancellationToken);
        Assert.True(row.IsRetired);
        Assert.Equal("first", row.Name);
        Assert.Equal(2, row.Version);
    }

    private static GadgetReadDbContext NewContext() =>
        new(new DbContextOptionsBuilder<GadgetReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
