using Bunit;
using VitalSync.DesignSystem.Primitives;

namespace VitalSync.DesignSystem.Tests.Primitives;

public sealed class BadgeTests : BunitContext
{
    private const string TagPathStart = "M243.31,136";
    private const string CheckCirclePathStart = "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm45.66";
    private const string WarningPathStart = "M236.8,188.09";
    private const string XCirclePathStart = "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm37.66";

    [Fact]
    public void Renders_label_from_ChildContent()
    {
        var cut = Render<Badge>(parameters => parameters
            .AddChildContent("Vegetarisch"));

        Assert.Equal("Vegetarisch", cut.Find(".vs-badge__label").TextContent);
    }

    [Fact]
    public void Throws_when_no_ChildContent_is_given()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Badge>(parameters => parameters
                .Add(p => p.Variant, BadgeVariant.Success)));
    }

    [Fact]
    public void Defaults_to_the_neutral_variant()
    {
        var cut = Render<Badge>(parameters => parameters
            .AddChildContent("Vegetarisch"));

        Assert.Contains("vs-badge--neutral", cut.Find(".vs-badge").ClassList);
    }

    [Fact]
    public void Renders_the_default_tag_icon_for_a_neutral_badge_without_a_custom_icon()
    {
        var cut = Render<Badge>(parameters => parameters
            .AddChildContent("Vegetarisch"));

        Assert.StartsWith(TagPathStart, IconPath(cut), StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_the_custom_icon_for_a_neutral_badge()
    {
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Icon, "<svg class=\"custom\"></svg>")
            .AddChildContent("Vegetarisch"));

        var icon = cut.Find(".vs-badge__icon");
        Assert.NotNull(icon.QuerySelector("svg.custom"));
        Assert.Single(icon.QuerySelectorAll("svg"));
    }

    [Theory]
    [InlineData(BadgeVariant.Success, "vs-badge--success", CheckCirclePathStart)]
    [InlineData(BadgeVariant.Warning, "vs-badge--warning", WarningPathStart)]
    [InlineData(BadgeVariant.Critical, "vs-badge--critical", XCirclePathStart)]
    public void Renders_the_fixed_icon_and_class_of_a_status_variant(
        BadgeVariant variant,
        string expectedClass,
        string expectedPathStart)
    {
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Variant, variant)
            .AddChildContent("Status"));

        Assert.Contains(expectedClass, cut.Find(".vs-badge").ClassList);
        Assert.StartsWith(expectedPathStart, IconPath(cut), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(BadgeVariant.Success)]
    [InlineData(BadgeVariant.Warning)]
    [InlineData(BadgeVariant.Critical)]
    public void Throws_when_a_status_variant_is_given_a_custom_icon(BadgeVariant variant)
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Badge>(parameters => parameters
                .Add(p => p.Variant, variant)
                .Add(p => p.Icon, "<svg></svg>")
                .AddChildContent("Status")));
    }

    [Theory]
    [InlineData(BadgeVariant.Success, "Erfolg: ")]
    [InlineData(BadgeVariant.Warning, "Warnung: ")]
    [InlineData(BadgeVariant.Critical, "Fehler: ")]
    public void Prefixes_a_status_label_with_its_kind_for_screen_readers_only(BadgeVariant variant, string expectedPrefix)
    {
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Variant, variant)
            .AddChildContent("Im Ziel"));

        Assert.Equal(expectedPrefix, cut.Find(".vs-badge__label > .vs-badge__kind").TextContent);
        Assert.Equal($"{expectedPrefix}Im Ziel", cut.Find(".vs-badge__label").TextContent);
    }

    [Fact]
    public void Adds_no_kind_prefix_to_a_neutral_badge()
    {
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Href, "/rezepte/vegetarisch")
            .AddChildContent("Vegetarisch"));

        Assert.Empty(cut.FindAll(".vs-badge__kind"));
    }

    [Fact]
    public void Renders_the_icon_as_decorative()
    {
        var cut = Render<Badge>(parameters => parameters
            .AddChildContent("Vegetarisch"));

        var icon = cut.Find(".vs-badge__icon");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        Assert.Equal("false", icon.QuerySelector("svg")?.GetAttribute("focusable"));
    }

    [Fact]
    public void Renders_as_a_span_when_no_Href_is_given()
    {
        var cut = Render<Badge>(parameters => parameters
            .AddChildContent("Vegetarisch"));

        Assert.Empty(cut.FindAll("a"));
        Assert.NotEmpty(cut.FindAll("span.vs-badge"));
    }

    [Fact]
    public void Renders_as_a_link_when_Href_is_given()
    {
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Variant, BadgeVariant.Critical)
            .Add(p => p.Href, "/workouts/123")
            .AddChildContent("Verpasst"));

        var link = cut.Find("a.vs-badge");
        Assert.Equal("/workouts/123", link.GetAttribute("href"));
    }

    [Fact]
    public void Gives_the_icon_wrapper_the_same_css_scope_as_the_badge_so_its_styles_apply()
    {
        var cut = Render<Badge>(parameters => parameters
            .AddChildContent("Vegetarisch"));

        var rootScope = ScopeAttributes(cut.Find(".vs-badge"));
        Assert.NotEmpty(rootScope);
        Assert.Equal(rootScope, ScopeAttributes(cut.Find(".vs-badge__icon")));
        Assert.Equal(rootScope, ScopeAttributes(cut.Find(".vs-badge__label")));
    }

    [Fact]
    public void Appends_a_passed_class_instead_of_replacing_the_component_classes()
    {
        var cut = Render<Badge>(parameters => parameters
            .AddUnmatched("class", "extra")
            .AddChildContent("Vegetarisch"));

        var classes = cut.Find(".vs-badge").ClassList;
        Assert.Contains("vs-badge--neutral", classes);
        Assert.Contains("extra", classes);
    }

    private static string IconPath(IRenderedComponent<Badge> cut) =>
        cut.Find(".vs-badge__icon svg path").GetAttribute("d") ?? "";

    private static string[] ScopeAttributes(AngleSharp.Dom.IElement element) =>
        [.. element.Attributes
            .Select(attribute => attribute.Name)
            .Where(name => name.StartsWith("b-", StringComparison.Ordinal))];
}
