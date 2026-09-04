namespace VitalSync.DesignTokens.Contrast.Checks;

internal static class SeparationCheckRunner
{
    public static IReadOnlyList<SeparationCheckResult> Run(CssCustomProperties tokens, ContrastRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(ruleSet);

        var results = new List<SeparationCheckResult>();

        foreach (var rule in ruleSet.Separations)
        {
            foreach (var theme in rule.Themes)
            {
                results.Add(Evaluate(tokens, ruleSet, rule, theme));
            }
        }

        return results;
    }

    private static SeparationCheckResult Evaluate(
        CssCustomProperties tokens,
        ContrastRuleSet ruleSet,
        SeparationRule rule,
        ThemeScope theme)
    {
        var waiver = ruleSet.FindSeparationWaiver(rule.Id, theme);
        var resolutions = rule.Colors.Select(color => tokens.ResolveColor(color, theme)).ToList();
        var unresolved = resolutions.Where(resolution => resolution.Color is null).ToList();

        if (unresolved.Count > 0)
        {
            return new SeparationCheckResult(
                rule,
                theme,
                ContrastCheckStatus.Unresolved,
                Distance: null,
                WorstVision: null,
                WorstPair: null,
                unresolved,
                waiver);
        }

        var closest = double.MaxValue;
        var worstVision = ColorVision.Normal;
        var worstPair = string.Empty;

        foreach (var vision in rule.Vision)
        {
            var simulated = resolutions
                .Select(resolution => CieLab.FromColor(ColorVisionSimulator.Simulate(resolution.Color!.Value, vision)))
                .ToList();

            for (var first = 0; first < simulated.Count; first++)
            {
                for (var second = first + 1; second < simulated.Count; second++)
                {
                    var distance = CieLab.Distance(simulated[first], simulated[second]);

                    if (distance < closest)
                    {
                        closest = distance;
                        worstVision = vision;
                        worstPair = FormattableString.Invariant(
                            $"{rule.Colors[first]} vs {rule.Colors[second]}");
                    }
                }
            }
        }

        var status = CheckOutcome.Determine(closest, rule.MinimumDistance, waiver?.Distance);

        return new SeparationCheckResult(rule, theme, status, closest, worstVision, worstPair, [], waiver);
    }
}
