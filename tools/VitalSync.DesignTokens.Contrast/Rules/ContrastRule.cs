namespace VitalSync.DesignTokens.Contrast.Rules;

internal enum ContrastRequirement
{
    TextNormal,
    TextLarge,
    NonText,
}

internal sealed record ContrastRule(
    string Id,
    string Criterion,
    string Description,
    string Foreground,
    string Background,
    ContrastRequirement Requirement,
    double MinimumRatio,
    IReadOnlyList<ThemeScope> Themes);

internal sealed record ContrastWaiver(string CheckId, ThemeScope Theme, double Ratio, string Reason);

internal sealed record RuleSetLoadResult(ContrastRuleSet? RuleSet, IReadOnlyList<string> Problems);
