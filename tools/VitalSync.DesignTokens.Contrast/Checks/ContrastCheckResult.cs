namespace VitalSync.DesignTokens.Contrast.Checks;

internal enum ContrastCheckStatus
{
    Passed,
    Failed,
    Waived,
    WaiverRegressed,
    WaiverObsolete,
    Unresolved,
}

internal sealed record ContrastCheckResult(
    ContrastRule Rule,
    ThemeScope Theme,
    ContrastCheckStatus Status,
    double? Ratio,
    TokenResolution Foreground,
    TokenResolution Background,
    ContrastWaiver? Waiver)
{
    public bool IsFatal(bool strict) => CheckOutcome.IsFatal(Status, strict);
}
