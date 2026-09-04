namespace VitalSync.DesignTokens.Contrast.Tests;

public class ContrastRuleSetTests
{
    private const string MinimalDocument =
        """
        {
          "checks": [
            {
              "id": "text-on-background",
              "criterion": "1.4.3",
              "description": "Body text on the page background",
              "foreground": "--color-text-primary",
              "background": "--color-background-primary",
              "requirement": "text-normal"
            }
          ]
        }
        """;

    [Fact]
    public void Load_WithAMinimalDocument_DefaultsToBothThemesAndTheAaThreshold()
    {
        var result = ContrastRuleSet.Load(MinimalDocument);

        Assert.Empty(result.Problems);
        var rule = Assert.Single(result.RuleSet!.Rules);
        Assert.Equal(4.5, rule.MinimumRatio);
        Assert.Equal(2, rule.Themes.Count);
        Assert.Contains(ThemeScope.Light, rule.Themes);
        Assert.Contains(ThemeScope.Dark, rule.Themes);
    }

    [Theory]
    [InlineData("text-normal", 4.5)]
    [InlineData("text-large", 3.0)]
    [InlineData("non-text", 3.0)]
    public void DefaultMinimumRatio_FollowsTheWcagThresholds(string requirement, double expected)
    {
        var document = MinimalDocument.Replace("text-normal", requirement, StringComparison.Ordinal);

        var result = ContrastRuleSet.Load(document);

        Assert.Equal(expected, Assert.Single(result.RuleSet!.Rules).MinimumRatio);
    }

    [Fact]
    public void Load_HonoursAnExplicitMinimumRatio()
    {
        var document = MinimalDocument.Replace(
            "\"requirement\": \"text-normal\"",
            "\"requirement\": \"text-normal\", \"minimumRatio\": 7.0",
            StringComparison.Ordinal);

        Assert.Equal(7.0, Assert.Single(ContrastRuleSet.Load(document).RuleSet!.Rules).MinimumRatio);
    }

