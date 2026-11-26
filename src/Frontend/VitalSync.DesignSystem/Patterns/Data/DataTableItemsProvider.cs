namespace VitalSync.DesignSystem.Patterns.Data;

public delegate ValueTask<DataTableResult<TItem>> DataTableItemsProvider<TItem>(
    DataTableRequest request,
    CancellationToken cancellationToken);
