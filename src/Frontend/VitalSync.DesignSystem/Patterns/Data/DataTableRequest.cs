namespace VitalSync.DesignSystem.Patterns.Data;

public sealed record DataTableRequest(
    IReadOnlyList<DataTableFilter> Filters,
    DataTableSort? Sort,
    int Page,
    int PageSize)
{
    public int Skip => (Page - 1) * PageSize;
}
