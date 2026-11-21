using Bunit;
using VitalSync.DesignSystem.Primitives;

namespace VitalSync.DesignSystem.Tests.Primitives;

public sealed class ToggletipTests : BunitContext
{
    private IRenderedComponent<Toggletip> RenderToggletip(string subject = "Grundumsatz", string text = "Energie, die dein Körper in Ruhe verbraucht.") =>
        Render<Toggletip>(parameters => parameters
            .Add(p => p.Subject, subject)
            .Add(p => p.Text, text));

    [Fact]
    public void Renders_a_plain_button_that_opens_its_popup_without_script()
    {
        var cut = RenderToggletip();

        var button = cut.Find("button");
        var popup = cut.Find(".vs-toggletip__popup");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal(popup.Id, button.GetAttribute("popovertarget"));
        Assert.Equal("auto", popup.GetAttribute("popover"));
        Assert.Equal("Energie, die dein Körper in Ruhe verbraucht.", popup.TextContent);
    }

    [Fact]
    public void Places_the_popup_directly_after_its_button()
    {
        var cut = RenderToggletip();

        Assert.Equal(cut.Find(".vs-toggletip__popup").Id, cut.Find("button").NextElementSibling?.Id);
    }

    [Fact]
    public void Names_the_button_with_visually_hidden_text_and_hides_the_icon()
    {
        var cut = RenderToggletip();

        var button = cut.Find("button");
        Assert.Equal("Hilfe zu Grundumsatz", cut.Find(".vs-toggletip__name").TextContent);
        Assert.False(button.HasAttribute("aria-label"));
        Assert.Equal("true", cut.Find(".vs-toggletip__icon").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Anchors_the_popup_to_its_own_button()
    {
        var cut = RenderToggletip();

        var popupId = cut.Find(".vs-toggletip__popup").Id;
        Assert.Equal($"anchor-name: --{popupId}", cut.Find("button").GetAttribute("style"));
        Assert.Equal($"position-anchor: --{popupId}", cut.Find(".vs-toggletip__popup").GetAttribute("style"));
    }

    [Fact]
    public void Gives_every_instance_its_own_popup()
    {
        var first = RenderToggletip();
        var second = RenderToggletip();

        Assert.NotEqual(first.Find(".vs-toggletip__popup").Id, second.Find(".vs-toggletip__popup").Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_Subject_is_blank(string subject)
    {
        Assert.Throws<InvalidOperationException>(() => RenderToggletip(subject: subject));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_Text_is_blank(string text)
    {
        Assert.Throws<InvalidOperationException>(() => RenderToggletip(text: text));
    }
}
