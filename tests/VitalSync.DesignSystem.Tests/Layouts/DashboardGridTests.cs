using Bunit;
using VitalSync.DesignSystem.Layouts;

namespace VitalSync.DesignSystem.Tests.Layouts;

public sealed class DashboardGridTests : BunitContext
{
    [Fact]
    public void Renders_a_list_with_one_item_per_card_in_the_given_order()
    {
        var cut = Render<DashboardGrid>(parameters => parameters
            .AddChildContent<DashboardGridItem>(item => item.AddChildContent("Schritte"))
            .AddChildContent<DashboardGridItem>(item => item.AddChildContent("Puls")));

        var list = cut.Find("ul");
        Assert.Equal("vs-dashboard-grid", list.ClassName);
        Assert.Equal("list", list.GetAttribute("role"));
        var items = list.QuerySelectorAll(":scope > li");
        Assert.Equal(["Schritte", "Puls"], items.Select(i => i.TextContent.Trim()));
        Assert.All(items, i => Assert.Equal("vs-dashboard-grid__item", i.ClassName));
    }

    [Fact]
    public void Marks_a_wide_item()
    {
        var cut = Render<DashboardGridItem>(parameters => parameters
            .Add(p => p.Wide, true)
            .AddChildContent("Verlauf"));

        Assert.Equal("vs-dashboard-grid__item vs-dashboard-grid__item--wide", cut.Find("li").ClassName);
    }

    [Fact]
    public void Merges_additional_classes_and_keeps_the_list_role()
    {
        var cut = Render<DashboardGrid>(parameters => parameters
            .AddUnmatched("class", "extra")
            .AddUnmatched("role", "presentation")
            .AddUnmatched("aria-label", "Heute")
            .AddChildContent<DashboardGridItem>(item => item
                .AddUnmatched("class", "extra-item")
                .AddChildContent("Schritte")));

        var list = cut.Find("ul");
        Assert.Equal("vs-dashboard-grid extra", list.ClassName);
        Assert.Equal("list", list.GetAttribute("role"));
        Assert.Equal("Heute", list.GetAttribute("aria-label"));
        Assert.Equal("vs-dashboard-grid__item extra-item", cut.Find("li").ClassName);
    }

    [Fact]
    public void Throws_when_the_grid_has_no_ChildContent()
    {
        Assert.Throws<InvalidOperationException>(() => Render<DashboardGrid>());
    }

    [Fact]
    public void Throws_when_an_item_has_no_ChildContent()
    {
        Assert.Throws<InvalidOperationException>(() => Render<DashboardGridItem>());
    }
}
