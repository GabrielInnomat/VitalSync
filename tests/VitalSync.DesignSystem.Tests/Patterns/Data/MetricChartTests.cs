using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using VitalSync.DesignSystem.Patterns.Data;

namespace VitalSync.DesignSystem.Tests.Patterns.Data;

public sealed class MetricChartTests : BunitContext
{
    private static readonly CultureInfo SwissGerman = new("de-CH");

    private static readonly DateOnly LastDay = new(2026, 11, 25);

    public MetricChartTests()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
    }

    [Fact]
    public void Shows_the_latest_value_and_summarises_bars_with_their_goal()
    {
        var cut = RenderChart(parameters => parameters
            .Add(p => p.Title, "  Schritte  ")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Kind, MetricChartKind.Bars)
            .Add(p => p.Unit, "  Schritte  ")
            .Add(p => p.Points, Days(8200m, 11000m, 6400m, 10500m, 9100m, 12300m, 7500m))
            .Add(p => p.Goal, 10000m));

        Assert.Equal("Schritte", cut.Find("h2.vs-metric-chart__title").TextContent);
        Assert.Equal("7’500", cut.Find(".vs-metric-chart__number").TextContent);
        Assert.Equal("Schritte", cut.Find(".vs-metric-chart__unit").TextContent);
        Assert.Equal("Neuester Wert: Mittwoch, 25.11.", cut.Find(".vs-metric-chart__latest .vs-metric-chart__detail").TextContent);
        Assert.Equal("true", cut.Find(".vs-metric-chart__latest").GetAttribute("aria-hidden"));
        Assert.Equal("Neuester Wert am Mittwoch, 25.11.: 7’500 Schritte.", cut.Find(".vs-metric-chart__latest-text").TextContent);
        Assert.Equal(
            "Vom 19.11. bis 25.11.: Durchschnitt 9’286 Schritte pro Tag, höchster Wert am Dienstag, 24.11.: 12’300 Schritte. "
            + "Ziel 10’000 Schritte an 3 von 7 Tagen erreicht.",
            cut.Find(".vs-metric-chart__summary").TextContent);
    }

    [Fact]
    public void Counts_the_days_with_data_when_bars_have_missing_days()
    {
        var cut = RenderChart(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Kind, MetricChartKind.Bars)
            .Add(p => p.Unit, "Schritte")
            .Add(p => p.Points, Days(8000m, null, 6000m, null, 10000m))
            .Add(p => p.Goal, 9000m));

        Assert.Equal(
            "Vom 21.11. bis 25.11., Daten an 3 von 5 Tagen: Durchschnitt 8’000 Schritte pro Tag, "
            + "höchster Wert am Mittwoch, 25.11.: 10’000 Schritte. Ziel 9’000 Schritte an 1 der 3 Tage mit Daten erreicht.",
            cut.Find(".vs-metric-chart__summary").TextContent);
    }

    [Fact]
    public void Summarises_a_line_by_its_lowest_and_highest_value_and_its_goal()
    {
        var cut = RenderChart(parameters => parameters
            .Add(p => p.Title, "Ruhepuls")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Kind, MetricChartKind.Line)
            .Add(p => p.Unit, "Schläge pro Minute")
            .Add(p => p.Points, Days(60m, null, 62m, 64m))
            .Add(p => p.Goal, 63m));

        Assert.Equal(
            "Vom 22.11. bis 25.11., Daten an 3 von 4 Tagen: tiefster Wert 60 Schläge pro Minute, "
            + "höchster Wert 64 Schläge pro Minute. Ziel: 63 Schläge pro Minute.",
            cut.Find(".vs-metric-chart__summary").TextContent);
    }

    [Fact]
    public void Hides_the_drawing_from_assistive_technology()
    {
        var cut = RenderChart(Chart(Days(8000m, 9000m)));

        var figure = cut.Find(".vs-metric-chart__figure");
        Assert.Equal("true", figure.GetAttribute("aria-hidden"));
        Assert.Equal("false", figure.QuerySelector("svg")!.GetAttribute("focusable"));
    }

    [Fact]
    public void Offers_the_same_data_as_a_table_behind_a_disclosure()
    {
        var cut = RenderChart(Chart(Days(8000m, null, 6000m, null, 10000m), goal: 9000m));

        var details = cut.Find("details.vs-metric-chart__details");
        Assert.Null(details.GetAttribute("open"));
        Assert.Equal("Als Tabelle anzeigen", details.QuerySelector(".vs-metric-chart__toggle-show")!.TextContent);
        Assert.Equal("Tabelle ausblenden", details.QuerySelector(".vs-metric-chart__toggle-hide")!.TextContent);
        Assert.Equal("Schritte, 21.11. bis 25.11., Ziel: 9’000 Schritte", cut.Find("caption").TextContent.Trim());
        Assert.Equal(["Tag", "Schritte", "Ziel"], cut.FindAll("thead th").Select(cell => cell.TextContent.Trim()));
        Assert.Equal(
            [
                ["Samstag, 21.11.", "8’000", "nicht erreicht"],
                ["Sonntag, 22.11.", "Keine Daten", "Keine Daten"],
                ["Montag, 23.11.", "6’000", "nicht erreicht"],
                ["Dienstag, 24.11.", "Keine Daten", "Keine Daten"],
                ["Mittwoch, 25.11.", "10’000", "erreicht"],
            ],
            cut.FindAll("tbody tr").Select(row => row.QuerySelectorAll("th, td").Select(cell => cell.TextContent.Trim()).ToArray()));
    }

    [Fact]
    public void Leaves_the_goal_column_out_of_the_table_for_a_line()
    {
        var cut = RenderChart(parameters => parameters
            .Add(p => p.Title, "Gewicht")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Kind, MetricChartKind.Line)
            .Add(p => p.Unit, "kg")
            .Add(p => p.Decimals, 1)
            .Add(p => p.Points, Days(72.4m, 71.9m))
            .Add(p => p.Goal, 70m));

        Assert.Equal(["Tag", "kg"], cut.FindAll("thead th").Select(cell => cell.TextContent.Trim()));
        Assert.Equal("72.4", cut.FindAll("tbody tr")[0].QuerySelectorAll("td")[0].TextContent.Trim());
    }

    [Fact]
    public void Starts_bars_at_zero_and_labels_round_ticks_in_Swiss_format()
    {
        var cut = RenderChart(Chart(Days(8200m, 11000m, 6400m, 10500m, 9100m, 12300m, 7500m)));

        Assert.Equal(
            ["0", "5’000", "10’000", "15’000"],
            cut.FindAll(".vs-metric-chart__y-label").Select(label => label.TextContent));
        Assert.Single(cut.FindAll(".vs-metric-chart__baseline"));
        Assert.Equal("100", cut.Find(".vs-metric-chart__baseline").GetAttribute("y1"));
    }

    [Fact]
    public void Draws_a_bar_per_day_with_data_and_a_marked_stub_per_missing_day_but_never_a_zero_bar()
    {
        var cut = RenderChart(Chart(Days(5000m, null, 0m, 10000m)));

        var bars = cut.FindAll("rect.vs-metric-chart__bar");
        Assert.Equal(["2", "32"], bars.Select(bar => bar.GetAttribute("x")));
        var missing = Assert.Single(cut.FindAll("rect.vs-metric-chart__missing"));
        Assert.Equal("12", missing.GetAttribute("x"));
        Assert.Equal(["Tag ohne Daten"], cut.FindAll(".vs-metric-chart__legend-item").Select(item => item.TextContent.Trim()));
    }

    [Fact]
    public void Breaks_the_line_at_a_missing_day_and_marks_every_value_with_a_dot()
    {
        var cut = RenderChart(parameters => parameters
            .Add(p => p.Title, "Ruhepuls")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Kind, MetricChartKind.Line)
            .Add(p => p.Unit, "Schläge pro Minute")
            .Add(p => p.Points, Days(60m, null, 62m, 64m)));

        Assert.Matches(@"^M5 [\d.]+ M25 [\d.]+ L35 [\d.]+$", cut.Find(".vs-metric-chart__line").GetAttribute("d"));
        Assert.Matches(@"^M5 [\d.]+ h0 M25 [\d.]+ h0 M35 [\d.]+ h0$", cut.Find(".vs-metric-chart__dots").GetAttribute("d"));
        Assert.Empty(cut.FindAll("rect"));
        Assert.Equal(
            ["Lücke in der Linie: Tag ohne Daten"],
            cut.FindAll(".vs-metric-chart__legend-item").Select(item => item.TextContent.Trim()));
    }

    [Fact]
    public void Draws_the_goal_line_and_names_it_in_the_legend()
    {
        var cut = RenderChart(Chart(Days(8000m, 12000m), goal: 10000m));

        var goal = cut.Find("line.vs-metric-chart__goal");
        Assert.Equal(goal.GetAttribute("y1"), goal.GetAttribute("y2"));
        Assert.Equal(["Ziel: 10’000 Schritte"], cut.FindAll(".vs-metric-chart__legend-item").Select(item => item.TextContent.Trim()));
    }

    [Fact]
    public void Leaves_the_legend_out_when_there_is_neither_a_goal_nor_a_missing_day()
    {
        var cut = RenderChart(Chart(Days(8000m, 12000m)));

        Assert.Empty(cut.FindAll(".vs-metric-chart__legend"));
        Assert.Empty(cut.FindAll(".vs-metric-chart__goal"));
    }

    [Fact]
    public void Labels_every_day_of_a_week_with_its_weekday()
    {
        var cut = RenderChart(Chart(Days(1m, 2m, 3m, 4m, 5m, 6m, 7m)));

        Assert.Equal(
            ["Do", "Fr", "Sa", "So", "Mo", "Di", "Mi"],
            cut.FindAll(".vs-metric-chart__x-label").Select(label => label.TextContent));
    }

    [Fact]
    public void Labels_a_month_with_fewer_dates_the_narrower_the_chart_counting_back_from_the_last_day()
    {
        var cut = RenderChart(Chart(Days(Enumerable.Range(1, 30).Select(value => (decimal?)value).ToArray())));

        Assert.Equal(["5.11.", "15.11.", "25.11."], LabelTexts(cut, "narrow"));
        Assert.Equal(["1.11.", "7.11.", "13.11.", "19.11.", "25.11."], LabelTexts(cut, "medium"));
        Assert.Equal(["28.10.", "1.11.", "5.11.", "9.11.", "13.11.", "17.11.", "21.11.", "25.11."], LabelTexts(cut, "wide"));
        Assert.Equal("right: 0; transform: none", cut.FindAll(".vs-metric-chart__x-label")[^1].GetAttribute("style"));
    }

    [Fact]
    public void Says_that_there_is_too_little_data_below_two_values_but_keeps_the_link()
    {
        var cut = RenderChart(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Kind, MetricChartKind.Bars)
            .Add(p => p.Unit, "Schritte")
            .Add(p => p.Points, Days(null, 6800m, null))
            .Add(p => p.Href, "fitness")
            .Add(p => p.LinkText, "Alle Aktivitäten"));

        Assert.Equal(
            "Noch zu wenig Daten. Das Diagramm erscheint ab zwei Tagen mit Daten.",
            cut.Find(".vs-metric-chart__detail").TextContent);
        Assert.Empty(cut.FindAll(".vs-metric-chart__figure"));
        Assert.Empty(cut.FindAll("details"));
        Assert.Equal("Alle Aktivitäten", cut.Find("a.vs-metric-chart__link").TextContent);
    }

    [Fact]
    public void Ends_with_a_link_with_the_trimmed_href_and_text()
    {
        var cut = RenderChart(parameters => parameters
            .Add(p => p.Title, "Schritte")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Kind, MetricChartKind.Bars)
            .Add(p => p.Unit, "Schritte")
            .Add(p => p.Points, Days(8000m, 9000m))
            .Add(p => p.Href, "  fitness  ")
            .Add(p => p.LinkText, "  Alle Aktivitäten  "));

        var link = cut.Find(".vs-metric-chart > a:last-child");
        Assert.Equal("fitness", link.GetAttribute("href"));
        Assert.Equal("Alle Aktivitäten", link.TextContent);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Renders_the_title_at_the_given_heading_level(int level)
    {
        var cut = RenderChart(Chart(Days(8000m, 9000m), headingLevel: level));

        Assert.Equal($"H{level}", cut.Find(".vs-metric-chart__title").TagName);
    }

    [Fact]
    public void Carries_the_kind_and_the_category_as_classes_and_merges_an_additional_class()
    {
        var cut = RenderChart(parameters => parameters
            .Add(p => p.Title, "Schlaf")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Kind, MetricChartKind.Line)
            .Add(p => p.Unit, "Stunden")
            .Add(p => p.Points, Days(7m, 8m))
            .Add(p => p.Category, VitalsCategory.Sleep)
            .AddUnmatched("class", "extra"));

        var root = cut.Find(".vs-metric-chart");
        Assert.True(root.ClassList.Contains("vs-metric-chart--line"));
        Assert.True(root.ClassList.Contains("vs-metric-chart--sleep"));
        Assert.True(root.ClassList.Contains("extra"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_Title_is_blank(string title)
    {
        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(Days(1m, 2m), title: title)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public void Throws_when_the_HeadingLevel_is_outside_2_to_6(int level)
    {
        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(Days(1m, 2m), headingLevel: level)));
    }

    [Fact]
    public void Throws_when_the_Kind_is_undefined()
    {
        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(Days(1m, 2m), kind: (MetricChartKind)99)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_Unit_is_blank(string unit)
    {
        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(Days(1m, 2m), unit: unit)));
    }

    [Fact]
    public void Throws_when_the_Points_are_missing()
    {
        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(null!)));
    }

    [Fact]
    public void Throws_when_the_Decimals_are_negative()
    {
        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(Days(1m, 2m), decimals: -1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Throws_when_the_Goal_is_not_above_zero(int goal)
    {
        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(Days(1m, 2m), goal: goal)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_Href_is_blank(string href)
    {
        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(Days(1m, 2m), href: href, linkText: "Alle Aktivitäten")));
    }

    [Fact]
    public void Throws_when_an_Href_is_given_without_a_LinkText()
    {
        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(Days(1m, 2m), href: "fitness")));
    }

    [Fact]
    public void Throws_when_a_LinkText_is_given_without_an_Href()
    {
        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(Days(1m, 2m), linkText: "Alle Aktivitäten")));
    }

    [Fact]
    public void Throws_when_the_days_are_not_consecutive()
    {
        IReadOnlyList<MetricChartPoint> points =
        [
            new(LastDay.AddDays(-2), 1m),
            new(LastDay, 2m),
        ];

        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(points)));
    }

    [Fact]
    public void Throws_when_a_bar_is_negative()
    {
        Assert.Throws<InvalidOperationException>(() => RenderChart(Chart(Days(1m, -2m))));
    }

    private static List<MetricChartPoint> Days(params decimal?[] values) =>
        values.Select((value, index) => new MetricChartPoint(LastDay.AddDays(index - values.Length + 1), value)).ToList();

    private static Action<ComponentParameterCollectionBuilder<MetricChart>> Chart(
        IReadOnlyList<MetricChartPoint> points,
        string title = "Schritte",
        int headingLevel = 2,
        MetricChartKind kind = MetricChartKind.Bars,
        string unit = "Schritte",
        int decimals = 0,
        decimal? goal = null,
        string? href = null,
        string? linkText = null) =>
        parameters => parameters
            .Add(p => p.Title, title)
            .Add(p => p.HeadingLevel, headingLevel)
            .Add(p => p.Kind, kind)
            .Add(p => p.Unit, unit)
            .Add(p => p.Points, points)
            .Add(p => p.Decimals, decimals)
            .Add(p => p.Goal, goal)
            .Add(p => p.Href, href)
            .Add(p => p.LinkText, linkText);

    private static IEnumerable<string> LabelTexts(IRenderedComponent<MetricChart> cut, string level) =>
        cut.FindAll($".vs-metric-chart__x-label--{level}").Select(label => label.TextContent);

    private IRenderedComponent<MetricChart> RenderChart(
        Action<ComponentParameterCollectionBuilder<MetricChart>> parameterBuilder)
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
