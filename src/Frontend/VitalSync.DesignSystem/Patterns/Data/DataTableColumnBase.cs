using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace VitalSync.DesignSystem.Patterns.Data;

public abstract class DataTableColumnBase<TItem> : ComponentBase
{
    private protected DataTableColumnBase()
    {
    }

    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    [Parameter]
    public bool? RowHeader { get; set; }

    [CascadingParameter]
    private DataTable<TItem>? Parent { get; set; }

    internal abstract bool IsNumeric { get; }

    internal abstract RenderFragment<TItem> Cell { get; }

    internal virtual string? QueryKey => null;

    internal virtual bool IsSortable => false;

    internal virtual DataTableFilterKind FilterKind => DataTableFilterKind.None;

    internal virtual IEnumerable<TItem> Sort(IEnumerable<TItem> items, bool descending) => items;

    internal virtual bool Matches(TItem item, DataTableFilter filter) => true;

    protected override void OnParametersSet()
    {
        if (Parent is null)
        {
            throw new InvalidOperationException("A data table column must be placed inside DataTable.");
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException("A data table column requires a visible Title; it names the column.");
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => Parent?.AddColumn(this);
}
