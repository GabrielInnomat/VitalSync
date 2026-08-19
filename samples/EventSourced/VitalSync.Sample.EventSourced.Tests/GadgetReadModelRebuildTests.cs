using GaWeCodes.Application.DomainEvents;
using GaWeCodes.Domain.Events;
using GaWeCodes.Testing;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Domain;
using VitalSync.Sample.EventSourced.Infrastructure.Read;

namespace VitalSync.Sample.EventSourced.Tests;

public sealed class GadgetReadModelRebuildTests
{
    [Fact]
    public async Task RebuildFromCurrentState_MatchesWhatTheLiveProjectionsProduced()
    {
        var gadget = BuildGadgetWithFullHistory();

        await using var projected = NewContext();
        await ProjectAsync(projected, gadget);

        await using var rebuilt = NewContext();
        await new GadgetReadModelRebuilder(rebuilt).RebuildAsync(gadget, TestContext.Current.CancellationToken);

        var fromEvents = await projected.Gadgets.SingleAsync(TestContext.Current.CancellationToken);
        var fromState = await rebuilt.Gadgets.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(fromEvents.Id, fromState.Id);
        Assert.Equal(fromEvents.Name, fromState.Name);
        Assert.Equal(fromEvents.RenameCount, fromState.RenameCount);
        Assert.Equal(fromEvents.IsRetired, fromState.IsRetired);
        Assert.Equal(fromEvents.Version, fromState.Version);
    }

    [Fact]
    public async Task RebuildFromCurrentState_DerivesEveryFieldAbsolutely()
    {
        var gadget = BuildGadgetWithFullHistory();

        await using var context = NewContext();
        await new GadgetReadModelRebuilder(context).RebuildAsync(gadget, TestContext.Current.CancellationToken);

        var row = await context.Gadgets.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal("renamed-twice", row.Name);
        Assert.Equal(2, row.RenameCount);
        Assert.True(row.IsRetired);
        Assert.Equal(5, row.Version);
    }

    [Fact]
    public async Task RebuiltReadModel_DiscardsAnEventItAlreadyContains()
    {
        var gadget = BuildGadgetWithFullHistory();

        await using var context = NewContext();
        await new GadgetReadModelRebuilder(context).RebuildAsync(gadget, TestContext.Current.CancellationToken);

        var rename = (GadgetRenamed)gadget.DomainEvents.First(@event => @event is GadgetRenamed);

        await new GadgetRenamedProjection(context).HandleAsync(
            rename,
            TestMetadata.For<Gadget>(gadget.Id, 2),
            TestContext.Current.CancellationToken);

        var row = await context.Gadgets.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal("renamed-twice", row.Name);
        Assert.Equal(5, row.Version);
    }

    private static Gadget BuildGadgetWithFullHistory()
    {
        var gadget = Gadget.Create(GadgetId.New(), "first");
        gadget.Rename("renamed-once");
        gadget.AddComponent("bolt");
        gadget.Rename("renamed-twice");
        gadget.Retire("obsolete");

        return gadget;
    }

    private static async Task ProjectAsync(GadgetReadDbContext context, Gadget gadget)
    {
        var version = 0L;

        foreach (var domainEvent in gadget.DomainEvents)
        {
            version++;
            var metadata = TestMetadata.For<Gadget>(gadget.Id, version);

            await DispatchAsync(context, domainEvent, metadata, TestContext.Current.CancellationToken);
        }
    }

    private static Task DispatchAsync(
        GadgetReadDbContext context,
        IDomainEvent domainEvent,
        DomainEventMetadata metadata,
        CancellationToken cancellationToken) => domainEvent switch
        {
            GadgetCreated created => new GadgetCreatedProjection(context).HandleAsync(created, metadata, cancellationToken),
            GadgetRenamed renamed => new GadgetRenamedProjection(context).HandleAsync(renamed, metadata, cancellationToken),
            GadgetRetired retired => new GadgetRetiredProjection(context).HandleAsync(retired, metadata, cancellationToken),
            GadgetComponentAdded => Task.CompletedTask,
            _ => throw new InvalidOperationException($"No projection is wired for '{domainEvent.GetType()}'."),
        };

    private static GadgetReadDbContext NewContext() =>
        new(new DbContextOptionsBuilder<GadgetReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
