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

    public IReadOnlyCollection<WidgetPart> Parts =>
        State.Parts.Select(part => new WidgetPart(this, part.Id)).ToList();

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
        RuleChecker.Check(new WidgetPartMustExist(State.Parts, partId));

        Part(partId).ChangeQuantity(quantity);
    }

    public void RemovePart(WidgetPartId partId)
    {
        RuleChecker.Check(new WidgetPartMustExist(State.Parts, partId));

        RaiseEvent(new WidgetPartRemoved(Id, partId, PartStateOrThrow(partId).Quantity));
    }

    public WidgetPart Part(WidgetPartId partId)
    {
        RuleChecker.Check(new WidgetPartMustExist(State.Parts, partId));

        return new WidgetPart(this, partId);
    }

    internal WidgetPartState? FindPart(WidgetPartId partId) =>
        State.Parts.FirstOrDefault(part => part.Id == partId);

    private WidgetPartState PartStateOrThrow(WidgetPartId partId) =>
        State.Parts.First(part => part.Id == partId);
}
