using Microsoft.AspNetCore.Components;

namespace VitalSync.DesignSystem.Patterns.Data;

public sealed class DataTableTemplateColumn<TItem> : DataTableColumnBase<TItem>
{
    [Parameter, EditorRequired]
    public RenderFragment<TItem> ChildContent { get; set; } = default!;

    [Parameter]
    public bool Numeric { get; set; }

    internal override bool IsNumeric => Numeric;

    internal override RenderFragment<TItem> Cell => ChildContent;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (ChildContent is null)
        {
            throw new InvalidOperationException(
                "A data table template column requires ChildContent; it renders the cell of a row.");
        }
    }
}
