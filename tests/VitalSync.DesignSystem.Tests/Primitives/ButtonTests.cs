using Bunit;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Primitives;

namespace VitalSync.DesignSystem.Tests.Primitives;

public sealed class ButtonTests : BunitContext
{
    public ButtonTests()
    {
        Services.AddScoped<LiveAnnouncer>();
    }

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

    [Fact]
    public void Appends_a_passed_class_to_the_component_classes()
    {
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Label")
            .AddUnmatched("class", "extra"));

        var classes = cut.Find("button").ClassList;
        Assert.Contains("vs-button", classes);
        Assert.Contains("vs-button--primary", classes);
        Assert.Contains("extra", classes);
    }

    [Fact]
    public void Shows_the_loading_label_and_a_decorative_ring_in_place_of_the_icon_while_loading()
    {
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Icon, "<svg></svg>")
            .Add(p => p.Loading, true)
            .AddChildContent("Speichern"));

        Assert.Equal("Bitte warten\u00A0…", cut.Find(".vs-button__label").TextContent);
        Assert.Equal("true", cut.Find(".vs-button__ring").GetAttribute("aria-hidden"));
        Assert.Empty(cut.FindAll(".vs-button__icon"));
    }

    [Fact]
    public void Uses_a_custom_LoadingLabel()
    {
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Loading, true)
            .Add(p => p.LoadingLabel, "Wird gespeichert\u00A0…")
            .AddChildContent("Speichern"));

        Assert.Equal("Wird gespeichert\u00A0…", cut.Find(".vs-button__label").TextContent);
    }

    [Fact]
    public void Stays_focusable_while_loading_even_when_Disabled_is_also_set()
    {
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Loading, true)
            .Add(p => p.Disabled, true)
            .AddChildContent("Speichern"));

        var button = cut.Find("button");
        Assert.False(button.HasAttribute("disabled"));
        Assert.Equal("true", button.GetAttribute("aria-disabled"));
        Assert.Contains("vs-button--loading", button.ClassList);
    }

    [Fact]
    public void Omits_aria_disabled_and_the_loading_class_when_not_loading()
    {
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Speichern"));

        var button = cut.Find("button");
        Assert.False(button.HasAttribute("aria-disabled"));
        Assert.DoesNotContain("vs-button--loading", button.ClassList);
        Assert.Empty(cut.FindAll(".vs-button__ring"));
    }

    [Fact]
    public void Ignores_clicks_while_loading()
    {
        var clicked = false;
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Loading, true)
            .Add(p => p.OnClick, () => clicked = true)
            .AddChildContent("Speichern"));

        cut.Find("button").Click();

        Assert.False(clicked);
    }

    [Theory]
    [InlineData(false, "submit")]
    [InlineData(true, "button")]
    public void Stops_being_a_submit_button_while_loading(bool loading, string expectedType)
    {
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Type, "submit")
            .Add(p => p.Loading, loading)
            .AddChildContent("Speichern"));

        Assert.Equal(expectedType, cut.Find("button").GetAttribute("type"));
    }

    [Fact]
    public void Announces_the_loading_label_once_when_loading_starts_on_an_unfocused_button()
    {
        var announcer = Services.GetRequiredService<LiveAnnouncer>();
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Speichern"));

        Assert.Empty(cut.FindAll("[role=status]"));

        cut.Render(parameters => parameters.Add(p => p.Loading, true));
        cut.Render(parameters => parameters.Add(p => p.Loading, true));

        Assert.Single(announcer.Announcements);
        Assert.False(announcer.Announcements[0].Urgent);
        Assert.Equal("Bitte warten\u00A0…", Text(announcer.Announcements[0]));

        cut.Render(parameters => parameters.Add(p => p.Loading, false));

        Assert.Single(announcer.Announcements);
    }

    [Fact]
    public void Leaves_the_announcement_to_the_new_name_when_the_button_has_focus()
    {
        var announcer = Services.GetRequiredService<LiveAnnouncer>();
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Speichern"));

        cut.Find("button").Focus();
        cut.Render(parameters => parameters.Add(p => p.Loading, true));

        Assert.Empty(announcer.Announcements);
        Assert.Equal("Bitte warten\u00A0…", cut.Find(".vs-button__label").TextContent);
    }

    [Fact]
    public void Does_not_announce_a_button_that_is_already_loading_when_it_appears()
    {
        var announcer = Services.GetRequiredService<LiveAnnouncer>();

        Render<Button>(parameters => parameters
            .Add(p => p.Loading, true)
            .AddChildContent("Speichern"));

        Assert.Empty(announcer.Announcements);
    }

    private string Text(LiveAnnouncement announcement) =>
        string.Concat(Render(announcement.Content).Nodes.Select(node => node.TextContent));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_LoadingLabel_is_blank(string loadingLabel)
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Button>(parameters => parameters
                .Add(p => p.LoadingLabel, loadingLabel)
                .AddChildContent("Speichern")));
    }
}
