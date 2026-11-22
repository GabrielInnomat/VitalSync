using Bunit;
using Microsoft.AspNetCore.Components;
using VitalSync.DesignSystem.Primitives;
using Food = VitalSync.DesignSystem.Tests.Primitives.TableTestHost.Food;

namespace VitalSync.DesignSystem.Tests.Primitives;

public sealed class TableTests : BunitContext
{
    private const string ModulePath = "./_content/VitalSync.DesignSystem/Primitives/Table.razor.js";

    private static readonly Food[] Foods =
    [
        new("Apfel", 52, "Obst"),
        new("Haferflocken", 372, "Getreide"),
    ];

    private readonly BunitJSModuleInterop _module;

    public TableTests()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _module = JSInterop.SetupModule(ModulePath);
        _module.SetupVoid("attach", _ => true).SetVoidResult();
        _module.SetupVoid("detach", _ => true).SetVoidResult();
    }

    [Fact]
    public void Shows_the_caption_inside_the_table_and_names_the_scroll_region_with_it()
    {
        var cut = RenderTable();

        var caption = cut.Find("table > caption");
        var region = cut.Find(".vs-table__scroller");
        Assert.Equal("Nährwerte", caption.TextContent.Trim());
        Assert.Equal("region", region.GetAttribute("role"));
        Assert.Equal(caption.Id, region.GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Every_column_title_is_a_column_header()
    {
        var cut = RenderTable();

        var headers = cut.FindAll("thead th");
        Assert.Equal(["Lebensmittel", "Energie", "Kategorie"], headers.Select(header => header.TextContent));
        Assert.All(headers, header => Assert.Equal("col", header.GetAttribute("scope")));
    }

    [Fact]
    public void The_first_column_is_the_row_header_by_default()
    {
        var cut = RenderTable();

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row =>
        {
            var cells = row.Children;
            Assert.Equal(["TH", "TD", "TD"], cells.Select(cell => cell.TagName));
            Assert.Equal("row", cells[0].GetAttribute("scope"));
        });
        Assert.Equal("Haferflocken", rows[1].Children[0].TextContent);
    }

    [Fact]
    public void RowHeader_moves_the_row_header_to_another_column()
    {
        var cut = RenderTable(categoryRowHeader: true);

        var cells = cut.Find("tbody tr").Children;
        Assert.Equal(["TD", "TD", "TH"], cells.Select(cell => cell.TagName));
        Assert.Equal("row", cells[2].GetAttribute("scope"));
    }

    [Fact]
    public void RowHeader_false_on_the_first_column_leaves_the_rows_without_a_header()
    {
        var cut = RenderTable(nameRowHeader: false);

        Assert.Empty(cut.FindAll("tbody th"));
    }

    [Fact]
    public void Throws_when_two_columns_are_the_row_header()
    {
        Assert.Throws<InvalidOperationException>(() => RenderTable(nameRowHeader: true, categoryRowHeader: true));
    }

    [Fact]
    public void A_numeric_column_aligns_its_header_and_its_cells_alike()
    {
        var cut = RenderTable();

        Assert.Equal(
            [false, true, false],
            cut.FindAll("thead th").Select(header => header.ClassList.Contains("vs-table__numeric")));
        Assert.Equal(
            [false, true, false],
            cut.Find("tbody tr").Children.Select(cell => cell.ClassList.Contains("vs-table__numeric")));
        Assert.Equal("372", cut.FindAll("tbody tr")[1].Children[1].TextContent);
    }

    [Fact]
    public void An_empty_table_keeps_caption_and_headers_and_says_so_in_one_row()
    {
        var cut = RenderTable(items: []);

        Assert.Equal("Nährwerte", cut.Find("caption").TextContent.Trim());
        Assert.Equal(3, cut.FindAll("thead th").Count);
        var cell = Assert.Single(cut.FindAll("tbody td"));
        Assert.Single(cut.FindAll("tbody tr"));
        Assert.Equal("3", cell.GetAttribute("colspan"));
        Assert.Equal("Keine Einträge vorhanden.", cell.TextContent);
    }

    [Fact]
    public void EmptyText_replaces_the_default_text_of_an_empty_table()
    {
        var cut = RenderTable(items: [], extra: parameters => parameters.Add(p => p.EmptyText, "Noch nichts gegessen."));

        Assert.Equal("Noch nichts gegessen.", cut.Find("tbody td").TextContent);
    }

    [Theory]
    [InlineData(" ", "Keine Einträge vorhanden.", "Lebensmittel")]
    [InlineData("Nährwerte", " ", "Lebensmittel")]
    [InlineData("Nährwerte", "Keine Einträge vorhanden.", " ")]
    public void Throws_on_a_blank_caption_empty_text_or_title(string caption, string emptyText, string title)
    {
        Assert.Throws<InvalidOperationException>(() => Render<Table<Food>>(parameters => parameters
            .Add(p => p.Caption, caption)
            .Add(p => p.Items, Foods)
            .Add(p => p.EmptyText, emptyText)
            .AddChildContent<TableColumn<Food>>(column => column
                .Add(p => p.Title, title)
                .Add(p => p.ChildContent, food => food.Name))));
    }

    [Fact]
    public void Throws_without_a_column()
    {
        Assert.Throws<InvalidOperationException>(() => Render<Table<Food>>(parameters => parameters
            .Add(p => p.Caption, "Nährwerte")
            .Add(p => p.Items, Foods)
            .AddChildContent("<p>keine Spalte</p>")));
    }

    [Fact]
    public void Throws_for_a_column_outside_a_table()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TableColumn<Food>>(parameters => parameters
            .Add(p => p.Title, "Lebensmittel")
            .Add(p => p.ChildContent, food => food.Name)));
    }

    [Fact]
    public void Before_the_page_is_interactive_the_region_is_always_a_tab_stop()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderTable();

        var region = cut.Find(".vs-table__scroller");
        Assert.Equal("region", region.GetAttribute("role"));
        Assert.Equal("0", region.GetAttribute("tabindex"));
        Assert.Empty(_module.Invocations);
    }

    [Fact]
    public async Task Once_interactive_the_region_is_a_tab_stop_only_while_it_scrolls()
    {
        var cut = RenderTable();
        Assert.Single(_module.Invocations["attach"]);

        await cut.InvokeAsync(() => cut.Instance.SetScrollable(false));

        var region = cut.Find(".vs-table__scroller");
        Assert.False(region.HasAttribute("role"));
        Assert.False(region.HasAttribute("tabindex"));
        Assert.False(region.HasAttribute("aria-labelledby"));

        await cut.InvokeAsync(() => cut.Instance.SetScrollable(true));

        region = cut.Find(".vs-table__scroller");
        Assert.Equal("region", region.GetAttribute("role"));
        Assert.Equal("0", region.GetAttribute("tabindex"));
        Assert.Equal(cut.Find("caption").Id, region.GetAttribute("aria-labelledby"));
    }

    [Fact]
    public async Task Detaches_the_scroll_watch_when_disposed()
    {
        RenderTable();

        await DisposeComponentsAsync();

        Assert.Single(_module.Invocations["detach"]);
    }

    [Fact]
    public void A_column_shown_later_takes_its_place_in_the_markup_order()
    {
        var cut = Render<TableTestHost>();

        cut.Render(parameters => parameters.Add(p => p.ShowEnergy, true));

        Assert.Equal(["Lebensmittel", "Energie", "Kategorie"], cut.FindAll("thead th").Select(header => header.TextContent));
        Assert.Equal(["Apfel", "52", "Obst"], cut.Find("tbody tr").Children.Select(cell => cell.TextContent));
    }

    [Fact]
    public void Cells_show_the_current_state_of_their_template()
    {
        var cut = Render<TableTestHost>();

        cut.Render(parameters => parameters.Add(p => p.Prefix, "Kategorie: "));

        Assert.Equal("Kategorie: Obst", cut.Find("tbody td").TextContent);
    }

    [Fact]
    public void Merges_an_extra_class_and_passes_other_attributes_on()
    {
        var cut = RenderTable(extra: parameters => parameters
            .AddUnmatched("class", "mahlzeiten")
            .AddUnmatched("data-test", "tabelle"));

        var table = cut.Find(".vs-table");
        Assert.Contains("mahlzeiten", table.ClassList);
        Assert.Equal("tabelle", table.GetAttribute("data-test"));
    }

    private IRenderedComponent<Table<Food>> RenderTable(
        IEnumerable<Food>? items = null,
        bool? nameRowHeader = null,
        bool? categoryRowHeader = null,
        Action<ComponentParameterCollectionBuilder<Table<Food>>>? extra = null) =>
        Render<Table<Food>>(parameters =>
        {
            parameters
                .Add(p => p.Caption, "Nährwerte")
                .Add(p => p.Items, items ?? Foods)
                .AddChildContent<TableColumn<Food>>(column => column
                    .Add(p => p.Title, "Lebensmittel")
                    .Add(p => p.RowHeader, nameRowHeader)
                    .Add(p => p.ChildContent, food => food.Name))
                .AddChildContent<TableColumn<Food>>(column => column
                    .Add(p => p.Title, "Energie")
                    .Add(p => p.Numeric, true)
                    .Add(p => p.ChildContent, food => food.Energy.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                .AddChildContent<TableColumn<Food>>(column => column
                    .Add(p => p.Title, "Kategorie")
                    .Add(p => p.RowHeader, categoryRowHeader)
                    .Add(p => p.ChildContent, food => food.Category));

            extra?.Invoke(parameters);
        });
}
