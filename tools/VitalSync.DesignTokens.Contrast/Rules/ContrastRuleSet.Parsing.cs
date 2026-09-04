using System.Text.Json.Serialization;

namespace VitalSync.DesignTokens.Contrast.Rules;

internal sealed partial record ContrastRuleSet
{
    private static ContrastRule? ConvertRule(CheckDocument entry, HashSet<string> identifiers, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
        {
            problems.Add("a check is missing its 'id'");

            return null;
        }

        if (!identifiers.Add(entry.Id))
        {
            problems.Add(FormattableString.Invariant($"check '{entry.Id}' is declared more than once"));

            return null;
        }

        if (string.IsNullOrWhiteSpace(entry.Foreground) || string.IsNullOrWhiteSpace(entry.Background))
        {
            problems.Add(FormattableString.Invariant($"check '{entry.Id}' needs both 'foreground' and 'background'"));

            return null;
        }

        if (!TryParseRequirement(entry.Requirement, out var requirement))
        {
            problems.Add(FormattableString.Invariant(
                $"check '{entry.Id}' has unknown requirement '{entry.Requirement}'; expected text-normal, text-large or non-text"));

            return null;
        }

        var themes = new List<ThemeScope>();

        foreach (var theme in entry.Themes.Count > 0 ? entry.Themes : ["light", "dark"])
        {
            if (TryParseTheme(theme, out var scope))
            {
                themes.Add(scope);
            }
            else
            {
                problems.Add(FormattableString.Invariant($"check '{entry.Id}' names unknown theme '{theme}'"));
            }
        }

        if (themes.Count == 0)
        {
            return null;
        }

        var minimum = entry.MinimumRatio ?? DefaultMinimumRatio(requirement);

        if (minimum is <= 1.0 or > 21.0)
        {
            problems.Add(FormattableString.Invariant($"check '{entry.Id}' has an out-of-range 'minimumRatio' of {minimum}"));

            return null;
        }

        return new ContrastRule(
            entry.Id,
            string.IsNullOrWhiteSpace(entry.Criterion) ? "-" : entry.Criterion,
            string.IsNullOrWhiteSpace(entry.Description) ? entry.Id : entry.Description,
            entry.Foreground,
            entry.Background,
            requirement,
            minimum,
            themes);
    }

    private static SeparationRule? ConvertSeparation(
        SeparationDocument entry,
        HashSet<string> checkIdentifiers,
        HashSet<string> separationIdentifiers,
        List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
        {
            problems.Add("a separation is missing its 'id'");

            return null;
        }

        if (checkIdentifiers.Contains(entry.Id) || !separationIdentifiers.Add(entry.Id))
        {
            problems.Add(FormattableString.Invariant($"identifier '{entry.Id}' is declared more than once"));

            return null;
        }

        if (entry.Colors.Count < 2)
        {
            problems.Add(FormattableString.Invariant($"separation '{entry.Id}' needs at least two entries in 'colors'"));

            return null;
        }

        var vision = new List<ColorVision>();

        foreach (var name in entry.Vision.Count > 0 ? entry.Vision : ["normal", "protanopia", "deuteranopia", "tritanopia"])
        {
            if (Enum.TryParse<ColorVision>(name?.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            {
                vision.Add(parsed);
            }
            else
            {
                problems.Add(FormattableString.Invariant($"separation '{entry.Id}' names unknown vision '{name}'"));
            }
        }

        var themes = new List<ThemeScope>();

        foreach (var theme in entry.Themes.Count > 0 ? entry.Themes : ["light", "dark"])
        {
            if (TryParseTheme(theme, out var scope))
            {
                themes.Add(scope);
            }
            else
            {
                problems.Add(FormattableString.Invariant($"separation '{entry.Id}' names unknown theme '{theme}'"));
            }
        }

        if ((vision.Count == 0) || (themes.Count == 0))
        {
            return null;
        }

        if (entry.MinimumDeltaE is not > 0.0 or > 200.0)
        {
            problems.Add(FormattableString.Invariant(
                $"separation '{entry.Id}' needs a 'minimumDeltaE' greater than 0 and at most 200"));

            return null;
        }

        return new SeparationRule(
            entry.Id,
            string.IsNullOrWhiteSpace(entry.Description) ? entry.Id : entry.Description,
            entry.Colors,
            entry.MinimumDeltaE.Value,
            vision,
            themes);
    }

    private static SeparationWaiver? ConvertSeparationWaiver(
        SeparationWaiverDocument entry,
        HashSet<string> identifiers,
        List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(entry.Separation) || !identifiers.Contains(entry.Separation))
        {
            problems.Add(FormattableString.Invariant($"waiver references unknown separation '{entry.Separation}'"));

            return null;
        }

        if (!TryParseTheme(entry.Theme, out var theme))
        {
            problems.Add(FormattableString.Invariant($"waiver for '{entry.Separation}' names unknown theme '{entry.Theme}'"));

            return null;
        }

        if (string.IsNullOrWhiteSpace(entry.Reason))
        {
            problems.Add(FormattableString.Invariant($"waiver for '{entry.Separation}' is missing its 'reason'"));

            return null;
        }

        return new SeparationWaiver(entry.Separation, theme, entry.DeltaE, entry.Reason);
    }

    private static ContrastWaiver? ConvertWaiver(WaiverDocument entry, HashSet<string> identifiers, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(entry.Check) || !identifiers.Contains(entry.Check))
        {
            problems.Add(FormattableString.Invariant($"waiver references unknown check '{entry.Check}'"));

            return null;
        }

        if (!TryParseTheme(entry.Theme, out var theme))
        {
            problems.Add(FormattableString.Invariant($"waiver for '{entry.Check}' names unknown theme '{entry.Theme}'"));

            return null;
        }

        if (string.IsNullOrWhiteSpace(entry.Reason))
        {
            problems.Add(FormattableString.Invariant($"waiver for '{entry.Check}' is missing its 'reason'"));

            return null;
        }

        return new ContrastWaiver(entry.Check, theme, entry.Ratio, entry.Reason);
    }

