namespace VitalSync.DesignSystem.Patterns.Data;

public sealed record DataTableResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);
