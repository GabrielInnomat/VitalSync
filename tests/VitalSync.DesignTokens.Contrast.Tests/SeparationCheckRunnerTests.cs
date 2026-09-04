namespace VitalSync.DesignTokens.Contrast.Tests;

public class SeparationCheckRunnerTests
{
    private const string Tokens =
        """
        :root {
          --series-a: #E69F00;
          --series-b: #0072B2;
          --series-c: #B27B00;
          --series-d: #D55E00;
        }
        [data-theme="dark"] {
          --series-b: #56B4E9;
        }
        """;

    [Fact]
    public void Run_ReportsAWellSeparatedSet()
    {
        var result = Assert.Single(Execute("[\"--series-a\", \"--series-b\"]", 16.0, "[]"));

        Assert.Equal(ContrastCheckStatus.Passed, result.Status);
        Assert.True(result.Distance > 16.0);
        Assert.False(result.IsFatal(strict: false));
    }

    [Fact]
    public void Run_ReportsASetThatCollapsesUnderColorVisionDeficiency()
    {
        var result = Assert.Single(Execute("[\"--series-c\", \"--series-d\"]", 16.0, "[]"));

        Assert.Equal(ContrastCheckStatus.Failed, result.Status);
        Assert.True(result.Distance < 16.0);
        Assert.True(result.IsFatal(strict: false));
    }

    [Fact]
    public void Run_NamesTheClosestPairAndTheVisionItWasFoundUnder()
    {
        var result = Assert.Single(Execute("[\"--series-c\", \"--series-d\"]", 16.0, "[]"));

        Assert.NotNull(result.WorstVision);
        Assert.Contains("--series-c", result.WorstPair, StringComparison.Ordinal);
        Assert.Contains("--series-d", result.WorstPair, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_MeasuresTheWorstCaseAcrossEveryVisionItIsGiven()
    {
        var everyVision = Assert.Single(Execute("[\"--series-a\", \"--series-b\"]", 16.0, "[]"));
        var normalOnly = Assert.Single(Execute("[\"--series-a\", \"--series-b\"]", 16.0, "[]", "[\"normal\"]"));

        Assert.True(everyVision.Distance <= normalOnly.Distance);
    }

    [Fact]
    public void Run_TreatsAWaivedSetAsKnownDebt()
    {
        var measured = Assert.Single(Execute("[\"--series-c\", \"--series-d\"]", 16.0, "[]")).Distance!.Value;
        var waivers = FormattableString.Invariant(
            $"[ {{ \"separation\": \"set\", \"theme\": \"light\", \"deltaE\": {measured:0.00}, \"reason\": \"tracked\" }} ]");

        var result = Assert.Single(Execute("[\"--series-c\", \"--series-d\"]", 16.0, waivers));

        Assert.Equal(ContrastCheckStatus.Waived, result.Status);
        Assert.False(result.IsFatal(strict: false));
        Assert.True(result.IsFatal(strict: true));
    }

    [Fact]
    public void Run_FailsWhenAWaiverIsNoLongerNeeded()
    {
        var waivers = "[ { \"separation\": \"set\", \"theme\": \"light\", \"deltaE\": 1.0, \"reason\": \"tracked\" } ]";

        var result = Assert.Single(Execute("[\"--series-a\", \"--series-b\"]", 16.0, waivers));

        Assert.Equal(ContrastCheckStatus.WaiverObsolete, result.Status);
        Assert.True(result.IsFatal(strict: false));
    }

    [Fact]
    public void Run_FailsWhenAWaivedSetGetsWorse()
    {
        var waivers = "[ { \"separation\": \"set\", \"theme\": \"light\", \"deltaE\": 15.0, \"reason\": \"tracked\" } ]";

        var result = Assert.Single(Execute("[\"--series-c\", \"--series-d\"]", 16.0, waivers));

        Assert.Equal(ContrastCheckStatus.WaiverRegressed, result.Status);
        Assert.True(result.IsFatal(strict: false));
    }

    [Fact]
    public void Run_FailsWhenATokenCannotBeResolved()
    {
        var result = Assert.Single(Execute("[\"--series-a\", \"--missing\"]", 16.0, "[]"));

        Assert.Equal(ContrastCheckStatus.Unresolved, result.Status);
        Assert.Null(result.Distance);
        Assert.Single(result.Unresolved);
    }

    [Fact]
    public void Run_MeasuresEachThemeWithItsOwnResolvedColors()
    {
        var document = FormattableString.Invariant($$"""
        {
          "checks": [ { "id": "anything", "foreground": "--series-a", "background": "--series-b", "requirement": "non-text" } ],
          "separations": [ { "id": "set", "colors": ["--series-a", "--series-b"], "minimumDeltaE": 16.0 } ]
        }
        """);
        var ruleSet = ContrastRuleSet.Load(document);
        Assert.Empty(ruleSet.Problems);

        var results = SeparationCheckRunner.Run(CssCustomProperties.Parse(Tokens), ruleSet.RuleSet!);

        Assert.Equal(2, results.Count);
        Assert.NotEqual(
            results.Single(r => r.Theme == ThemeScope.Light).Distance,
            results.Single(r => r.Theme == ThemeScope.Dark).Distance);
    }

    private static IReadOnlyList<SeparationCheckResult> Execute(
        string colors,
        double minimum,
        string waivers,
        string vision = "[\"normal\", \"protanopia\", \"deuteranopia\", \"tritanopia\"]")
    {
        var document = FormattableString.Invariant($$"""
        {
          "checks": [ { "id": "anything", "foreground": "--series-a", "background": "--series-b", "requirement": "non-text" } ],
          "separations": [
            {
              "id": "set",
              "description": "set",
              "colors": {{colors}},
              "minimumDeltaE": {{minimum}},
              "vision": {{vision}},
              "themes": [ "light" ]
            }
          ],
          "separationWaivers": {{waivers}}
        }
        """);

        var ruleSet = ContrastRuleSet.Load(document);

        Assert.Empty(ruleSet.Problems);

        return SeparationCheckRunner.Run(CssCustomProperties.Parse(Tokens), ruleSet.RuleSet!);
    }
}
