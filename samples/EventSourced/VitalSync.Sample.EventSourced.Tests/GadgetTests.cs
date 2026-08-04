using BuildingBlocks.Domain;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Tests;

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

        Assert.Throws<BusinessRuleViolationException>(() => gadget.Rename("later"));
        Assert.Throws<BusinessRuleViolationException>(() => gadget.Retire("again"));
    }

    [Fact]
    public void RaisingEvents_AdvancesTheVersion()
    {
        var gadget = Gadget.Create(GadgetId.New(), "first");
        gadget.Rename("second");

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

        var gadget = Reconstitute<Gadget>();
        ((IEventSourcedAggregateRoot<GadgetId>)gadget).LoadFromHistory(history);

        Assert.Equal(id, gadget.Id);
        Assert.Equal("second", gadget.Name);
        Assert.Equal(1, gadget.RenameCount);
        Assert.True(gadget.IsRetired);
        Assert.Equal(3, ((IEventSourcedAggregateRoot<GadgetId>)gadget).Version);

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
        where TAggregate : class =>
        (TAggregate)Activator.CreateInstance(typeof(TAggregate), nonPublic: true)!;
}
