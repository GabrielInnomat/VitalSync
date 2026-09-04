using System.Collections.Frozen;
using System.Text;

namespace VitalSync.DesignTokens.Contrast.Tokens;

internal sealed class CssCustomProperties
{
    private const int MaxResolutionDepth = 32;

    private readonly FrozenDictionary<string, string> _light;
    private readonly FrozenDictionary<string, string> _dark;

    private CssCustomProperties(FrozenDictionary<string, string> light, FrozenDictionary<string, string> dark)
    {
        _light = light;
        _dark = dark;
    }

    public static CssCustomProperties Parse(string css)
    {
        ArgumentNullException.ThrowIfNull(css);

        var light = new Dictionary<string, string>(StringComparer.Ordinal);
        var dark = new Dictionary<string, string>(StringComparer.Ordinal);
        var stripped = RemoveComments(css);
        var index = 0;

        while (index < stripped.Length)
        {
            var open = stripped.IndexOf('{', index);

            if (open < 0)
            {
                break;
            }

            var prelude = stripped[index..open].Trim();
            var close = FindMatchingBrace(stripped, open);

            if (close < 0)
            {
                break;
            }

            if (!prelude.StartsWith('@'))
            {
                ApplyRule(prelude, stripped[(open + 1)..close], light, dark);
            }

            index = close + 1;
        }

        var merged = new Dictionary<string, string>(light, StringComparer.Ordinal);

        foreach (var declaration in dark)
        {
            merged[declaration.Key] = declaration.Value;
        }

        return new CssCustomProperties(
            light.ToFrozenDictionary(StringComparer.Ordinal),
            merged.ToFrozenDictionary(StringComparer.Ordinal));
    }

    public IReadOnlyDictionary<string, string> DeclarationsFor(ThemeScope theme) =>
        theme == ThemeScope.Dark ? _dark : _light;

    public TokenResolution ResolveColor(string reference, ThemeScope theme)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var trimmed = reference.Trim();

        if (!trimmed.StartsWith("--", StringComparison.Ordinal))
        {
            return SrgbColor.TryParse(trimmed, out var literal)
                ? TokenResolution.Resolved(trimmed, trimmed, literal)
                : TokenResolution.Unresolved(trimmed, "value is neither a custom property nor a supported color");
        }

        var declarations = DeclarationsFor(theme);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = trimmed;

        for (var depth = 0; depth < MaxResolutionDepth; depth++)
        {
            if (!visited.Add(current))
            {
                return TokenResolution.Unresolved(trimmed, FormattableString.Invariant($"circular reference at {current}"));
            }

            if (!declarations.TryGetValue(current, out var raw))
            {
                return TokenResolution.Unresolved(
                    trimmed,
                    FormattableString.Invariant($"{current} is not declared for the {theme} theme"));
            }

            var value = raw.Trim();

            if (TrySplitVarReference(value, out var referenced, out var fallback))
            {
                if (declarations.ContainsKey(referenced))
                {
                    current = referenced;
                    continue;
                }

                if (fallback is null)
                {
                    return TokenResolution.Unresolved(
                        trimmed,
                        FormattableString.Invariant($"{current} references {referenced}, which is not declared for the {theme} theme"));
                }

                value = fallback;
            }

            return SrgbColor.TryParse(value, out var color)
                ? TokenResolution.Resolved(trimmed, value, color)
                : TokenResolution.Unresolved(
                    trimmed,
                    FormattableString.Invariant($"{current} resolves to '{value}', which is not a supported color"));
        }

        return TokenResolution.Unresolved(trimmed, "maximum resolution depth exceeded");
    }

    internal static bool TrySplitVarReference(string value, out string reference, out string? fallback)
    {
        reference = string.Empty;
        fallback = null;

        if (!value.StartsWith("var(", StringComparison.Ordinal) || !value.EndsWith(')'))
        {
            return false;
        }

        var depth = 0;

        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '(')
            {
                depth++;
            }
            else if (value[index] == ')')
            {
                depth--;

                if ((depth == 0) && (index != (value.Length - 1)))
                {
                    return false;
                }
            }
        }

        if (depth != 0)
        {
            return false;
        }

        var inner = value[4..^1];
        var separator = IndexOfTopLevelComma(inner);

        if (separator < 0)
        {
            reference = inner.Trim();
        }
        else
        {
            reference = inner[..separator].Trim();
            fallback = inner[(separator + 1)..].Trim();
        }

        return reference.StartsWith("--", StringComparison.Ordinal);
    }

    private static int IndexOfTopLevelComma(string value)
    {
        var depth = 0;

        for (var index = 0; index < value.Length; index++)
        {
            switch (value[index])
            {
                case '(':
                    depth++;
                    break;

                case ')':
                    depth--;
                    break;

                case ',' when depth == 0:
                    return index;

                default:
                    break;
            }
        }

        return -1;
    }

    private static void ApplyRule(
        string selector,
        string body,
        Dictionary<string, string> light,
        Dictionary<string, string> dark)
    {
        var targets = new List<Dictionary<string, string>>();

        foreach (var part in selector.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = part.Replace(" ", string.Empty, StringComparison.Ordinal);

            if (ContainsThemeAttribute(normalized, "dark"))
            {
                targets.Add(dark);
            }
            else if (normalized.Equals(":root", StringComparison.Ordinal) || ContainsThemeAttribute(normalized, "light"))
            {
                targets.Add(light);
            }
        }

        if (targets.Count == 0)
        {
            return;
        }

        foreach (var declaration in ParseDeclarations(body))
        {
            foreach (var target in targets)
            {
                target[declaration.Key] = declaration.Value;
            }
        }
    }

    private static bool ContainsThemeAttribute(string selector, string theme) =>
        selector.Contains(FormattableString.Invariant($"[data-theme=\"{theme}\"]"), StringComparison.Ordinal)
        || selector.Contains(FormattableString.Invariant($"[data-theme='{theme}']"), StringComparison.Ordinal);

    private static IEnumerable<KeyValuePair<string, string>> ParseDeclarations(string body)
    {
        foreach (var statement in body.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = statement.IndexOf(':', StringComparison.Ordinal);

            if (separator <= 0)
            {
                continue;
            }

            var name = statement[..separator].Trim();

            if (!name.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            yield return new KeyValuePair<string, string>(name, statement[(separator + 1)..].Trim());
        }
    }

    private static string RemoveComments(string css)
    {
        var builder = new StringBuilder(css.Length);
        var index = 0;

        while (index < css.Length)
        {
            var start = css.IndexOf("/*", index, StringComparison.Ordinal);

            if (start < 0)
            {
                builder.Append(css, index, css.Length - index);
                break;
            }

            builder.Append(css, index, start - index);

            var end = css.IndexOf("*/", start + 2, StringComparison.Ordinal);

            if (end < 0)
            {
                break;
            }

            index = end + 2;
        }

        return builder.ToString();
    }

    private static int FindMatchingBrace(string css, int openIndex)
    {
        var depth = 0;

        for (var index = openIndex; index < css.Length; index++)
        {
            if (css[index] == '{')
            {
                depth++;
            }
            else if (css[index] == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return index;
                }
            }
        }

        return -1;
    }
}
