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
}
