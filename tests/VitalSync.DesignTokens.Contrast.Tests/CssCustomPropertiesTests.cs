namespace VitalSync.DesignTokens.Contrast.Tests;

public class CssCustomPropertiesTests
{
    [Fact]
    public void Parse_ResolvesAChainOfVarReferences()
    {
        var tokens = CssCustomProperties.Parse(
            """
            :root {
              --neutral-900: #0F172A;
              --color-text-primary: var(--neutral-900);
              --input-text-color: var(--color-text-primary);
            }
            """);

        var resolution = tokens.ResolveColor("--input-text-color", ThemeScope.Light);

        Assert.Null(resolution.Failure);
        Assert.Equal(new SrgbColor(0x0F, 0x17, 0x2A), resolution.Color);
    }

    [Fact]
    public void Parse_LetsTheDarkThemeOverrideOnlyWhatItRedeclares()
    {
        var tokens = CssCustomProperties.Parse(
            """
            :root {
              --neutral-50: #F8FAFC;
              --neutral-900: #0F172A;
              --color-text-primary: var(--neutral-900);
              --color-background-primary: var(--neutral-50);
            }
            [data-theme="dark"] {
              --color-text-primary: var(--neutral-50);
            }
            """);

        Assert.Equal(
            new SrgbColor(0xF8, 0xFA, 0xFC),
            tokens.ResolveColor("--color-text-primary", ThemeScope.Dark).Color);

        Assert.Equal(
            new SrgbColor(0xF8, 0xFA, 0xFC),
            tokens.ResolveColor("--color-background-primary", ThemeScope.Dark).Color);

        Assert.Equal(
            new SrgbColor(0x0F, 0x17, 0x2A),
            tokens.ResolveColor("--color-text-primary", ThemeScope.Light).Color);
    }

    [Fact]
    public void Parse_MergesRepeatedRootBlocksInSourceOrder()
    {
        var tokens = CssCustomProperties.Parse(
            """
            :root { --color-focus-ring: #2563EB; }
            :root { --focus-ring-color: var(--color-focus-ring); }
            """);

        Assert.Equal(
            new SrgbColor(0x25, 0x63, 0xEB),
            tokens.ResolveColor("--focus-ring-color", ThemeScope.Light).Color);
    }

    [Fact]
    public void Parse_IgnoresDeclarationsInsideAtRules()
    {
        var tokens = CssCustomProperties.Parse(
            """
            :root { --motion-duration-base: 200ms; }
            @media (prefers-reduced-motion: reduce) {
              :root { --motion-duration-base: 0ms; }
            }
            """);

        Assert.Equal("200ms", tokens.DeclarationsFor(ThemeScope.Light)["--motion-duration-base"]);
    }

    [Fact]
    public void Parse_DropsComments()
    {
        var tokens = CssCustomProperties.Parse(
            """
            :root {
              /* --color-text-primary: #FFFFFF; */
              --color-text-primary: #0F172A;
            }
            """);

        Assert.Single(tokens.DeclarationsFor(ThemeScope.Light));
        Assert.Equal(new SrgbColor(0x0F, 0x17, 0x2A), tokens.ResolveColor("--color-text-primary", ThemeScope.Light).Color);
    }

    [Fact]
    public void ResolveColor_ForAnUndeclaredToken_ReportsAFailure()
    {
        var tokens = CssCustomProperties.Parse(":root { --color-text-primary: #0F172A; }");

        var resolution = tokens.ResolveColor("--button-primary-text", ThemeScope.Light);

        Assert.Null(resolution.Color);
        Assert.Contains("--button-primary-text", resolution.Failure, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveColor_ForANonColorValue_ReportsAFailure()
    {
        var tokens = CssCustomProperties.Parse(
            """
            :root {
              --border-width-thin: 1px;
              --color-border-subtle: #E2E8F0;
              --card-border: var(--border-width-thin) solid var(--color-border-subtle);
            }
            """);

        var resolution = tokens.ResolveColor("--card-border", ThemeScope.Light);

        Assert.Null(resolution.Color);
        Assert.Contains("not a supported color", resolution.Failure, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveColor_ForACircularChain_ReportsAFailure()
    {
        var tokens = CssCustomProperties.Parse(
            """
            :root {
              --first: var(--second);
              --second: var(--first);
            }
            """);

        var resolution = tokens.ResolveColor("--first", ThemeScope.Light);

        Assert.Null(resolution.Color);
        Assert.Contains("circular", resolution.Failure, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveColor_UsesTheFallbackOfAVarReferenceWhenTheTokenIsMissing()
    {
        var tokens = CssCustomProperties.Parse(":root { --card-background: var(--missing, #FFFFFF); }");

        Assert.Equal(
            new SrgbColor(255, 255, 255),
            tokens.ResolveColor("--card-background", ThemeScope.Light).Color);
    }

    [Fact]
    public void ResolveColor_AcceptsALiteralInsteadOfATokenReference()
    {
        var tokens = CssCustomProperties.Parse(":root { --anything: #000000; }");

        Assert.Equal(
            new SrgbColor(255, 255, 255),
            tokens.ResolveColor("#FFFFFF", ThemeScope.Light).Color);
    }

    [Theory]
    [InlineData("var(--color-text-primary)", "--color-text-primary")]
    [InlineData("var( --color-text-primary )", "--color-text-primary")]
    [InlineData("var(--missing, var(--fallback))", "--missing")]
    public void TrySplitVarReference_ForASingleVarExpression_ReturnsTheReference(string value, string expected)
    {
        Assert.True(CssCustomProperties.TrySplitVarReference(value, out var reference, out _));
        Assert.Equal(expected, reference);
    }

    [Theory]
    [InlineData("1px solid var(--color-border-subtle)")]
    [InlineData("var(--border-width-thin) solid var(--color-border-subtle)")]
    [InlineData("#FFFFFF")]
    public void TrySplitVarReference_ForACompositeValue_ReturnsFalse(string value) =>
        Assert.False(CssCustomProperties.TrySplitVarReference(value, out _, out _));
}
