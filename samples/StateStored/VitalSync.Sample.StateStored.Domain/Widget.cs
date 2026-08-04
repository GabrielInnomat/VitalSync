using BuildingBlocks.Domain;

namespace VitalSync.Sample.StateStored.Domain;

[AggregateName("widget")]
public sealed class Widget : AggregateRoot<WidgetId, WidgetState>
{
    private Widget() : base(WidgetState.Empty)
    {
    }

    public string Name => State.Name;

    public int RenameCount => State.RenameCount;

    public IReadOnlyCollection<WidgetPart> Parts => State.Parts;

    public static Widget Create(WidgetId id, string name)
    {
        RuleChecker.Check(new WidgetNameMustNotBeEmpty(name));

        var widget = new Widget();
        widget.RaiseEvent(new WidgetCreated(id, name));
        return widget;
    }

    public void Rename(string name)
    {
        RuleChecker.Check(new WidgetNameMustNotBeEmpty(name));

        RaiseEvent(new WidgetRenamed(Id, name, RenameCount + 1));
    }

    public WidgetPartId AddPart(string label, int quantity)
    {
        RuleChecker.Check(new WidgetPartLabelMustNotBeEmpty(label));
        RuleChecker.Check(new WidgetPartQuantityMustBePositive(quantity));

        var partId = WidgetPartId.New();
        RaiseEvent(new WidgetPartAdded(Id, partId, label, quantity));
        return partId;
    }

    public void ChangePartQuantity(WidgetPartId partId, int quantity)
    {
        RuleChecker.Check(new WidgetPartQuantityMustBePositive(quantity));
        RuleChecker.Check(new WidgetPartMustExist(Parts, partId));

        RaiseEvent(new WidgetPartQuantityChanged(Id, partId, quantity, PartOrThrow(partId).Quantity));
    }

    public void RemovePart(WidgetPartId partId)
    {
        RuleChecker.Check(new WidgetPartMustExist(Parts, partId));

        RaiseEvent(new WidgetPartRemoved(Id, partId, PartOrThrow(partId).Quantity));
    }

    private WidgetPart PartOrThrow(WidgetPartId partId) => Parts.First(part => part.Id == partId);
}
