namespace VitalSync.DesignTokens.Contrast.Checks;

internal sealed record SeparationRule(
    string Id,
    string Description,
    IReadOnlyList<string> Colors,
    double MinimumDistance,
    IReadOnlyList<ColorVision> Vision,
    IReadOnlyList<ThemeScope> Themes);

internal sealed record SeparationWaiver(string SeparationId, ThemeScope Theme, double Distance, string Reason);
