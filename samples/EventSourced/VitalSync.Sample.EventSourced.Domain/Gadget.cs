using BuildingBlocks.Domain;

namespace VitalSync.Sample.EventSourced.Domain;

public sealed class Gadget : EventSourcedAggregateRoot<GadgetId, GadgetState>, IReconstitutable<Gadget>
{
    private Gadget() : base(GadgetState.Empty)
    {
    }

    public string Name => State.Name;

    public int RenameCount => State.RenameCount;

    public bool IsRetired => State.IsRetired;

    static Gadget IReconstitutable<Gadget>.CreateEmpty() => new();

    public static Gadget Create(GadgetId id, string name)
    {
        RuleChecker.Check(new GadgetNameMustNotBeEmpty(name));

        var gadget = new Gadget();
        gadget.RaiseEvent(new GadgetCreated(id, name));
        return gadget;
    }

    public void Rename(string name)
    {
        RuleChecker.Check(new GadgetNameMustNotBeEmpty(name));
        RuleChecker.Check(new RetiredGadgetMustNotChange(IsRetired));

        RaiseEvent(new GadgetRenamed(Id, name, RenameCount + 1));
    }

    public void Retire(string reason)
    {
        RuleChecker.Check(new RetiredGadgetMustNotChange(IsRetired));

        RaiseEvent(new GadgetRetired(Id, reason));
    }
}
