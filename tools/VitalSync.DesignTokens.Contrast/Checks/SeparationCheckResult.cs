namespace VitalSync.DesignTokens.Contrast.Checks;

internal sealed record SeparationCheckResult(
    SeparationRule Rule,
    ThemeScope Theme,
    ContrastCheckStatus Status,
    double? Distance,
    ColorVision? WorstVision,
    string? WorstPair,
    IReadOnlyList<TokenResolution> Unresolved,
    SeparationWaiver? Waiver)
{
    public bool IsFatal(bool strict) => CheckOutcome.IsFatal(Status, strict);
}
