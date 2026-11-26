using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace VitalSync.DesignSystem.Patterns.Data;

public sealed class DataTableColumn<TItem, TValue> : DataTableColumnBase<TItem>
{
    private static readonly Type ValueType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
    private static readonly DataTableFilterKind Kind = KindOf(ValueType);

    private readonly RenderFragment<TItem> _cell;

    public DataTableColumn()
    {
        _cell = item => ChildContent is { } template
            ? template(item)
            : builder => builder.AddContent(0, Formatted(Value(item)));
    }

    [Parameter, EditorRequired]
    public Func<TItem, TValue> Value { get; set; } = default!;

    [Parameter]
    public string? Key { get; set; }

    [Parameter]
    public bool Sortable { get; set; }

    [Parameter]
    public bool Filterable { get; set; }

    [Parameter]
    public string? Format { get; set; }

    [Parameter]
    public bool? Numeric { get; set; }

    [Parameter]
    public RenderFragment<TItem>? ChildContent { get; set; }

    internal override bool IsNumeric => Numeric ?? (Kind == DataTableFilterKind.Number);

    internal override RenderFragment<TItem> Cell => _cell;

    internal override string? QueryKey => Key;

    internal override bool IsSortable => Sortable;

    internal override DataTableFilterKind FilterKind => Filterable ? Kind : DataTableFilterKind.None;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Value is null)
        {
            throw new InvalidOperationException("A data table column requires a Value; it is what the column shows, sorts and filters.");
        }

        if ((Sortable || Filterable) && string.IsNullOrWhiteSpace(Key))
        {
            throw new InvalidOperationException(
                "A sortable or filterable data table column requires a Key; it names the column in the address and in the request.");
        }

        if (Filterable && Kind == DataTableFilterKind.None)
        {
            throw new InvalidOperationException(
                $"A data table column of type {ValueType.Name} cannot be filtered; only text, numbers and dates can.");
        }

        if (Sortable && !typeof(IComparable).IsAssignableFrom(ValueType))
        {
            throw new InvalidOperationException(
                $"A data table column of type {ValueType.Name} cannot be sorted; its values are not comparable.");
        }
    }

    internal override IEnumerable<TItem> Sort(IEnumerable<TItem> items, bool descending)
    {
        var comparer = typeof(TValue) == typeof(string)
            ? (IComparer<TValue>)StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true)
            : Comparer<TValue>.Default;

        return descending ? items.OrderByDescending(Value, comparer) : items.OrderBy(Value, comparer);
    }

    internal override bool Matches(TItem item, DataTableFilter filter)
    {
        object? value = Value(item);

        return filter switch
        {
            DataTableTextFilter text => value is string content
                && CultureInfo.CurrentCulture.CompareInfo.IndexOf(content, text.Text, CompareOptions.IgnoreCase) >= 0,
            DataTableNumberFilter number => AsNumber(value) is { } amount && number.Range.Contains(amount),
            DataTableDateFilter date => AsDate(value) is { } day && date.Range.Contains(day),
            _ => true,
        };
    }

    private static DataTableFilterKind KindOf(Type type) =>
        type == typeof(string) ? DataTableFilterKind.Text
        : type == typeof(DateOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset) ? DataTableFilterKind.Date
        : type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
            || type == typeof(sbyte) || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort)
            || type == typeof(decimal) || type == typeof(double) || type == typeof(float) ? DataTableFilterKind.Number
        : DataTableFilterKind.None;

    private static decimal? AsNumber(object? value) => value switch
    {
        decimal amount => amount,
        double amount when double.IsFinite(amount) && Math.Abs(amount) < 7.9e28 => (decimal)amount,
        float amount when float.IsFinite(amount) && Math.Abs(amount) < 7.9e28f => (decimal)amount,
        double or float => null,
        IConvertible convertible => convertible.ToDecimal(CultureInfo.InvariantCulture),
        _ => null,
    };

    private static DateOnly? AsDate(object? value) => value switch
    {
        DateOnly date => date,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        DateTimeOffset dateTime => DateOnly.FromDateTime(dateTime.DateTime),
        _ => null,
    };

    private string Formatted(TValue value) => value switch
    {
        null => "",
        DateOnly date => date.ToString(Format ?? "dd.MM.yyyy", CultureInfo.CurrentCulture),
        DateTime dateTime => dateTime.ToString(Format ?? "dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture),
        DateTimeOffset dateTime => dateTime.ToString(Format ?? "dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture),
        IFormattable formattable => formattable.ToString(
            Format ?? (Kind == DataTableFilterKind.Number ? "#,0.##" : null),
            CultureInfo.CurrentCulture),
        _ => value.ToString() ?? "",
    };
}
