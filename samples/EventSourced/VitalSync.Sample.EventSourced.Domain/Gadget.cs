using BuildingBlocks.Domain;

namespace VitalSync.Sample.EventSourced.Domain;

// The event-sourced counterpart of the state-stored sample's Widget. The only difference is the base class:
// EventSourcedAggregateRoot adds Version and LoadFromHistory and nothing else (ADR-0025), so the business
// logic below is written exactly as it would be for a state-stored aggregate. That is the claim this sample
// is here to test - and the parameterless constructor is not cosmetic: MartenEventSourcedRepository
// constrains TAggregate to new() because it rehydrates by folding the raw stream into an empty instance.
public sealed class Gadget() : EventSourcedAggregateRoot<GadgetId, GadgetState>(GadgetState.Empty)
{
    public string Name => State.Name;

    public int RenameCount => State.RenameCount;

    public bool IsRetired => State.IsRetired;

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

    // Retiring is a state change, not a removal: IRepository has no Remove (ADR-0026), and in an event-sourced
    // context deleting would mean rewriting history.
    public void Retire(string reason)
    {
        RuleChecker.Check(new RetiredGadgetMustNotChange(IsRetired));

        RaiseEvent(new GadgetRetired(Id, reason));
    }
}
