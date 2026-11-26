using System.Globalization;
using Bunit;
using VitalSync.DesignSystem.Icons;
using VitalSync.DesignSystem.Patterns.Data;

namespace VitalSync.DesignSystem.Tests.Patterns.Data;

public sealed class VitalsCardTests : BunitContext
{
    private static readonly CultureInfo SwissGerman = new("de-CH");

    [Fact]
    public void Renders_the_trimmed_title_the_value_in_Swiss_format_and_the_trimmed_unit()
    {
        var cut = RenderCard(parameters => parameters
            .Add(p => p.Title, "  Schritte  ")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, 12500m)
            .Add(p => p.Unit, "  Schritte  "));

        Assert.Equal("Schritte", cut.Find("h2.vs-vitals-card__title").TextContent);
        Assert.Equal("12’500", cut.Find(".vs-vitals-card__number").TextContent);
        Assert.Equal("Schritte", cut.Find(".vs-vitals-card__unit").TextContent);
    }

    [Fact]
    public void Renders_the_value_with_the_given_decimals()
    {
        var cut = RenderCard(parameters => parameters
            .Add(p => p.Title, "Schlaf")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, 7.46m)
            .Add(p => p.Unit, "h")
            .Add(p => p.Decimals, 1));

        Assert.Equal("7.5", cut.Find(".vs-vitals-card__number").TextContent);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Renders_the_title_at_the_given_heading_level(int level)
    {
        var cut = RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, level)
            .Add(p => p.Value, 7500m)
            .Add(p => p.Unit, "Schritte"));

        Assert.Equal($"H{level}", cut.Find(".vs-vitals-card__title").TagName);
    }

    [Fact]
    public void Says_that_there_is_no_data_when_the_value_is_missing()
    {
        var cut = RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, null)
            .Add(p => p.Unit, "Schritte")
            .Add(p => p.Goal, 10000m));

        Assert.Equal("Noch keine Daten", cut.Find(".vs-vitals-card__no-data").TextContent);
        Assert.Empty(cut.FindAll(".vs-vitals-card__value"));
        Assert.Equal("Ziel: 10’000 Schritte", cut.Find(".vs-vitals-card__goal-text").TextContent);
        Assert.Empty(cut.FindAll(".vs-vitals-card__bar"));
    }

    [Theory]
    [InlineData(7500, "75%", "inline-size: 75%")]
    [InlineData(9960, "99%", "inline-size: 99%")]
    [InlineData(12500, "125%", "inline-size: 100%")]
    [InlineData(0, "0%", "inline-size: 0%")]
    public void States_the_goal_and_how_much_of_it_is_reached_rounding_down(
        int value, string percentage, string barStyle)
    {
        var cut = RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, value)
            .Add(p => p.Unit, "Schritte")
            .Add(p => p.Goal, 10000m));

        Assert.Equal(
            $"Ziel: 10’000 Schritte, {percentage} erreicht",
            cut.Find(".vs-vitals-card__goal-text").TextContent);
        Assert.Equal("true", cut.Find(".vs-vitals-card__bar").GetAttribute("aria-hidden"));
        Assert.Equal(barStyle, cut.Find(".vs-vitals-card__bar-fill").GetAttribute("style"));
    }

    [Fact]
    public void Renders_no_goal_no_trend_no_icon_and_no_link_by_default()
    {
        var cut = RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, 7500m)
            .Add(p => p.Unit, "Schritte"));

        Assert.Empty(cut.FindAll(".vs-vitals-card__goal"));
        Assert.Empty(cut.FindAll(".vs-vitals-card__trend"));
        Assert.Empty(cut.FindAll(".vs-vitals-card__icon"));
        Assert.Empty(cut.FindAll("a"));
        Assert.Equal("vs-vitals-card", cut.Find("div").ClassName);
    }

    [Theory]
    [InlineData(VitalsTrendDirection.Up, SystemIconName.ArrowUp)]
    [InlineData(VitalsTrendDirection.Down, SystemIconName.ArrowDown)]
    [InlineData(VitalsTrendDirection.Steady, SystemIconName.ArrowRight)]
    public void Renders_the_trend_as_text_with_a_hidden_arrow(
        VitalsTrendDirection direction, SystemIconName expectedIcon)
    {
        var cut = RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, 7500m)
            .Add(p => p.Unit, "Schritte")
            .Add(p => p.Trend, new VitalsTrend(direction, "  12% mehr als gestern  ")));
        var expected = Render<SystemIcon>(parameters => parameters.Add(p => p.Name, expectedIcon));

        Assert.Equal("12% mehr als gestern", cut.Find(".vs-vitals-card__trend").TextContent.Trim());
        Assert.Equal("true", cut.Find(".vs-vitals-card__trend-icon").GetAttribute("aria-hidden"));
        Assert.Equal(
            expected.Find("path").GetAttribute("d"),
            cut.Find(".vs-vitals-card__trend-icon path").GetAttribute("d"));
    }

    [Fact]
    public void Turns_the_title_into_a_link_when_an_Href_is_given()
    {
        var cut = RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 3)
            .Add(p => p.Value, 7500m)
            .Add(p => p.Unit, "Schritte")
            .Add(p => p.Href, "  /aktivitaet  "));

        var link = cut.Find("h3.vs-vitals-card__title > a.vs-vitals-card__link");
        Assert.Equal("/aktivitaet", link.GetAttribute("href"));
        Assert.Equal("Schritte", link.TextContent);
        Assert.Single(cut.FindAll("a"));
        Assert.Contains("vs-vitals-card--link", cut.Find("div").ClassList);
    }

    [Theory]
    [InlineData(VitalsCategory.Activity, "vs-vitals-card--activity")]
    [InlineData(VitalsCategory.Nutrition, "vs-vitals-card--nutrition")]
    [InlineData(VitalsCategory.HeartRate, "vs-vitals-card--heartrate")]
    [InlineData(VitalsCategory.Sleep, "vs-vitals-card--sleep")]
    public void Marks_the_category_and_hides_the_icon(VitalsCategory category, string expectedClass)
    {
        var cut = RenderCard(parameters => parameters
            .Add(p => p.Title, "Puls")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, 62m)
            .Add(p => p.Unit, "Schläge pro Minute")
            .Add(p => p.Icon, SystemIconName.Heartbeat)
            .Add(p => p.Category, category));

        Assert.Contains(expectedClass, cut.Find("div").ClassList);
        Assert.Equal("true", cut.Find(".vs-vitals-card__icon").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Merges_additional_classes_and_passes_other_attributes()
    {
        var cut = RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, 7500m)
            .Add(p => p.Unit, "Schritte")
            .AddUnmatched("class", "extra")
            .AddUnmatched("id", "steps"));

        var root = cut.Find("div");
        Assert.Equal("vs-vitals-card extra", root.ClassName);
        Assert.Equal("steps", root.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void Throws_when_the_heading_level_is_outside_2_to_6(int level)
    {
        Assert.Throws<InvalidOperationException>(() => RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, level)
            .Add(p => p.Value, 7500m)
            .Add(p => p.Unit, "Schritte")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_Title_is_blank(string title)
    {
        Assert.Throws<InvalidOperationException>(() => RenderCard(parameters => parameters
            .Add(p => p.Title, title)
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, 7500m)
            .Add(p => p.Unit, "Schritte")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_Unit_is_blank(string unit)
    {
        Assert.Throws<InvalidOperationException>(() => RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, 7500m)
            .Add(p => p.Unit, unit)));
    }

    [Fact]
    public void Throws_when_Decimals_is_negative()
    {
        Assert.Throws<InvalidOperationException>(() => RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, 7500m)
            .Add(p => p.Unit, "Schritte")
            .Add(p => p.Decimals, -1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Throws_when_the_Goal_is_not_above_zero(int goal)
    {
        Assert.Throws<InvalidOperationException>(() => RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, 7500m)
            .Add(p => p.Unit, "Schritte")
            .Add(p => p.Goal, goal)));
    }

    [Fact]
    public void Throws_when_a_Trend_is_given_without_a_Value()
    {
        Assert.Throws<InvalidOperationException>(() => RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, null)
            .Add(p => p.Unit, "Schritte")
            .Add(p => p.Trend, new VitalsTrend(VitalsTrendDirection.Up, "12% mehr als gestern"))));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_Href_is_blank(string href)
    {
        Assert.Throws<InvalidOperationException>(() => RenderCard(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Value, 7500m)
            .Add(p => p.Unit, "Schritte")
            .Add(p => p.Href, href)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_trend_requires_a_text(string text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new VitalsTrend(VitalsTrendDirection.Up, text));
    }

    private IRenderedComponent<VitalsCard> RenderCard(
        Action<ComponentParameterCollectionBuilder<VitalsCard>> parameterBuilder)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = SwissGerman;
        try
        {
            return Render(parameterBuilder);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
