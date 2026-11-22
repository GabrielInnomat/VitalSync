using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace VitalSync.DesignSystem.Primitives;

public sealed class TableColumn<TItem> : ComponentBase
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    [Parameter, EditorRequired]
    public RenderFragment<TItem> ChildContent { get; set; } = default!;

    [Parameter]
    public bool Numeric { get; set; }

    [Parameter]
    public bool? RowHeader { get; set; }

    [CascadingParameter]
    private Table<TItem>? Parent { get; set; }

    protected override void OnParametersSet()
    {
        if (Parent is null)
        {
            throw new InvalidOperationException("TableColumn must be placed inside Table.");
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException("A table column requires a visible Title; it names the column.");
        }

        if (ChildContent is null)
        {
            throw new InvalidOperationException("A table column requires ChildContent; it renders the cell of a row.");
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => Parent?.AddColumn(this);
}
