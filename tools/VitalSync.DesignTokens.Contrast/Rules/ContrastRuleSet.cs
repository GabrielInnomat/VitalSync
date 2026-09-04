using System.Globalization;
using System.Text.Json;

namespace VitalSync.DesignTokens.Contrast.Rules;

internal sealed partial record ContrastRuleSet(
    IReadOnlyList<ContrastRule> Rules,
    IReadOnlyList<ContrastWaiver> Waivers,
    IReadOnlyList<SeparationRule> Separations,
    IReadOnlyList<SeparationWaiver> SeparationWaivers)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static RuleSetLoadResult Load(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        RuleSetDocument? document;

        try
        {
            document = JsonSerializer.Deserialize<RuleSetDocument>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            return new RuleSetLoadResult(RuleSet: null, [FormattableString.Invariant($"invalid JSON: {exception.Message}")]);
        }

        if (document is null)
        {
            return new RuleSetLoadResult(RuleSet: null, ["the rule document is empty"]);
        }

        var problems = new List<string>();
        var rules = new List<ContrastRule>();
        var identifiers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in document.Checks)
        {
            var rule = ConvertRule(entry, identifiers, problems);

            if (rule is not null)
            {
                rules.Add(rule);
            }
        }

        var separations = new List<SeparationRule>();
        var separationIdentifiers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in document.Separations)
        {
            var separation = ConvertSeparation(entry, identifiers, separationIdentifiers, problems);

            if (separation is not null)
            {
                separations.Add(separation);
            }
        }

        var waivers = new List<ContrastWaiver>();

        foreach (var entry in document.Waivers)
        {
            var waiver = ConvertWaiver(entry, identifiers, problems);

            if (waiver is not null)
            {
                waivers.Add(waiver);
            }
        }

        var separationWaivers = new List<SeparationWaiver>();

        foreach (var entry in document.SeparationWaivers)
        {
            var waiver = ConvertSeparationWaiver(entry, separationIdentifiers, problems);

            if (waiver is not null)
            {
                separationWaivers.Add(waiver);
            }
        }

        if (rules.Count == 0)
        {
            problems.Add("the rule document declares no checks");
        }

        return problems.Count > 0
            ? new RuleSetLoadResult(RuleSet: null, problems)
            : new RuleSetLoadResult(new ContrastRuleSet(rules, waivers, separations, separationWaivers), problems);
    }

    public ContrastWaiver? FindWaiver(string checkId, ThemeScope theme) =>
        Waivers.FirstOrDefault(waiver =>
            string.Equals(waiver.CheckId, checkId, StringComparison.Ordinal) && (waiver.Theme == theme));

    public SeparationWaiver? FindSeparationWaiver(string separationId, ThemeScope theme) =>
        SeparationWaivers.FirstOrDefault(waiver =>
            string.Equals(waiver.SeparationId, separationId, StringComparison.Ordinal) && (waiver.Theme == theme));

    internal static double DefaultMinimumRatio(ContrastRequirement requirement) =>
        requirement switch
        {
            ContrastRequirement.TextNormal => 4.5,
            ContrastRequirement.TextLarge => 3.0,
            ContrastRequirement.NonText => 3.0,
            _ => 4.5,
        };

    internal static string Format(double ratio) =>
        ratio.ToString("0.00", CultureInfo.InvariantCulture);
}
