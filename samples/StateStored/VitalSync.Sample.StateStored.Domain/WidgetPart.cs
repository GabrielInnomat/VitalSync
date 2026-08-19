using GaWeCodes.Thessera.Domain.Entities;
using GaWeCodes.Thessera.Domain.Rules;

namespace VitalSync.Sample.StateStored.Domain;

public sealed class WidgetPart : Entity<WidgetPartId, WidgetPartState>
{
    private readonly WidgetId _widgetId;

    internal WidgetPart(Widget widget, WidgetPartId id)
        : base(widget, id)
    {
        _widgetId = widget.Id;
    }

    public string Label => GetCurrentState().Label;

    public int Quantity => GetCurrentState().Quantity;

    public void ChangeQuantity(int quantity)
    {
        RuleChecker.CheckValidationRule(new WidgetPartQuantityMustBePositive(quantity));

        RaiseEvent(new WidgetPartQuantityChanged(_widgetId, Id, quantity, Quantity));
    }
}
