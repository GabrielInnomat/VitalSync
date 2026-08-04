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

    [Fact]
    public void AddComponent_AppendsTheChildToTheState()
    {
        var gadget = Gadget.Create(GadgetId.New(), "first");

        var componentId = gadget.AddComponent("lens");

        var component = Assert.Single(gadget.Components);
        Assert.Equal(componentId, component.Id);
        Assert.Equal("lens", component.Label);

        var raised = Assert.IsType<GadgetComponentAdded>(gadget.DomainEvents.Last());
        Assert.Equal(gadget.Id, raised.GadgetId);
        Assert.Equal(componentId, raised.ComponentId);
    }

    [Fact]
    public void Component_RelabelsItselfAndTheEventLandsOnTheRoot()
    {
        var gadget = Gadget.Create(GadgetId.New(), "first");
        var componentId = gadget.AddComponent("lens");

        gadget.Component(componentId).Relabel("mirror");

        Assert.Equal("mirror", gadget.Component(componentId).Label);
        Assert.Equal(3, gadget.DomainEvents.Count);
        Assert.Equal(3, ((IEventSourcedAggregateRoot<GadgetId>)gadget).Version);

        var raised = Assert.IsType<GadgetComponentRelabelled>(gadget.DomainEvents.Last());
        Assert.Equal(componentId, raised.ComponentId);
        Assert.Equal("mirror", raised.Label);
    }

    [Fact]
    public void Component_OfARetiredGadget_CannotBeChanged()
    {
        var gadget = Gadget.Create(GadgetId.New(), "first");
        var componentId = gadget.AddComponent("lens");
        var component = gadget.Component(componentId);

        gadget.Retire("obsolete");

        Assert.Throws<BusinessRuleViolationException>(() => component.Relabel("mirror"));
        Assert.Throws<BusinessRuleViolationException>(() => gadget.AddComponent("bracket"));
    }

    [Fact]
    public void Component_WithABlankLabel_ThrowsDomainValidation()
    {
        var gadget = Gadget.Create(GadgetId.New(), "first");
        var componentId = gadget.AddComponent("lens");

        Assert.Throws<DomainValidationException>(() => gadget.Component(componentId).Relabel(" "));
        Assert.Equal("lens", gadget.Component(componentId).Label);
    }

    [Fact]
    public void Component_ThatDoesNotExist_BreaksABusinessRule()
    {
        var gadget = Gadget.Create(GadgetId.New(), "first");

        Assert.Throws<BusinessRuleViolationException>(
            () => gadget.Component(GadgetComponentId.New()));
    }

    [Fact]
    public void LoadFromHistory_RebuildsChildrenRaisedByTheChildPath()
    {
        var id = GadgetId.New();
        var componentId = GadgetComponentId.New();
        IEnumerable<IDomainEvent> history =
        [
            new GadgetCreated(id, "first"),
            new GadgetComponentAdded(id, componentId, "lens"),
            new GadgetComponentRelabelled(id, componentId, "mirror"),
        ];

        var gadget = Reconstitute<Gadget>();
        ((IEventSourcedAggregateRoot<GadgetId>)gadget).LoadFromHistory(history);

        Assert.Equal("mirror", gadget.Component(componentId).Label);
        Assert.Equal(3, ((IEventSourcedAggregateRoot<GadgetId>)gadget).Version);
        Assert.Empty(gadget.DomainEvents);
    }

    private static TAggregate Reconstitute<TAggregate>()
        where TAggregate : class =>
        (TAggregate)Activator.CreateInstance(typeof(TAggregate), nonPublic: true)!;
}
