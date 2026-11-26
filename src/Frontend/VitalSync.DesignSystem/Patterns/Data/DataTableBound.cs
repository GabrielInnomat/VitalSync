namespace VitalSync.DesignSystem.Patterns.Data;

public readonly record struct DataTableBound<T>(T Value, bool Inclusive)
    where T : struct, IComparable<T>;
