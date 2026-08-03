using BuildingBlocks.Domain;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Tests;

// Pins the aggregate before Marten enters the picture: if the round trip breaks later, these say whether the
// fold itself was ever correct.
public sealed class GadgetTests
{
    [Fact]
    public void Create_RaisesGadgetCreatedAndAdoptsTheIdentity()
    {
        var id = GadgetId.New();

        var gadget = Gadget.Create(id, "first");

        Assert.Equal(id, gadget.Id);
        Assert.Equal("first", gadget.Name);
        Assert.Equal(0, gadget.RenameCount);
        Assert.False(gadget.IsRetired);

        var raised = Assert.IsType<GadgetCreated>(Assert.Single(gadget.DomainEvents));
        Assert.Equal(id, raised.GadgetId);
        Assert.Equal("first", raised.Name);
    }

    [Fact]
    public void Rename_RaisesGadgetRenamedAndFoldsTheNewName()
    {
        var gadget = Gadget.Create(GadgetId.New(), "first");

        gadget.Rename("second");

        Assert.Equal("second", gadget.Name);
        Assert.Equal(1, gadget.RenameCount);
        Assert.Equal(2, gadget.DomainEvents.Count);
        Assert.IsType<GadgetRenamed>(gadget.DomainEvents.Last());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainValidation(string name)
    {
        Assert.Throws<DomainValidationException>(() => Gadget.Create(GadgetId.New(), name));
    }

    [Fact]
    public void Retire_MakesFurtherChangesViolateABusinessRule()
    {
        var gadget = Gadget.Create(GadgetId.New(), "first");

        gadget.Retire("obsolete");

        Assert.True(gadget.IsRetired);

        // A broken business rule, not a validation error: the two travel different paths through the
        // pipeline and end up as different transport statuses.
        Assert.Throws<BusinessRuleViolationException>(() => gadget.Rename("later"));
        Assert.Throws<BusinessRuleViolationException>(() => gadget.Retire("again"));
    }

    [Fact]
    public void RaisingEvents_AdvancesTheVersion()
    {
        var gadget = Gadget.Create(GadgetId.New(), "first");
        gadget.Rename("second");

        // Version is implemented explicitly, so only infrastructure sees it - the domain cannot accidentally
        // build behavior on the stream position.
        Assert.Equal(2, ((IEventSourcedAggregateRoot<GadgetId>)gadget).Version);
    }

    [Fact]
    public void LoadFromHistory_RebuildsStateAndVersionWithoutUncommittedEvents()
    {
        var id = GadgetId.New();
        IEnumerable<IDomainEvent> history =
        [
            new GadgetCreated(id, "first"),
            new GadgetRenamed(id, "second", 1),
            new GadgetRetired(id, "obsolete"),
        ];

        // This is the only route to an empty hull, and it is the same one the repository takes: a static
        // abstract member is reachable only through a type parameter constrained to IReconstitutable, so
        // `new Gadget()` and `Gadget.CreateEmpty()` are both compile errors here.
        var gadget = Reconstitute<Gadget>();
        ((IEventSourcedAggregateRoot<GadgetId>)gadget).LoadFromHistory(history);

        Assert.Equal(id, gadget.Id);
        Assert.Equal("second", gadget.Name);
        Assert.Equal(1, gadget.RenameCount);
        Assert.True(gadget.IsRetired);
        Assert.Equal(3, ((IEventSourcedAggregateRoot<GadgetId>)gadget).Version);

        // Replay must not look like new work: appending these again would duplicate the whole stream.
        Assert.Empty(gadget.DomainEvents);
    }

    [Fact]
    public void LoadFromHistory_AfterAnEventWasRaised_Throws()
    {
        var gadget = Gadget.Create(GadgetId.New(), "first");

        Assert.Throws<InvalidOperationException>(
            () => ((IEventSourcedAggregateRoot<GadgetId>)gadget).LoadFromHistory([]));
    }

    private static TAggregate Reconstitute<TAggregate>()
        where TAggregate : IReconstitutable<TAggregate> => TAggregate.CreateEmpty();
}
