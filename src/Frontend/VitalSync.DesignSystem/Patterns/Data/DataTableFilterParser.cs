using System.Globalization;
using System.Text.RegularExpressions;

namespace VitalSync.DesignSystem.Patterns.Data;

internal static partial class DataTableFilterParser
{
    private const string Number = @"-?(?:[0-9]{1,3}(?:[’'][0-9]{3})+|[0-9]+)(?:\.[0-9]+)?";
    private const string Date = @"[0-9]{1,2}\.[0-9]{1,2}\.[0-9]{4}";
    private const string Separator = @"\s*[-–]\s*";

    internal static DataTableRange<decimal>? ParseNumber(string text) =>
        Parse(text, NumberRangePattern(), NumberValue);

    internal static DataTableRange<DateOnly>? ParseDate(string text) =>
        Parse(text, DateRangePattern(), DateValue);

    private static DataTableRange<T>? Parse<T>(string text, Regex range, Func<string, T?> value)
        where T : struct, IComparable<T>
    {
        var trimmed = text.Trim();
        var comparison = OperatorPattern().Match(trimmed);

        if (comparison.Success)
        {
            return value(comparison.Groups["value"].Value) is { } bound
                ? Compare(comparison.Groups["operator"].Value, bound)
                : null;
        }

        var between = range.Match(trimmed);

        return between.Success
            ? Between(value(between.Groups["from"].Value), value(between.Groups["to"].Value))
            : value(trimmed) is { } single ? Compare("=", single) : null;
    }

    private static DataTableRange<T>? Between<T>(T? from, T? to)
        where T : struct, IComparable<T> =>
        from is not { } lower || to is not { } upper
            ? null
            : lower.CompareTo(upper) <= 0
                ? new(new(lower, true), new(upper, true))
                : new(new(upper, true), new(lower, true));

    private static DataTableRange<T> Compare<T>(string comparison, T bound)
        where T : struct, IComparable<T> => comparison switch
        {
            "<" => new(null, new(bound, false)),
            "<=" or "≤" => new(null, new(bound, true)),
            ">" => new(new(bound, false), null),
            ">=" or "≥" => new(new(bound, true), null),
            _ => new(new(bound, true), new(bound, true)),
        };

    private static decimal? NumberValue(string text)
    {
        if (!NumberPattern().IsMatch(text))
        {
            return null;
        }

        var plain = text.Replace("’", "", StringComparison.Ordinal).Replace("'", "", StringComparison.Ordinal);

        return decimal.TryParse(
            plain,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var number)
            ? number
            : null;
    }

    private static DateOnly? DateValue(string text) =>
        DatePattern().IsMatch(text)
        && DateOnly.TryParseExact(text, "d.M.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    [GeneratedRegex(@"^(?<operator><=|>=|≤|≥|<|>|=)\s*(?<value>\S.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex OperatorPattern();

    [GeneratedRegex("^" + Number + "$", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();

    [GeneratedRegex("^(?<from>" + Number + ")" + Separator + "(?<to>" + Number + ")$", RegexOptions.CultureInvariant)]
    private static partial Regex NumberRangePattern();

    [GeneratedRegex("^" + Date + "$", RegexOptions.CultureInvariant)]
    private static partial Regex DatePattern();

    [GeneratedRegex("^(?<from>" + Date + ")" + Separator + "(?<to>" + Date + ")$", RegexOptions.CultureInvariant)]
    private static partial Regex DateRangePattern();
}
