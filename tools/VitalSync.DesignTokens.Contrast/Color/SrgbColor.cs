using System.Globalization;

namespace VitalSync.DesignTokens.Contrast.Color;

internal readonly record struct SrgbColor(byte Red, byte Green, byte Blue)
{
    public static bool TryParse(string? value, out SrgbColor color)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim();

        return text.StartsWith('#')
            ? TryParseHex(text.AsSpan(1), out color)
            : TryParseRgbFunction(text, out color);
    }

    public static double ContrastRatio(SrgbColor first, SrgbColor second)
    {
        var lighter = Math.Max(RelativeLuminance(first), RelativeLuminance(second));
        var darker = Math.Min(RelativeLuminance(first), RelativeLuminance(second));

        return (lighter + 0.05) / (darker + 0.05);
    }

    public string ToHex() =>
        string.Create(CultureInfo.InvariantCulture, $"#{Red:X2}{Green:X2}{Blue:X2}");

    internal static double Linearize(byte channel)
    {
        var value = channel / 255.0;

        return value <= 0.03928
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    internal static SrgbColor FromLinear(double red, double green, double blue) =>
        new(Encode(red), Encode(green), Encode(blue));

    private static byte Encode(double linear)
    {
        var clamped = Math.Clamp(linear, 0.0, 1.0);

        var encoded = clamped <= 0.0031308
            ? 12.92 * clamped
            : (1.055 * Math.Pow(clamped, 1.0 / 2.4)) - 0.055;

        return (byte)Math.Round(encoded * 255.0, MidpointRounding.AwayFromZero);
    }

    private static double RelativeLuminance(SrgbColor color)
    {
        var red = Linearize(color.Red);
        var green = Linearize(color.Green);
        var blue = Linearize(color.Blue);

        return (0.2126 * red) + (0.7152 * green) + (0.0722 * blue);
    }

    private static bool TryParseHex(ReadOnlySpan<char> digits, out SrgbColor color)
    {
        color = default;

        switch (digits.Length)
        {
            case 3:
            case 4:
                if ((digits.Length == 4) && !IsOpaqueShorthandAlpha(digits[3]))
                {
                    return false;
                }

                return TryComposeHex(
                    digits[..1],
                    digits.Slice(1, 1),
                    digits.Slice(2, 1),
                    expand: true,
                    out color);

            case 6:
            case 8:
                if ((digits.Length == 8) && !IsOpaqueAlpha(digits.Slice(6, 2)))
                {
                    return false;
                }

                return TryComposeHex(
                    digits[..2],
                    digits.Slice(2, 2),
                    digits.Slice(4, 2),
                    expand: false,
                    out color);

            default:
                return false;
        }
    }

    private static bool IsOpaqueShorthandAlpha(char alpha) =>
        alpha is 'f' or 'F';

    private static bool IsOpaqueAlpha(ReadOnlySpan<char> alpha) =>
        alpha.Equals("ff", StringComparison.OrdinalIgnoreCase);

    private static bool TryComposeHex(
        ReadOnlySpan<char> red,
        ReadOnlySpan<char> green,
        ReadOnlySpan<char> blue,
        bool expand,
        out SrgbColor color)
    {
        color = default;

        if (!TryParseHexChannel(red, expand, out var redValue)
            || !TryParseHexChannel(green, expand, out var greenValue)
            || !TryParseHexChannel(blue, expand, out var blueValue))
        {
            return false;
        }

        color = new SrgbColor(redValue, greenValue, blueValue);

        return true;
    }

    private static bool TryParseHexChannel(ReadOnlySpan<char> digits, bool expand, out byte channel)
    {
        channel = 0;

        if (!int.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        channel = expand ? (byte)((value * 16) + value) : (byte)value;

        return true;
    }

    private static bool TryParseRgbFunction(string text, out SrgbColor color)
    {
        color = default;

        var open = text.IndexOf('(', StringComparison.Ordinal);

        if ((open < 0) || !text.EndsWith(')'))
        {
            return false;
        }

        var name = text[..open].Trim();

        if (!name.Equals("rgb", StringComparison.OrdinalIgnoreCase)
            && !name.Equals("rgba", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var arguments = text[(open + 1)..^1]
            .Replace(',', ' ')
            .Replace('/', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return (arguments.Length is 3 or 4)
            && ((arguments.Length != 4) || IsOpaqueAlphaArgument(arguments[3]))
            && TryComposeChannels(arguments[0], arguments[1], arguments[2], out color);
    }

    private static bool IsOpaqueAlphaArgument(string alpha) =>
        double.TryParse(alpha.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
        && (Math.Abs(value - (alpha.EndsWith('%') ? 100.0 : 1.0)) < 0.0001);

    private static bool TryComposeChannels(string red, string green, string blue, out SrgbColor color)
    {
        color = default;

        if (!TryParseChannel(red, out var redValue)
            || !TryParseChannel(green, out var greenValue)
            || !TryParseChannel(blue, out var blueValue))
        {
            return false;
        }

        color = new SrgbColor(redValue, greenValue, blueValue);

        return true;
    }

    private static bool TryParseChannel(string text, out byte channel)
    {
        channel = 0;

        var isPercentage = text.EndsWith('%');
        var literal = isPercentage ? text[..^1] : text;

        if (!double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        var scaled = isPercentage ? value / 100.0 * 255.0 : value;

        if (scaled is < 0.0 or > 255.0)
        {
            return false;
        }

        channel = (byte)Math.Round(scaled, MidpointRounding.AwayFromZero);

        return true;
    }
}
