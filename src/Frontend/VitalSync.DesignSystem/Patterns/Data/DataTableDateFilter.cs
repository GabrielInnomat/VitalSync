namespace VitalSync.DesignSystem.Patterns.Data;

public sealed record DataTableDateFilter(string Key, DataTableRange<DateOnly> Range) : DataTableFilter(Key);
