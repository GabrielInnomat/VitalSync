namespace VitalSync.DesignTokens.Contrast.Checks;

internal static class ContrastCheckRunner
{
    public static IReadOnlyList<ContrastCheckResult> Run(CssCustomProperties tokens, ContrastRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(ruleSet);

        var results = new List<ContrastCheckResult>();

        foreach (var rule in ruleSet.Rules)
        {
            foreach (var theme in rule.Themes)
            {
                results.Add(Evaluate(tokens, ruleSet, rule, theme));
            }
        }

        return results;
    }

    private static ContrastCheckResult Evaluate(
        CssCustomProperties tokens,
        ContrastRuleSet ruleSet,
        ContrastRule rule,
        ThemeScope theme)
    {
        var foreground = tokens.ResolveColor(rule.Foreground, theme);
        var background = tokens.ResolveColor(rule.Background, theme);
        var waiver = ruleSet.FindWaiver(rule.Id, theme);

        if ((foreground.Color is not { } foregroundColor) || (background.Color is not { } backgroundColor))
        {
            return new ContrastCheckResult(
                rule,
                theme,
                ContrastCheckStatus.Unresolved,
                Ratio: null,
                foreground,
                background,
                waiver);
        }

        var ratio = SrgbColor.ContrastRatio(foregroundColor, backgroundColor);
        var status = CheckOutcome.Determine(ratio, rule.MinimumRatio, waiver?.Ratio);

        return new ContrastCheckResult(rule, theme, status, ratio, foreground, background, waiver);
    }
}
