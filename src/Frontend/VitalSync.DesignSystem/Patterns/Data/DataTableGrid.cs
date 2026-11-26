using Microsoft.AspNetCore.Components;

namespace VitalSync.DesignSystem.Patterns.Data;

internal sealed class DataTableGrid<TItem> : Table<TItem>
{
    [Parameter, EditorRequired]
    public DataTable<TItem> Owner { get; set; } = default!;

    private protected override bool CaptionFocusable => true;

    private protected override RenderFragment? EmptyContent => Owner.EmptyContent;

    private protected override RenderFragment HeaderContent(int index, TableColumn<TItem> column) =>
        Owner.HeaderContent(index);

    private protected override string? HeaderSort(int index) => Owner.HeaderSort(index);
}
