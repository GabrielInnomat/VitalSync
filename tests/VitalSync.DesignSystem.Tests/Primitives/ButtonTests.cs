using Bunit;
using VitalSync.DesignSystem.Primitives;

namespace VitalSync.DesignSystem.Tests.Primitives;

public sealed class ButtonTests : BunitContext
{
    [Fact]
    public void Renders_label_from_ChildContent()
    {
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Speichern"));

        Assert.Equal("Speichern", cut.Find(".vs-button__label").TextContent);
    }

    [Theory]
    [InlineData(ButtonVariant.Primary, "vs-button--primary")]
    [InlineData(ButtonVariant.Secondary, "vs-button--secondary")]
    [InlineData(ButtonVariant.Critical, "vs-button--critical")]
    public void Renders_the_variant_class(ButtonVariant variant, string expectedClass)
    {
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Variant, variant)
            .AddChildContent("Label"));

        Assert.Contains(expectedClass, cut.Find("button").ClassList);
    }

    [Fact]
    public void Defaults_to_the_primary_variant()
    {
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Label"));

        Assert.Contains("vs-button--primary", cut.Find("button").ClassList);
    }

    [Fact]
    public void Invokes_OnClick_when_clicked()
    {
        var clicked = false;
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Label")
            .Add(p => p.OnClick, () => clicked = true));

        cut.Find("button").Click();

        Assert.True(clicked);
    }

    [Fact]
    public void Renders_disabled_attribute_when_Disabled_is_true()
    {
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Label")
            .Add(p => p.Disabled, true));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Omits_disabled_attribute_by_default()
    {
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Label"));

        Assert.False(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Renders_icon_as_decorative_when_a_label_is_present()
    {
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Icon, "<svg></svg>")
            .AddChildContent("Speichern"));

        var icon = cut.Find(".vs-button__icon");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Throws_when_an_icon_is_given_without_a_label()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Button>(parameters => parameters
                .Add(p => p.Icon, "<svg></svg>")));
    }

    [Fact]
    public void Passes_unmatched_attributes_through_to_the_button_element()
    {
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Label")
            .AddUnmatched("aria-label", "Speichern"));

        Assert.Equal("Speichern", cut.Find("button").GetAttribute("aria-label"));
    }
}
