namespace VitalSync.DesignTokens.Contrast.Tests;

public class ContrastCheckRunnerTests
{
    private const string Tokens =
        """
        :root {
          --neutral-50: #F8FAFC;
          --neutral-100: #F1F5F9;
          --neutral-900: #0F172A;
          --red-600: #DC2626;
          --color-background-primary: var(--neutral-50);
          --card-background: var(--neutral-100);
          --color-text-primary: var(--neutral-900);
          --color-text-critical: var(--red-600);
        }
        """;

    [Fact]
    public void Run_ReportsAPassingPair()
    {
        var results = Execute(Rule("--color-text-primary", "--color-background-primary", "text-normal"), waivers: "[]");

        var result = Assert.Single(results);
        Assert.Equal(ContrastCheckStatus.Passed, result.Status);
        Assert.Equal(17.06, result.Ratio!.Value, precision: 2);
        Assert.False(result.IsFatal(strict: false));
    }

    [Fact]
    public void Run_ReportsAFailingPair()
    {
        var results = Execute(Rule("--color-text-critical", "--card-background", "text-normal"), waivers: "[]");

        var result = Assert.Single(results);
        Assert.Equal(ContrastCheckStatus.Failed, result.Status);
        Assert.Equal(4.41, result.Ratio!.Value, precision: 2);
        Assert.True(result.IsFatal(strict: false));
    }

    [Fact]
    public void Run_TreatsAWaivedFailureAsKnownDebt()
    {
        var results = Execute(
            Rule("--color-text-critical", "--card-background", "text-normal"),
            waivers: "[ { \"check\": \"pair\", \"theme\": \"light\", \"ratio\": 4.41, \"reason\": \"tracked\" } ]");

        var result = Assert.Single(results);
        Assert.Equal(ContrastCheckStatus.Waived, result.Status);
        Assert.False(result.IsFatal(strict: false));
        Assert.True(result.IsFatal(strict: true));
    }

    [Fact]
    public void Run_FailsWhenAWaivedPairGetsWorse()
    {
        var results = Execute(
            Rule("--color-text-critical", "--card-background", "text-normal"),
            waivers: "[ { \"check\": \"pair\", \"theme\": \"light\", \"ratio\": 4.45, \"reason\": \"tracked\" } ]");

        var result = Assert.Single(results);
        Assert.Equal(ContrastCheckStatus.WaiverRegressed, result.Status);
        Assert.True(result.IsFatal(strict: false));
    }

    [Fact]
    public void Run_FailsWhenAWaiverIsNoLongerNeeded()
    {
        var results = Execute(
            Rule("--color-text-primary", "--color-background-primary", "text-normal"),
            waivers: "[ { \"check\": \"pair\", \"theme\": \"light\", \"ratio\": 1.0, \"reason\": \"tracked\" } ]");

        var result = Assert.Single(results);
        Assert.Equal(ContrastCheckStatus.WaiverObsolete, result.Status);
        Assert.True(result.IsFatal(strict: false));
    }

    [Fact]
    public void Run_FailsWhenATokenCannotBeResolved()
    {
        var results = Execute(Rule("--button-primary-text", "--color-background-primary", "text-normal"), waivers: "[]");

        var result = Assert.Single(results);
        Assert.Equal(ContrastCheckStatus.Unresolved, result.Status);
        Assert.Null(result.Ratio);
        Assert.True(result.IsFatal(strict: false));
    }

    [Fact]
    public void Run_EvaluatesEveryThemeARuleNames()
    {
        var ruleSet = ContrastRuleSet.Load(
            """
            {
              "checks": [
                {
                  "id": "pair",
                  "criterion": "1.4.3",
                  "description": "pair",
                  "foreground": "--color-text-primary",
                  "background": "--color-background-primary",
                  "requirement": "text-normal"
                }
              ]
            }
            """);

        var results = ContrastCheckRunner.Run(CssCustomProperties.Parse(Tokens), ruleSet.RuleSet!);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, result => result.Theme == ThemeScope.Light);
        Assert.Contains(results, result => result.Theme == ThemeScope.Dark);
    }

    private static IReadOnlyList<ContrastCheckResult> Execute(string rule, string waivers)
    {
        var document = FormattableString.Invariant($$"""
        {
          "checks": [ {{rule}} ],
          "waivers": {{waivers}}
        }
        """);

        var ruleSet = ContrastRuleSet.Load(document);

        Assert.Empty(ruleSet.Problems);

        return ContrastCheckRunner.Run(CssCustomProperties.Parse(Tokens), ruleSet.RuleSet!);
    }

    private static string Rule(string foreground, string background, string requirement) =>
        FormattableString.Invariant($$"""
        {
          "id": "pair",
          "criterion": "1.4.3",
          "description": "pair",
          "foreground": "{{foreground}}",
          "background": "{{background}}",
          "requirement": "{{requirement}}",
          "themes": [ "light" ]
        }
        """);
}
