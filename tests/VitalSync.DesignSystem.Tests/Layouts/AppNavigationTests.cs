using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Layouts;

namespace VitalSync.DesignSystem.Tests.Layouts;

public sealed class AppNavigationTests : BunitContext
{
    [Fact]
    public void Renders_the_title_as_a_start_page_link_and_a_collapsed_menu_button_for_a_named_navigation()
    {
        var cut = RenderNavigation();

        var title = cut.Find("a.vs-app-navigation__title");
        Assert.Equal("VitalSync", title.TextContent);
        Assert.Equal("", title.GetAttribute("href"));

        var toggle = cut.Find("button.vs-app-navigation__toggle");
        var nav = cut.Find("nav");
        Assert.Equal("button", toggle.GetAttribute("type"));
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        Assert.Equal(nav.Id, toggle.GetAttribute("aria-controls"));
        Assert.Equal("Menü", toggle.TextContent.Trim());
        Assert.All(toggle.QuerySelectorAll(".vs-app-navigation__toggle-icon"), icon => Assert.Equal("true", icon.GetAttribute("aria-hidden")));
        Assert.Equal("Hauptnavigation", nav.GetAttribute("aria-label"));
        Assert.Equal(["Übersicht", "Ernährung", "Fitness"], cut.FindAll("nav > ul > li > a").Select(link => link.TextContent.Trim()));
    }

    [Fact]
    public void Gives_every_instance_its_own_navigation_id()
    {
        var first = RenderNavigation();
        var second = RenderNavigation();

        Assert.NotEqual(first.Find("nav").Id, second.Find("nav").Id);
    }

    [Fact]
    public void Marks_only_the_start_page_as_current_at_the_root()
    {
        var cut = RenderNavigation();

        Assert.Equal(["page", null, null], CurrentStates(cut));
    }

    [Theory]
    [InlineData("nutrition")]
    [InlineData("nutrition/")]
    [InlineData("nutrition/recipes/42")]
    [InlineData("Nutrition?week=3")]
    [InlineData("nutrition#plan")]
    public void Marks_an_area_as_current_for_its_own_and_nested_pages(string address)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(address);

        var cut = RenderNavigation();

        Assert.Equal([null, "page", null], CurrentStates(cut));
    }

    [Fact]
    public void Does_not_mark_an_area_whose_name_only_starts_the_address()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("nutritionist");

        var cut = RenderNavigation();

        Assert.Equal([null, null, null], CurrentStates(cut));
    }

    [Fact]
    public void Matches_only_the_exact_page_when_Match_is_All()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("fitness/plans");

        var cut = Render<AppNavigation>(parameters => parameters
            .Add(p => p.Title, "VitalSync")
            .AddChildContent<AppNavigationLink>(link => link
                .Add(p => p.Href, "fitness")
                .Add(p => p.Title, "Fitness")
                .Add(p => p.Match, NavLinkMatch.All)));

        Assert.False(cut.Find("a.vs-app-navigation__link").HasAttribute("aria-current"));
    }

    [Fact]
    public void Moves_the_current_mark_when_the_address_changes()
    {
        var cut = RenderNavigation();

        Services.GetRequiredService<NavigationManager>().NavigateTo("fitness");

        cut.WaitForAssertion(() => Assert.Equal([null, null, "page"], CurrentStates(cut)));
    }

    [Fact]
    public void Hides_the_entry_icon_from_screen_readers_and_omits_its_slot_without_an_icon()
    {
        var cut = Render<AppNavigation>(parameters => parameters
            .Add(p => p.Title, "VitalSync")
            .AddChildContent<AppNavigationLink>(link => link
                .Add(p => p.Href, "")
                .Add(p => p.Title, "Übersicht")
                .Add(p => p.Icon, "<svg></svg>"))
            .AddChildContent<AppNavigationLink>(link => link
                .Add(p => p.Href, "fitness")
                .Add(p => p.Title, "Fitness")));

        var links = cut.FindAll("a.vs-app-navigation__link");
        Assert.Equal("true", links[0].QuerySelector(".vs-app-navigation__icon")!.GetAttribute("aria-hidden"));
        Assert.Null(links[1].QuerySelector(".vs-app-navigation__icon"));
    }

    [Fact]
    public void Merges_extra_attributes_and_classes_onto_the_root()
    {
        var cut = Render<AppNavigation>(parameters => parameters
            .Add(p => p.Title, "VitalSync")
            .AddUnmatched("class", "shell")
            .AddUnmatched("data-test", "x")
            .AddChildContent<AppNavigationLink>(link => link.Add(p => p.Href, "").Add(p => p.Title, "Übersicht")));

        var root = cut.Find("header");
        Assert.Equal("vs-app-navigation shell", root.GetAttribute("class"));
        Assert.Equal("x", root.GetAttribute("data-test"));
        Assert.True(root.HasAttribute("data-vs-app-navigation"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_without_a_title(string title)
    {
        Assert.Throws<InvalidOperationException>(() => Render<AppNavigation>(parameters => parameters
            .Add(p => p.Title, title)
            .AddChildContent<AppNavigationLink>(link => link.Add(p => p.Href, "").Add(p => p.Title, "Übersicht"))));
    }

    [Fact]
    public void Throws_without_entries()
    {
        Assert.Throws<InvalidOperationException>(() => Render<AppNavigation>(parameters => parameters
            .Add(p => p.Title, "VitalSync")));
    }

    [Fact]
    public void Throws_for_an_entry_outside_the_navigation()
    {
        Assert.Throws<InvalidOperationException>(() => Render<AppNavigationLink>(parameters => parameters
            .Add(p => p.Href, "fitness")
            .Add(p => p.Title, "Fitness")));
    }

    [Theory]
    [InlineData("fitness", "")]
    [InlineData("fitness", "  ")]
    [InlineData(null, "Fitness")]
    [InlineData("fitness?week=3", "Fitness")]
    [InlineData("fitness#plan", "Fitness")]
    public void Throws_for_an_entry_without_a_title_or_with_an_unusable_address(string? href, string title)
    {
        Assert.Throws<InvalidOperationException>(() => Render<AppNavigation>(parameters => parameters
            .Add(p => p.Title, "VitalSync")
            .AddChildContent<AppNavigationLink>(link => link
                .Add(p => p.Href, href!)
                .Add(p => p.Title, title))));
    }

    private IRenderedComponent<AppNavigation> RenderNavigation() =>
        Render<AppNavigation>(parameters => parameters
            .Add(p => p.Title, "VitalSync")
            .AddChildContent<AppNavigationLink>(link => link
                .Add(p => p.Href, "")
                .Add(p => p.Title, "Übersicht")
                .Add(p => p.Match, NavLinkMatch.All))
            .AddChildContent<AppNavigationLink>(link => link
                .Add(p => p.Href, "nutrition")
                .Add(p => p.Title, "Ernährung"))
            .AddChildContent<AppNavigationLink>(link => link
                .Add(p => p.Href, "fitness")
                .Add(p => p.Title, "Fitness")));

    private static string?[] CurrentStates(IRenderedComponent<AppNavigation> cut) =>
        [.. cut.FindAll("a.vs-app-navigation__link").Select(link => link.GetAttribute("aria-current"))];
}
