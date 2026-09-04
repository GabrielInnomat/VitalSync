using System.Globalization;

namespace VitalSync.DesignTokens.Contrast.Tests;

public class DesignSystemContrastTests
{
    [Fact]
    public void TheTokenFileAndTheRuleFileCanBothBeRead()
    {
        var check = DesignTokenContrastCheck.ForRepository();

        Assert.Empty(check.Problems);
        Assert.NotEmpty(check.Results);
    }

    [Fact]
    public void EveryTokenPairNamedByTheRulesResolvesToAColor()
    {
        var check = DesignTokenContrastCheck.ForRepository();

        var unresolved = check.Results
            .Where(result => result.Status == ContrastCheckStatus.Unresolved)
            .Select(ContrastReport.Describe)
            .ToList();

        Assert.True(unresolved.Count == 0, string.Join(Environment.NewLine, unresolved));
    }

    [Fact]
    public void NoWaiverIsStaleAndNoWaivedPairHasGotWorse()
    {
        var check = DesignTokenContrastCheck.ForRepository();

        var drifted = check.Results
            .Where(result => result.Status is ContrastCheckStatus.WaiverObsolete or ContrastCheckStatus.WaiverRegressed)
            .Select(ContrastReport.Describe)
            .ToList();

        Assert.True(drifted.Count == 0, string.Join(Environment.NewLine, drifted));
    }

    [Fact]
    public void NoUnwaivedTokenPairViolatesWcagAa()
    {
        var check = DesignTokenContrastCheck.ForRepository();

        var violations = check.Results
            .Where(result => result.Status == ContrastCheckStatus.Failed)
            .Select(ContrastReport.Describe)
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void EveryWaivedPairIsStillTracked()
    {
        var check = DesignTokenContrastCheck.ForRepository();

        foreach (var result in check.Results.Where(result => result.Status == ContrastCheckStatus.Waived))
        {
            Assert.NotNull(result.Waiver);
            Assert.False(string.IsNullOrWhiteSpace(result.Waiver.Reason));
        }
    }

    [Fact]
    public void EverySeparationSetResolvesAndStaysApart()
    {
        var check = DesignTokenContrastCheck.ForRepository();

        Assert.NotEmpty(check.Separations);

        var findings = check.Separations
            .Where(result => result.Status != ContrastCheckStatus.Passed)
            .Select(ContrastReport.Describe)
            .ToList();

        Assert.True(findings.Count == 0, string.Join(Environment.NewLine, findings));
    }

    [Fact]
    public void EverySeparationSetIsCheckedUnderAllThreeColorVisionDeficiencies()
    {
        var check = DesignTokenContrastCheck.ForRepository();

        foreach (var result in check.Separations)
        {
            Assert.Contains(ColorVision.Protanopia, result.Rule.Vision);
            Assert.Contains(ColorVision.Deuteranopia, result.Rule.Vision);
            Assert.Contains(ColorVision.Tritanopia, result.Rule.Vision);
        }
    }

    [Fact]
    public void TheReportRendersEveryEvaluatedPair()
    {
        var check = DesignTokenContrastCheck.ForRepository();

        var report = ContrastReport.Render(check.Results, check.Separations, strict: false);

        Assert.Contains(
            check.Results.Count.ToString(CultureInfo.InvariantCulture) + " checks",
            report,
            StringComparison.Ordinal);

        foreach (var result in check.Results)
        {
            Assert.Contains(result.Rule.Id, report, StringComparison.Ordinal);
        }
    }
}
