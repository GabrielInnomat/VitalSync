namespace VitalSync.DesignTokens.Contrast.Tests;

public class SrgbColorTests
{
    [Theory]
    [InlineData("#FFFFFF", 255, 255, 255)]
    [InlineData("#000", 0, 0, 0)]
    [InlineData("#0F172A", 15, 23, 42)]
    [InlineData("#0f172aff", 15, 23, 42)]
    [InlineData("  #E2E8F0  ", 226, 232, 240)]
    [InlineData("rgb(37, 99, 235)", 37, 99, 235)]
    [InlineData("rgb(37 99 235)", 37, 99, 235)]
    [InlineData("rgba(37, 99, 235, 1)", 37, 99, 235)]
    public void TryParse_WithSupportedNotation_ReturnsColor(string value, int red, int green, int blue)
    {
        Assert.True(SrgbColor.TryParse(value, out var color));
        Assert.Equal(new SrgbColor((byte)red, (byte)green, (byte)blue), color);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0.5rem")]
    [InlineData("#12345")]
    [InlineData("#GGHHII")]
    [InlineData("hsl(210, 40%, 96%)")]
    public void TryParse_WithUnsupportedNotation_Fails(string? value) =>
        Assert.False(SrgbColor.TryParse(value, out _));

    [Theory]
    [InlineData("rgba(15, 23, 42, 0.16)")]
    [InlineData("#0F172A80")]
    public void TryParse_WithTranslucentColor_FailsBecauseCompositingIsNotSupported(string value) =>
        Assert.False(SrgbColor.TryParse(value, out _));

    [Fact]
    public void ContrastRatio_BetweenBlackAndWhite_IsTwentyOne()
    {
        var ratio = SrgbColor.ContrastRatio(new SrgbColor(0, 0, 0), new SrgbColor(255, 255, 255));

        Assert.Equal(21.0, ratio, precision: 2);
    }

    [Fact]
    public void ContrastRatio_ForIdenticalColors_IsOne()
    {
        var color = new SrgbColor(0x25, 0x63, 0xEB);

        Assert.Equal(1.0, SrgbColor.ContrastRatio(color, color), precision: 4);
    }

    [Fact]
    public void ContrastRatio_IsSymmetric()
    {
        var first = new SrgbColor(0x0F, 0x17, 0x2A);
        var second = new SrgbColor(0xF8, 0xFA, 0xFC);

        Assert.Equal(
            SrgbColor.ContrastRatio(first, second),
            SrgbColor.ContrastRatio(second, first),
            precision: 10);
    }

    [Theory]
    [InlineData("#475569", "#FFFFFF", 7.58)]
    [InlineData("#2563EB", "#FFFFFF", 5.17)]
    [InlineData("#DC2626", "#FFFFFF", 4.83)]
    [InlineData("#16A34A", "#FFFFFF", 3.30)]
    [InlineData("#94A3B8", "#FFFFFF", 2.56)]
    [InlineData("#60A5FA", "#0F172A", 7.02)]
    public void ContrastRatio_MatchesTheValuesDocumentedInTheTokenFile(
        string foreground,
        string background,
        double expected)
    {
        Assert.True(SrgbColor.TryParse(foreground, out var first));
        Assert.True(SrgbColor.TryParse(background, out var second));

        Assert.Equal(expected, SrgbColor.ContrastRatio(first, second), precision: 2);
    }

    [Fact]
    public void ToHex_RoundTripsThroughTryParse()
    {
        var color = new SrgbColor(0x1E, 0x29, 0x3B);

        Assert.True(SrgbColor.TryParse(color.ToHex(), out var parsed));
        Assert.Equal(color, parsed);
    }
}
