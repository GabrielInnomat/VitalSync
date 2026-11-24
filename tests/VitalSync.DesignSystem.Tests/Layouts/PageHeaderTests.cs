using Bunit;
using Microsoft.AspNetCore.Components;
using VitalSync.DesignSystem.Layouts;

namespace VitalSync.DesignSystem.Tests.Layouts;

public sealed class PageHeaderTests : BunitContext
{
    [Fact]
    public void Renders_the_title_as_the_only_first_level_heading_inside_a_header()
    {
        var cut = Render<PageHeader>(parameters => parameters.Add(p => p.Title, "Ernährung"));

        var header = cut.Find("header");
        Assert.Equal("vs-page-header", header.ClassName);
        var heading = Assert.Single(header.QuerySelectorAll("h1"));
        Assert.Equal("Ernährung", heading.TextContent);
        Assert.Empty(cut.FindAll("p"));
        Assert.Empty(cut.FindAll(".vs-page-header__actions"));
    }

    [Fact]
    public void Renders_the_description_below_the_title()
    {
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Ernährung")
            .Add(p => p.Description, "Zutaten, Rezepte und Speisepläne."));

        var text = cut.Find(".vs-page-header__text");
        Assert.Equal(["H1", "P"], text.Children.Select(child => child.TagName));
        Assert.Equal("Zutaten, Rezepte und Speisepläne.", text.QuerySelector("p")!.TextContent);
    }

    [Fact]
    public void Renders_the_actions_after_the_title_so_they_follow_it_in_reading_and_focus_order()
    {
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Rezepte")
            .Add(p => p.Actions, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddContent(1, "Rezept hinzufügen");
                builder.CloseElement();
            })));

        var header = cut.Find("header");
        Assert.Equal(["vs-page-header__text", "vs-page-header__actions"], header.Children.Select(child => child.ClassName));
        Assert.Equal("Rezept hinzufügen", cut.Find(".vs-page-header__actions > button").TextContent);
    }

    [Fact]
    public void Merges_additional_attributes_and_classes()
    {
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Fitness")
            .AddUnmatched("class", "extra")
            .AddUnmatched("data-test", "header"));

        var header = cut.Find("header");
        Assert.Equal("vs-page-header extra", header.ClassName);
        Assert.Equal("header", header.GetAttribute("data-test"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_blank_title(string title)
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<PageHeader>(parameters => parameters.Add(p => p.Title, title)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_blank_description(string description)
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<PageHeader>(parameters => parameters
                .Add(p => p.Title, "Fitness")
                .Add(p => p.Description, description)));
    }
}
