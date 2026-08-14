namespace VitalSync.Sample.EventSourced.Domain;

public sealed class GadgetComponent : Entity<GadgetComponentId, GadgetComponentState>
{
    private readonly Gadget _gadget;

    internal GadgetComponent(Gadget gadget, GadgetComponentId id)
        : base(gadget, id, gadget.FindComponent)
    {
        _gadget = gadget;
    }

    public string Label => GetCurrentState().Label;

    public void Relabel(string label)
    {
        RuleChecker.CheckValidationRule(new GadgetComponentLabelMustNotBeEmpty(label));
        RuleChecker.CheckBusinessRule(new RetiredGadgetMustNotChange(_gadget.IsRetired));

        RaiseEvent(new GadgetComponentRelabelled(_gadget.Id, Id, label));
    }
}
