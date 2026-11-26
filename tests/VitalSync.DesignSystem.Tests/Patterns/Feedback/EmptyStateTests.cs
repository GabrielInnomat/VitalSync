using Bunit;
using Microsoft.AspNetCore.Components;
using VitalSync.DesignSystem.Icons;
using VitalSync.DesignSystem.Patterns.Feedback;

namespace VitalSync.DesignSystem.Tests.Patterns.Feedback;

public sealed class EmptyStateTests : BunitContext
{
    [Fact]
    public void Renders_the_trimmed_title_and_the_text()
    {
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Title, "  Noch keine Rezepte  ")
            .Add(p => p.HeadingLevel, 2)
            .AddChildContent("Lege dein erstes Rezept an."));

        Assert.Equal("Noch keine Rezepte", cut.Find("h2.vs-empty-state__title").TextContent);
        Assert.Equal("Lege dein erstes Rezept an.", cut.Find(".vs-empty-state__text").TextContent);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Renders_the_title_at_the_given_heading_level(int level)
    {
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Title, "Noch keine Rezepte")
            .Add(p => p.HeadingLevel, level)
            .AddChildContent("Lege dein erstes Rezept an."));

        Assert.Equal($"H{level}", cut.Find(".vs-empty-state__title").TagName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void Throws_when_the_heading_level_is_outside_2_to_6(int level)
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<EmptyState>(parameters => parameters
                .Add(p => p.Title, "Noch keine Rezepte")
                .Add(p => p.HeadingLevel, level)
                .AddChildContent("Lege dein erstes Rezept an.")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_Title_is_blank(string title)
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<EmptyState>(parameters => parameters
                .Add(p => p.Title, title)
                .Add(p => p.HeadingLevel, 2)
                .AddChildContent("Lege dein erstes Rezept an.")));
    }

    [Fact]
    public void Throws_when_no_ChildContent_is_given()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<EmptyState>(parameters => parameters
                .Add(p => p.Title, "Noch keine Rezepte")
                .Add(p => p.HeadingLevel, 2)));
    }

    [Fact]
    public void Renders_no_icon_and_no_action_by_default()
    {
        var cut = RenderEmptyState();

        Assert.Empty(cut.FindAll(".vs-empty-state__icon"));
        Assert.Empty(cut.FindAll(".vs-empty-state__action"));
    }

    [Fact]
    public void Shows_the_given_icon_hidden_from_assistive_technology()
    {
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Title, "Noch keine Rezepte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Icon, SystemIconName.ForkKnife)
            .AddChildContent("Lege dein erstes Rezept an."));

        var icon = cut.Find(".vs-empty-state__icon");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        Assert.NotNull(icon.QuerySelector("svg"));
    }

    [Fact]
    public void Renders_the_action_below_the_text()
    {
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Title, "Noch keine Rezepte")
            .Add(p => p.HeadingLevel, 2)
            .AddChildContent("Lege dein erstes Rezept an.")
            .Add(p => p.Action, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "a");
                builder.AddAttribute(1, "href", "/rezepte/neu");
                builder.AddContent(2, "Rezept anlegen");
                builder.CloseElement();
            })));

        Assert.Equal("Rezept anlegen", cut.Find(".vs-empty-state__text + .vs-empty-state__action > a").TextContent);
    }

    [Fact]
    public void Is_not_a_live_region()
    {
        var cut = RenderEmptyState();

        var root = cut.Find(".vs-empty-state");
        Assert.False(root.HasAttribute("role"));
        Assert.False(root.HasAttribute("aria-live"));
    }

    [Fact]
    public void Merges_additional_classes_and_passes_other_attributes_through()
    {
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Title, "Noch keine Rezepte")
            .Add(p => p.HeadingLevel, 2)
            .AddChildContent("Lege dein erstes Rezept an.")
            .AddUnmatched("class", "extra")
            .AddUnmatched("data-testid", "recipes-empty"));

        var root = cut.Find(".vs-empty-state");
        Assert.Contains("extra", root.ClassList);
        Assert.Equal("recipes-empty", root.GetAttribute("data-testid"));
    }

    private IRenderedComponent<EmptyState> RenderEmptyState() =>
        Render<EmptyState>(parameters => parameters
            .Add(p => p.Title, "Noch keine Rezepte")
            .Add(p => p.HeadingLevel, 2)
            .AddChildContent("Lege dein erstes Rezept an."));
}
