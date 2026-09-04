using System.Text;

namespace VitalSync.DesignTokens.Contrast.Reporting;

internal static class ContrastReport
{
    public static string Render(
        IReadOnlyList<ContrastCheckResult> results,
        IReadOnlyList<SeparationCheckResult> separations,
        bool strict)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(separations);

        var builder = new StringBuilder();

        foreach (var theme in Enum.GetValues<ThemeScope>())
        {
            var themeResults = results.Where(result => result.Theme == theme).ToList();
            var themeSeparations = separations.Where(result => result.Theme == theme).ToList();

            if ((themeResults.Count == 0) && (themeSeparations.Count == 0))
            {
                continue;
            }

            builder.Append(theme.ToString().ToUpperInvariant()).AppendLine();

            foreach (var result in themeResults.OrderBy(result => Rank(result.Status)).ThenBy(result => result.Rule.Id, StringComparer.Ordinal))
            {
                AppendResult(builder, result);
            }

            foreach (var result in themeSeparations.OrderBy(result => Rank(result.Status)).ThenBy(result => result.Rule.Id, StringComparer.Ordinal))
            {
                AppendSeparation(builder, result);
            }

            builder.AppendLine();
        }

        AppendSummary(builder, results, separations, strict);

        return builder.ToString();
    }

    public static string Describe(SeparationCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var distance = result.Distance is { } value ? ContrastRuleSet.Format(value) : "n/a";

        return FormattableString.Invariant(
            $"[{Label(result.Status)}] {result.Theme}/{result.Rule.Id}: closest pair dE {distance} under {result.WorstVision}, required {ContrastRuleSet.Format(result.Rule.MinimumDistance)} ({result.WorstPair})");
    }

    private static void AppendSeparation(StringBuilder builder, SeparationCheckResult result)
    {
        var distance = result.Distance is { } value ? ContrastRuleSet.Format(value) : "  --";

        builder
            .Append("  ")
            .Append(Label(result.Status).PadRight(9))
            .Append(distance.PadLeft(6))
            .Append(" >= ")
            .Append(ContrastRuleSet.Format(result.Rule.MinimumDistance).PadLeft(5))
            .Append("  dE     ")
            .Append(result.Rule.Id)
            .AppendLine();

        if (result.Status == ContrastCheckStatus.Passed)
        {
            return;
        }

        builder.Append("             ").Append(result.Rule.Description).AppendLine();

        if (result.WorstPair is { } pair)
        {
            builder
                .Append("             closest under ")
                .Append(result.WorstVision)
                .Append(": ")
                .Append(pair)
                .AppendLine();
        }

        foreach (var resolution in result.Unresolved)
        {
            builder
                .Append("             ")
                .Append(resolution.Reference)
                .Append(" -> unresolved: ")
                .Append(resolution.Failure)
                .AppendLine();
        }

        if (result.Waiver is { } waiver)
        {
            builder
                .Append("             waiver (recorded dE ")
                .Append(ContrastRuleSet.Format(waiver.Distance))
                .Append("): ")
                .Append(waiver.Reason)
                .AppendLine();
        }
    }

    public static string Describe(ContrastCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var ratio = result.Ratio is { } value ? ContrastRuleSet.Format(value) : "n/a";

        return FormattableString.Invariant(
            $"[{Label(result.Status)}] {result.Theme}/{result.Rule.Id}: {ratio}:1, required {ContrastRuleSet.Format(result.Rule.MinimumRatio)}:1 ({result.Rule.Criterion})");
    }

    private static void AppendResult(StringBuilder builder, ContrastCheckResult result)
    {
        var ratio = result.Ratio is { } value ? ContrastRuleSet.Format(value) : "  --";

        builder
            .Append("  ")
            .Append(Label(result.Status).PadRight(9))
            .Append(ratio.PadLeft(6))
            .Append(" >= ")
            .Append(ContrastRuleSet.Format(result.Rule.MinimumRatio).PadLeft(5))
            .Append("  ")
            .Append(result.Rule.Criterion.PadRight(7))
            .Append(result.Rule.Id)
            .AppendLine();

        if (result.Status == ContrastCheckStatus.Passed)
        {
            return;
        }

        builder.Append("             ").Append(result.Rule.Description).AppendLine();
        AppendOperand(builder, result.Foreground, "foreground");
        AppendOperand(builder, result.Background, "background");

        if (result.Waiver is { } waiver)
        {
            builder
                .Append("             waiver (recorded ")
                .Append(ContrastRuleSet.Format(waiver.Ratio))
                .Append(":1): ")
                .Append(waiver.Reason)
                .AppendLine();
        }
    }

    private static void AppendOperand(StringBuilder builder, TokenResolution resolution, string role)
    {
        builder.Append("             ").Append(role).Append(' ').Append(resolution.Reference);

        if (resolution.Color is { } color)
        {
            builder.Append(" -> ").Append(color.ToHex());
        }
        else
        {
            builder.Append(" -> unresolved: ").Append(resolution.Failure);
        }

        builder.AppendLine();
    }

    private static void AppendSummary(
        StringBuilder builder,
        IReadOnlyList<ContrastCheckResult> results,
        IReadOnlyList<SeparationCheckResult> separations,
        bool strict)
    {
        var fatal = results.Count(result => result.IsFatal(strict))
            + separations.Count(result => result.IsFatal(strict));

        if (separations.Count > 0)
        {
            builder
                .Append("SEPARATION  ")
                .Append(separations.Count)
                .Append(" checks: ")
                .Append(separations.Count(result => result.Status == ContrastCheckStatus.Passed))
                .Append(" passed, ")
                .Append(separations.Count(result => result.Status != ContrastCheckStatus.Passed))
                .Append(" not passed")
                .AppendLine();
        }

        builder
            .Append("SUMMARY  ")
            .Append(results.Count)
            .Append(" checks: ")
            .Append(Count(results, ContrastCheckStatus.Passed))
            .Append(" passed, ")
            .Append(Count(results, ContrastCheckStatus.Waived))
            .Append(" waived, ")
            .Append(Count(results, ContrastCheckStatus.Failed))
            .Append(" failed, ")
            .Append(Count(results, ContrastCheckStatus.WaiverRegressed))
            .Append(" regressed, ")
            .Append(Count(results, ContrastCheckStatus.WaiverObsolete))
            .Append(" obsolete waivers, ")
            .Append(Count(results, ContrastCheckStatus.Unresolved))
            .Append(" unresolved")
            .AppendLine();

        builder
            .Append("         ")
            .Append(fatal)
            .Append(fatal == 1 ? " finding blocks the build" : " findings block the build")
            .AppendLine();
    }

    private static int Count(IReadOnlyList<ContrastCheckResult> results, ContrastCheckStatus status) =>
        results.Count(result => result.Status == status);

    private static string Label(ContrastCheckStatus status) =>
        status switch
        {
            ContrastCheckStatus.Passed => "PASS",
            ContrastCheckStatus.Failed => "FAIL",
            ContrastCheckStatus.Waived => "WAIVED",
            ContrastCheckStatus.WaiverRegressed => "REGRESSED",
            ContrastCheckStatus.WaiverObsolete => "STALE",
            ContrastCheckStatus.Unresolved => "BROKEN",
            _ => "?",
        };

    private static int Rank(ContrastCheckStatus status) =>
        status switch
        {
            ContrastCheckStatus.Unresolved => 0,
            ContrastCheckStatus.WaiverRegressed => 1,
            ContrastCheckStatus.Failed => 2,
            ContrastCheckStatus.WaiverObsolete => 3,
            ContrastCheckStatus.Waived => 4,
            _ => 5,
        };
}
