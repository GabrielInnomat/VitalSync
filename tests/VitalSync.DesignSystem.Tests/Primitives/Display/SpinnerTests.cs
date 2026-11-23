using Bunit;
using VitalSync.DesignSystem.Primitives.Display;

namespace VitalSync.DesignSystem.Tests.Primitives.Display;

public sealed class SpinnerTests : BunitContext
{
    [Fact]
    public void Announces_the_default_loading_text_as_a_status()
    {
        var cut = Render<Spinner>();

        var spinner = cut.Find(".vs-spinner");
        Assert.Equal("status", spinner.GetAttribute("role"));
        Assert.Equal("Wird geladen\u00A0…", cut.Find(".vs-spinner__label").TextContent);
    }

    [Fact]
    public void Shows_a_given_Label_trimmed()
    {
        var cut = Render<Spinner>(parameters => parameters
            .Add(p => p.Label, "  Rezepte werden geladen …  "));

        Assert.Equal("Rezepte werden geladen …", cut.Find(".vs-spinner__label").TextContent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_Label_is_blank(string label)
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Spinner>(parameters => parameters
                .Add(p => p.Label, label)));
    }

    [Fact]
    public void Hides_the_ring_from_assistive_technology()
    {
        var cut = Render<Spinner>();

        Assert.Equal("true", cut.Find(".vs-spinner__ring").GetAttribute("aria-hidden"));
        Assert.False(cut.Find(".vs-spinner__label").HasAttribute("aria-hidden"));
    }

    [Fact]
    public void Keeps_its_own_role_and_appends_a_passed_class()
    {
        var cut = Render<Spinner>(parameters => parameters
            .AddUnmatched("role", "img")
            .AddUnmatched("class", "page-loader")
            .AddUnmatched("data-test", "x"));

        var spinner = cut.Find(".vs-spinner");
        Assert.Equal("status", spinner.GetAttribute("role"));
        Assert.Equal("vs-spinner page-loader", spinner.GetAttribute("class"));
        Assert.Equal("x", spinner.GetAttribute("data-test"));
    }
}
