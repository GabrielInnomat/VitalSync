namespace VitalSync.DesignTokens.Contrast.Tests;

public class TokenIdentityTests
{
    public static TheoryData<string, string, string> GuardedPairs => new()
    {
        { "input surface vs. page background", "--color-background-input", "--color-background-primary" },
        { "input surface vs. card background", "--color-background-input", "--card-background" },
    };

    [Theory]
    [MemberData(nameof(GuardedPairs))]
    public void ThePairStaysDistinctInLightMode(string name, string first, string second) =>
        AssertDistinct(ThemeScope.Light, name, first, second);

    [Theory]
    [MemberData(nameof(GuardedPairs))]
    public void ThePairStaysDistinctInDarkMode(string name, string first, string second) =>
        AssertDistinct(ThemeScope.Dark, name, first, second);

    private static void AssertDistinct(ThemeScope theme, string name, string first, string second)
    {
        var tokens = LoadRepositoryTokens();
        var firstResolution = tokens.ResolveColor(first, theme);
        var secondResolution = tokens.ResolveColor(second, theme);

        Assert.True(
            firstResolution.Color is not null,
            FormattableString.Invariant($"{first} did not resolve to a colour in {theme}: {firstResolution.Failure}"));
        Assert.True(
            secondResolution.Color is not null,
            FormattableString.Invariant($"{second} did not resolve to a colour in {theme}: {secondResolution.Failure}"));

        Assert.False(
            firstResolution.Color == secondResolution.Color,
            FormattableString.Invariant(
                $"{name}: {first} and {second} both resolve to {firstResolution.Color!.Value.ToHex()} in {theme} — two distinct roles have aliased by coincidence"));
    }

    private static CssCustomProperties LoadRepositoryTokens()
    {
        var root = RepositoryLocator.RequireRoot();

        return CssCustomProperties.Parse(File.ReadAllText(RepositoryLocator.TokensPath(root)));
    }
}
