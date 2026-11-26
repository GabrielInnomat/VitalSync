namespace VitalSync.DesignSystem.Patterns.Data;

public sealed record DataTableRange<T>(DataTableBound<T>? From, DataTableBound<T>? To)
    where T : struct, IComparable<T>
{
    public bool Contains(T value) => Above(value) && Below(value);

    private bool Above(T value) => From is not { } from
        || (from.Inclusive ? value.CompareTo(from.Value) >= 0 : value.CompareTo(from.Value) > 0);

    private bool Below(T value) => To is not { } to
        || (to.Inclusive ? value.CompareTo(to.Value) <= 0 : value.CompareTo(to.Value) < 0);
}
