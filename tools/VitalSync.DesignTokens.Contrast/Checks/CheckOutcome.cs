namespace VitalSync.DesignTokens.Contrast.Checks;

internal static class CheckOutcome
{
    public const double RegressionTolerance = 0.005;

    public static ContrastCheckStatus Determine(double measured, double minimum, double? waivedAt)
    {
        var passes = measured >= (minimum - RegressionTolerance);

        return (passes, waivedAt) switch
        {
            (true, null) => ContrastCheckStatus.Passed,
            (false, null) => ContrastCheckStatus.Failed,
            (true, _) => ContrastCheckStatus.WaiverObsolete,
            (false, { } recorded) when measured < (recorded - RegressionTolerance) =>
                ContrastCheckStatus.WaiverRegressed,
            _ => ContrastCheckStatus.Waived,
        };
    }

    public static bool IsFatal(ContrastCheckStatus status, bool strict) =>
        status switch
        {
            ContrastCheckStatus.Passed => false,
            ContrastCheckStatus.Waived => strict,
            _ => true,
        };
}