    private static bool TryParseRequirement(string? value, out ContrastRequirement requirement)
    {
        switch (value?.Trim().ToUpperInvariant())
        {
            case "TEXT-NORMAL":
                requirement = ContrastRequirement.TextNormal;
                return true;

            case "TEXT-LARGE":
                requirement = ContrastRequirement.TextLarge;
                return true;

            case "NON-TEXT":
                requirement = ContrastRequirement.NonText;
                return true;

            default:
                requirement = ContrastRequirement.TextNormal;
                return false;
        }
    }

    private static bool TryParseTheme(string? value, out ThemeScope theme) =>
        Enum.TryParse(value?.Trim(), ignoreCase: true, out theme) && Enum.IsDefined(theme);

    private sealed record RuleSetDocument
    {
        [JsonPropertyName("checks")]
        public IReadOnlyList<CheckDocument> Checks { get; init; } = [];

        [JsonPropertyName("waivers")]
        public IReadOnlyList<WaiverDocument> Waivers { get; init; } = [];

        [JsonPropertyName("separations")]
        public IReadOnlyList<SeparationDocument> Separations { get; init; } = [];

        [JsonPropertyName("separationWaivers")]
        public IReadOnlyList<SeparationWaiverDocument> SeparationWaivers { get; init; } = [];
    }

    private sealed record SeparationDocument
    {
        public string? Id { get; init; }

        public string? Description { get; init; }

        public IReadOnlyList<string> Colors { get; init; } = [];

        public double? MinimumDeltaE { get; init; }

        public IReadOnlyList<string> Vision { get; init; } = [];

        public IReadOnlyList<string> Themes { get; init; } = [];
    }

    private sealed record SeparationWaiverDocument
    {
        public string? Separation { get; init; }

        public string? Theme { get; init; }

        public double DeltaE { get; init; }

        public string? Reason { get; init; }
    }

    private sealed record CheckDocument
    {
        public string? Id { get; init; }

        public string? Criterion { get; init; }

        public string? Description { get; init; }

        public string? Foreground { get; init; }

        public string? Background { get; init; }

        public string? Requirement { get; init; }

        public double? MinimumRatio { get; init; }

        public IReadOnlyList<string> Themes { get; init; } = [];
    }

    private sealed record WaiverDocument
    {
        public string? Check { get; init; }

        public string? Theme { get; init; }

        public double Ratio { get; init; }

        public string? Reason { get; init; }
    }
}
