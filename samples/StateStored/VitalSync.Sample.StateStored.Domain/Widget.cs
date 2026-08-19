using GaWeCodes.Domain.Aggregates;
using GaWeCodes.Domain.Entities;
using GaWeCodes.Domain.Naming;
using GaWeCodes.Domain.Rules;

namespace VitalSync.Sample.StateStored.Domain;

[AggregateName("widget")]
public sealed class Widget : AggregateRoot<WidgetId, WidgetState>,
    IChildOwner<WidgetPartId, WidgetPartState>
{
    private Widget() : base(WidgetState.Empty)
    {
    }

    public string Name => State.Name;

    public int RenameCount => State.RenameCount;

    public IReadOnlyCollection<WidgetPart> Parts =>
        State.Parts.Select(part => new WidgetPart(this, part.Id)).ToList();

    public static Widget Create(WidgetId id, string name)
    {
        RuleChecker.CheckValidationRule(new WidgetNameMustNotBeEmpty(name));

        var widget = new Widget();
        widget.RaiseEvent(new WidgetCreated(id, name));
        return widget;
    }

    public void Rename(string name)
    {
        RuleChecker.CheckValidationRule(new WidgetNameMustNotBeEmpty(name));

        RaiseEvent(new WidgetRenamed(Id, name, RenameCount + 1));
    }

    public WidgetPartId AddPart(string label, int quantity)
    {
        RuleChecker.CheckAllValidationRules(
            new WidgetPartLabelMustNotBeEmpty(label),
            new WidgetPartQuantityMustBePositive(quantity));

        var partId = WidgetPartId.New();
        RaiseEvent(new WidgetPartAdded(Id, partId, label, quantity));
        return partId;
    }

    public void ChangePartQuantity(WidgetPartId partId, int quantity)
    {
        RuleChecker.CheckBusinessRule(new WidgetPartMustExist(State.Parts, partId));

        Part(partId).ChangeQuantity(quantity);
    }

    public void RemovePart(WidgetPartId partId)
    {
        RuleChecker.CheckBusinessRule(new WidgetPartMustExist(State.Parts, partId));

        RaiseEvent(new WidgetPartRemoved(Id, partId, PartStateOrThrow(partId).Quantity));
    }

    public WidgetPart Part(WidgetPartId partId)
    {
        RuleChecker.CheckBusinessRule(new WidgetPartMustExist(State.Parts, partId));

        return new WidgetPart(this, partId);
    }

    internal WidgetPartState? FindPart(WidgetPartId partId) =>
        State.Parts.FirstOrDefault(part => part.Id == partId);

    WidgetPartState? IChildOwner<WidgetPartId, WidgetPartState>.FindChild(WidgetPartId childId) =>
        FindPart(childId);

    private WidgetPartState PartStateOrThrow(WidgetPartId partId) =>
        State.Parts.First(part => part.Id == partId);
}
