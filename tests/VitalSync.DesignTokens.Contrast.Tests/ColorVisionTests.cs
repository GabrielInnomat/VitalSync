namespace VitalSync.DesignTokens.Contrast.Tests;

public class ColorVisionTests
{
    private static SrgbColor Parse(string hex)
    {
        Assert.True(SrgbColor.TryParse(hex, out var color));

        return color;
    }

    private static double Distance(string first, string second, ColorVision vision) =>
        CieLab.Distance(
            CieLab.FromColor(ColorVisionSimulator.Simulate(Parse(first), vision)),
            CieLab.FromColor(ColorVisionSimulator.Simulate(Parse(second), vision)));

    [Fact]
    public void FromColor_ForWhite_IsFullLightnessAndNeutral()
    {
        var white = CieLab.FromColor(Parse("#FFFFFF"));

        Assert.Equal(100.0, white.Lightness, precision: 2);
        Assert.Equal(0.0, white.A, precision: 2);
        Assert.Equal(0.0, white.B, precision: 2);
    }

    [Fact]
    public void FromColor_ForBlack_IsZeroLightness()
    {
        Assert.Equal(0.0, CieLab.FromColor(Parse("#000000")).Lightness, precision: 2);
    }

    [Fact]
    public void Distance_BetweenIdenticalColors_IsZero()
    {
        var color = CieLab.FromColor(Parse("#E69F00"));

        Assert.Equal(0.0, CieLab.Distance(color, color), precision: 10);
    }

    [Fact]
    public void Distance_IsSymmetric()
    {
        var first = CieLab.FromColor(Parse("#E69F00"));
        var second = CieLab.FromColor(Parse("#0072B2"));

        Assert.Equal(CieLab.Distance(first, second), CieLab.Distance(second, first), precision: 10);
    }

    [Theory]
    [InlineData("#E69F00")]
    [InlineData("#0072B2")]
    [InlineData("#F1F5F9")]
    public void Simulate_WithNormalVision_ReturnsTheInputUnchanged(string hex)
    {
        var color = Parse(hex);

        Assert.Equal(color, ColorVisionSimulator.Simulate(color, ColorVision.Normal));
    }

    [Theory]
    [InlineData("Protanopia")]
    [InlineData("Deuteranopia")]
    [InlineData("Tritanopia")]
    public void Simulate_LeavesGreyUntouched(string visionName)
    {
        var vision = Enum.Parse<ColorVision>(visionName);
        var grey = Parse("#808080");
        var simulated = ColorVisionSimulator.Simulate(grey, vision);

        Assert.True(CieLab.Distance(CieLab.FromColor(grey), CieLab.FromColor(simulated)) < 2.0);
    }

    [Fact]
    public void Simulate_PullsRedAndGreenTogetherForDeuteranopia()
    {
        var normal = Distance("#009E73", "#D55E00", ColorVision.Normal);
        var deuteranopic = Distance("#009E73", "#D55E00", ColorVision.Deuteranopia);

        Assert.True(deuteranopic < normal);
    }

    [Fact]
    public void UniformlyDarkeningOkabeItoCollapsesTwoSeriesUnderDeuteranopia()
    {
        var original = Distance("#E69F00", "#D55E00", ColorVision.Deuteranopia);
        var naivelyDarkened = Distance("#B27B00", "#D55E00", ColorVision.Deuteranopia);

        Assert.True(original > 15.0, $"expected the Okabe-Ito pair to stay apart, was {original}");
        Assert.True(naivelyDarkened < 5.0, $"expected the darkened pair to collapse, was {naivelyDarkened}");
    }

    [Fact]
    public void TheDerivedLightPaletteKeepsThatPairApart()
    {
        var derived = Distance("#C07500", "#AE3E00", ColorVision.Deuteranopia);

        Assert.True(derived > 15.0, $"expected the derived pair to stay apart, was {derived}");
    }
}
