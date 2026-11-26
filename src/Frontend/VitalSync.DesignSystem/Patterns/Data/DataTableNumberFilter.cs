namespace VitalSync.DesignSystem.Patterns.Data;

public sealed record DataTableNumberFilter(string Key, DataTableRange<decimal> Range) : DataTableFilter(Key);