    [Fact]
    public void Load_RejectsDuplicateCheckIdentifiers()
    {
        var document = MinimalDocument.Replace(
            "\"checks\": [",
            "\"checks\": [ { \"id\": \"text-on-background\", \"foreground\": \"--a\", \"background\": \"--b\", \"requirement\": \"non-text\" },",
            StringComparison.Ordinal);

        var result = ContrastRuleSet.Load(document);

        Assert.Null(result.RuleSet);
        Assert.Contains(result.Problems, problem => problem.Contains("more than once", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_RejectsAnUnknownRequirement()
    {
        var document = MinimalDocument.Replace("text-normal", "text-huge", StringComparison.Ordinal);

        var result = ContrastRuleSet.Load(document);

        Assert.Null(result.RuleSet);
        Assert.Contains(result.Problems, problem => problem.Contains("unknown requirement", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_RejectsAWaiverForAnUnknownCheck()
    {
        var result = ContrastRuleSet.Load(DocumentWithWaiver("\"check\": \"nope\", \"theme\": \"light\", \"ratio\": 1.0, \"reason\": \"x\""));

        Assert.Null(result.RuleSet);
        Assert.Contains(result.Problems, problem => problem.Contains("unknown check", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_RejectsAWaiverWithoutAReason()
    {
        var result = ContrastRuleSet.Load(DocumentWithWaiver("\"check\": \"text-on-background\", \"theme\": \"light\", \"ratio\": 1.0"));

        Assert.Null(result.RuleSet);
        Assert.Contains(result.Problems, problem => problem.Contains("missing its 'reason'", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_AcceptsAWaiverForADeclaredCheck()
    {
        var result = ContrastRuleSet.Load(
            DocumentWithWaiver("\"check\": \"text-on-background\", \"theme\": \"dark\", \"ratio\": 4.41, \"reason\": \"tracked\""));

        Assert.Empty(result.Problems);
        var waiver = Assert.Single(result.RuleSet!.Waivers);
        Assert.Equal(ThemeScope.Dark, waiver.Theme);
        Assert.Equal(4.41, waiver.Ratio);
        Assert.NotNull(result.RuleSet.FindWaiver("text-on-background", ThemeScope.Dark));
        Assert.Null(result.RuleSet.FindWaiver("text-on-background", ThemeScope.Light));
    }

    [Fact]
    public void Load_WithMalformedJson_ReportsTheProblemInsteadOfThrowing()
    {
        var result = ContrastRuleSet.Load("{ not json ");

        Assert.Null(result.RuleSet);
        Assert.Contains(result.Problems, problem => problem.Contains("invalid JSON", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_WithoutChecks_ReportsAProblem()
    {
        var result = ContrastRuleSet.Load("{ \"checks\": [] }");

        Assert.Null(result.RuleSet);
        Assert.Contains(result.Problems, problem => problem.Contains("no checks", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_WithoutSeparations_IsStillValid()
    {
        var result = ContrastRuleSet.Load(MinimalDocument);

        Assert.Empty(result.Problems);
        Assert.Empty(result.RuleSet!.Separations);
    }

    [Fact]
    public void Load_DefaultsASeparationToBothThemesAndAllFourVisionModels()
    {
        var result = ContrastRuleSet.Load(DocumentWithSeparation(
            "\"id\": \"set\", \"colors\": [\"--a\", \"--b\"], \"minimumDeltaE\": 16.0"));

        Assert.Empty(result.Problems);
        var separation = Assert.Single(result.RuleSet!.Separations);
        Assert.Equal(2, separation.Themes.Count);
        Assert.Equal(4, separation.Vision.Count);
        Assert.Contains(ColorVision.Deuteranopia, separation.Vision);
    }

    [Fact]
    public void Load_RejectsASeparationWithFewerThanTwoColors()
    {
        var result = ContrastRuleSet.Load(DocumentWithSeparation(
            "\"id\": \"set\", \"colors\": [\"--a\"], \"minimumDeltaE\": 16.0"));

        Assert.Null(result.RuleSet);
        Assert.Contains(result.Problems, problem => problem.Contains("at least two", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_RejectsASeparationWithoutAMinimumDistance()
    {
        var result = ContrastRuleSet.Load(DocumentWithSeparation("\"id\": \"set\", \"colors\": [\"--a\", \"--b\"]"));

        Assert.Null(result.RuleSet);
        Assert.Contains(result.Problems, problem => problem.Contains("minimumDeltaE", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_RejectsAnUnknownVisionModel()
    {
        var result = ContrastRuleSet.Load(DocumentWithSeparation(
            "\"id\": \"set\", \"colors\": [\"--a\", \"--b\"], \"minimumDeltaE\": 16.0, \"vision\": [\"monochromacy\"]"));

        Assert.Null(result.RuleSet);
        Assert.Contains(result.Problems, problem => problem.Contains("unknown vision", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_RejectsASeparationThatReusesACheckIdentifier()
    {
        var result = ContrastRuleSet.Load(DocumentWithSeparation(
            "\"id\": \"text-on-background\", \"colors\": [\"--a\", \"--b\"], \"minimumDeltaE\": 16.0"));

        Assert.Null(result.RuleSet);
        Assert.Contains(result.Problems, problem => problem.Contains("more than once", StringComparison.Ordinal));
    }

    private static string DocumentWithSeparation(string separation) =>
        FormattableString.Invariant($$"""
        {
          "checks": [
            {
              "id": "text-on-background",
              "criterion": "1.4.3",
              "description": "Body text on the page background",
              "foreground": "--color-text-primary",
              "background": "--color-background-primary",
              "requirement": "text-normal"
            }
          ],
          "separations": [ { {{separation}} } ]
        }
        """);

    private static string DocumentWithWaiver(string waiver) =>
        FormattableString.Invariant($$"""
        {
          "checks": [
            {
              "id": "text-on-background",
              "criterion": "1.4.3",
              "description": "Body text on the page background",
              "foreground": "--color-text-primary",
              "background": "--color-background-primary",
              "requirement": "text-normal"
            }
          ],
          "waivers": [ { {{waiver}} } ]
        }
        """);
}
