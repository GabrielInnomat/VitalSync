namespace VitalSync.Sample.EventSourced.Domain;

[AggregateName("gadget")]
public sealed class Gadget : EventSourcedAggregateRoot<GadgetId, GadgetState>
{
    private Gadget() : base(GadgetState.Empty)
    {
    }

    public string Name => State.Name;

    public int RenameCount => State.RenameCount;

    public bool IsRetired => State.IsRetired;

    public static Gadget Create(GadgetId id, string name)
    {
        RuleChecker.CheckValidationRule(new GadgetNameMustNotBeEmpty(name));

        var gadget = new Gadget();
        gadget.RaiseEvent(new GadgetCreated(id, name));
        return gadget;
    }

    public void Rename(string name)
    {
        RuleChecker.CheckValidationRule(new GadgetNameMustNotBeEmpty(name));
        RuleChecker.CheckBusinessRule(new RetiredGadgetMustNotChange(IsRetired));

        RaiseEvent(new GadgetRenamed(Id, name, RenameCount + 1));
    }

    public void Retire(string reason)
    {
        RuleChecker.CheckBusinessRule(new RetiredGadgetMustNotChange(IsRetired));

        RaiseEvent(new GadgetRetired(Id, reason));
    }

    public IReadOnlyCollection<GadgetComponent> Components =>
        State.Components.Select(component => new GadgetComponent(this, component.Id)).ToList();

    public GadgetComponentId AddComponent(string label)
    {
        RuleChecker.CheckValidationRule(new GadgetComponentLabelMustNotBeEmpty(label));
        RuleChecker.CheckBusinessRule(new RetiredGadgetMustNotChange(IsRetired));

        var componentId = GadgetComponentId.New();
        RaiseEvent(new GadgetComponentAdded(Id, componentId, label));
        return componentId;
    }

    public GadgetComponent Component(GadgetComponentId componentId)
    {
        RuleChecker.CheckBusinessRule(new GadgetComponentMustExist(State.Components, componentId));

        return new GadgetComponent(this, componentId);
    }

    internal GadgetComponentState? FindComponent(GadgetComponentId componentId) =>
        State.Components.FirstOrDefault(component => component.Id == componentId);
}
