using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Accessibility;
using VitalSync.DesignSystem.Patterns.Data;
using Food = VitalSync.DesignSystem.Tests.Patterns.Data.DataTableTestHost.Food;

namespace VitalSync.DesignSystem.Tests.Patterns.Data;

public sealed class DataTableTests : BunitContext
{
    private const string ModulePath = "./_content/VitalSync.DesignSystem/Patterns/Data/DataTable.razor.js";
    private const string TableModulePath = "./_content/VitalSync.DesignSystem/Patterns/Data/Table.razor.js";
    private const string Focus = "Blazor._internal.domWrapper.focus";

    private static readonly Food[] Foods =
    [
        new("Apfel", 52, new DateOnly(2026, 1, 10)),
        new("Banane", 89, new DateOnly(2026, 2, 5)),
        new("Haferflocken", 372, new DateOnly(2026, 3, 1)),
        new("Ananas", 50, new DateOnly(2026, 3, 20)),
    ];

    private readonly BunitJSModuleInterop _module;

    public DataTableTests()
    {
        Services.AddScoped<LiveAnnouncer>();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _module = JSInterop.SetupModule(ModulePath);
        _module.SetupVoid("attach", _ => true).SetVoidResult();
        _module.SetupVoid("replaceUrl", _ => true).SetVoidResult();
        _module.SetupVoid("detach", _ => true).SetVoidResult();
        var table = JSInterop.SetupModule(TableModulePath);
        table.SetupVoid("attach", _ => true).SetVoidResult();
        table.SetupVoid("detach", _ => true).SetVoidResult();
        JSInterop.SetupVoid(Focus, _ => true).SetVoidResult();
    }

    private LiveAnnouncer Announcer => Services.GetRequiredService<LiveAnnouncer>();

    [Theory]
    [InlineData("100", 100, true, 100, true)]
    [InlineData("=7", 7, true, 7, true)]
    [InlineData(" -5 ", -5, true, -5, true)]
    [InlineData("10’000.5", 10000.5, true, 10000.5, true)]
    [InlineData("1'000", 1000, true, 1000, true)]
    [InlineData("10-20", 10, true, 20, true)]
    [InlineData("20 – 10", 10, true, 20, true)]
    [InlineData("-5--2", -5, true, -2, true)]
    public void Parses_a_number_or_a_range_into_closed_bounds(string text, double from, bool fromInclusive, double to, bool toInclusive)
    {
        var range = DataTableFilterParser.ParseNumber(text);

        Assert.Equal(
            new DataTableRange<decimal>(new((decimal)from, fromInclusive), new((decimal)to, toInclusive)),
            range);
    }

    [Theory]
    [InlineData("<50", false, true)]
    [InlineData("<=50", true, true)]
    [InlineData("≤ 50", true, true)]
    [InlineData(">50", false, false)]
    [InlineData(">=50", true, false)]
    [InlineData("≥50", true, false)]
    public void Parses_a_comparison_into_an_open_range(string text, bool inclusive, bool upper)
    {
        var range = DataTableFilterParser.ParseNumber(text);

        var bound = new DataTableBound<decimal>(50m, inclusive);
        Assert.Equal(upper ? new DataTableRange<decimal>(null, bound) : new DataTableRange<decimal>(bound, null), range);
    }

    [Theory]
    [InlineData("viel")]
    [InlineData("<")]
    [InlineData("10-")]
    [InlineData("1,5")]
    [InlineData("1.2.3")]
    [InlineData("<>5")]
    public void Rejects_what_is_not_a_number(string text)
    {
        Assert.Null(DataTableFilterParser.ParseNumber(text));
    }

    [Fact]
    public void Parses_dates_comparisons_and_date_ranges_in_swiss_notation()
    {
        var first = new DateOnly(2026, 11, 1);
        var last = new DateOnly(2026, 11, 30);

        Assert.Equal(new DataTableRange<DateOnly>(new(last, true), new(last, true)), DataTableFilterParser.ParseDate("30.11.2026"));
        Assert.Equal(new DataTableRange<DateOnly>(null, new(first, false)), DataTableFilterParser.ParseDate("<1.11.2026"));
        Assert.Equal(new DataTableRange<DateOnly>(new(first, true), new(last, true)), DataTableFilterParser.ParseDate("30.11.2026 - 1.11.2026"));
    }

    [Theory]
    [InlineData("2026-11-25")]
    [InlineData("31.2.2026")]
    [InlineData("25.11.26")]
    public void Rejects_what_is_not_a_date(string text)
    {
        Assert.Null(DataTableFilterParser.ParseDate(text));
    }

    [Theory]
    [InlineData(5, true, 5, true, 5, true)]
    [InlineData(5, false, 5, true, 5, false)]
    [InlineData(5, true, 6, false, 6, false)]
    public void A_range_contains_its_bounds_only_when_they_are_inclusive(
        int from, bool fromInclusive, int to, bool toInclusive, int value, bool expected)
    {
        var range = new DataTableRange<int>(new(from, fromInclusive), new(to, toInclusive));

        Assert.Equal(expected, range.Contains(value));
    }

    [Fact]
    public void Offers_a_labelled_filter_for_every_filterable_column_inside_a_named_group()
    {
        var cut = RenderTable();

        Assert.Equal("Lebensmittel filtern", cut.Find("fieldset > legend").TextContent);
        Assert.Equal(
            ["Name", "Energie", "Erfasst"],
            cut.FindAll(".vs-data-table__filter label").Select(label => label.TextContent.Trim()));
        Assert.Empty(cut.FindAll(".vs-data-table__reset"));
    }

    [Fact]
    public void Explains_the_number_and_date_syntax_with_a_toggletip_but_not_the_text_syntax()
    {
        var cut = RenderTable();

        var filters = cut.FindAll(".vs-data-table__filter");
        Assert.Empty(filters[0].QuerySelectorAll(".vs-toggletip"));
        Assert.Contains("<50", filters[1].QuerySelector(".vs-toggletip__popup")!.TextContent, StringComparison.Ordinal);
        Assert.Contains("1.1.2026", filters[2].QuerySelector(".vs-toggletip__popup")!.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void A_text_filter_matches_any_part_of_the_text_regardless_of_case()
    {
        var cut = RenderTable();

        Type(cut, 0, "AN");
        FilterInput(cut, 0).KeyDown(Key.Enter);

        Assert.Equal(["Banane", "Ananas"], RowNames(cut));
    }

    [Fact]
    public void Filters_combine_so_a_row_must_match_all_of_them()
    {
        var cut = RenderTable();

        Type(cut, 0, "an");
        Type(cut, 1, ">60");
        FilterInput(cut, 1).KeyDown(Key.Enter);

        Assert.Equal(["Banane"], RowNames(cut));
    }

    [Fact]
    public void Number_and_date_filters_compare_with_operators_and_ranges()
    {
        var cut = RenderTable();

        Type(cut, 1, "50-60");
        Type(cut, 2, ">=1.3.2026");
        FilterInput(cut, 2).KeyDown(Key.Enter);

        Assert.Equal(["Ananas"], RowNames(cut));
    }

    [Fact]
    public void Leaves_the_rows_alone_while_typing_and_applies_the_filter_after_the_pause()
    {
        var cut = RenderTable();
        cut.FindComponent<DataTable<Food>>().Instance.FilterDelay = TimeSpan.FromMilliseconds(50);

        Type(cut, 0, "hafer");

        Assert.Equal(4, RowNames(cut).Length);
        cut.WaitForAssertion(() => Assert.Equal(["Haferflocken"], RowNames(cut)));
    }

    [Fact]
    public void Keeps_waiting_while_the_pause_has_not_passed()
    {
        var cut = RenderTable();

        Type(cut, 0, "hafer");
        Thread.Sleep(200);

        Assert.Equal(4, RowNames(cut).Length);
    }

    [Fact]
    public void Leaving_the_filter_applies_it_at_once()
    {
        var cut = RenderTable();

        Type(cut, 0, "apf");
        FilterInput(cut, 0).FocusOut();

        Assert.Equal(["Apfel"], RowNames(cut));
    }

    [Fact]
    public void An_invalid_entry_shows_its_error_and_suspends_only_its_own_filter()
    {
        var cut = RenderTable();

        Type(cut, 0, "an");
        Type(cut, 1, "viel");
        FilterInput(cut, 1).Change("viel");
        FilterInput(cut, 1).FocusOut();

        Assert.Equal(["Banane", "Ananas"], RowNames(cut));
        Assert.Equal("true", FilterInput(cut, 1).GetAttribute("aria-invalid"));
        Assert.Contains(
            "Bitte gib bei Energie eine Zahl ein, etwa 100, <50 oder 10-20.",
            cut.Find(".vs-input__error").TextContent,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Announces_the_number_of_matches_only_when_it_changed()
    {
        var cut = RenderTable();

        Type(cut, 0, "an");
        FilterInput(cut, 0).KeyDown(Key.Enter);
        Type(cut, 0, "ana");
        FilterInput(cut, 0).KeyDown(Key.Enter);
        Type(cut, 0, "anan");
        FilterInput(cut, 0).KeyDown(Key.Enter);

        Assert.Equal(["2 Treffer"], Announcer.Announcements.Select(Text));
    }

    [Fact]
    public void Announces_when_nothing_matches_and_offers_to_reset_the_filters_in_the_empty_row()
    {
        var cut = RenderTable();

        Type(cut, 0, "kiwi");
        FilterInput(cut, 0).KeyDown(Key.Enter);

        Assert.Equal(["Keine Treffer"], Announcer.Announcements.Select(Text));
        var empty = cut.Find("tbody .vs-data-table__no-results");
        Assert.StartsWith("Keine Treffer.", empty.TextContent.Trim(), StringComparison.Ordinal);
        Assert.Equal("Alle Filter zurücksetzen", empty.QuerySelector("a")!.TextContent);
    }

    [Fact]
    public async Task Resetting_clears_every_filter_and_moves_focus_to_the_caption()
    {
        var cut = RenderTable();
        Type(cut, 0, "kiwi");
        await FilterInput(cut, 0).KeyDownAsync(Key.Enter);

        await cut.FindComponent<DataTable<Food>>().Instance.ResetFilters();

        Assert.Equal(4, RowNames(cut).Length);
        Assert.All(cut.FindAll(".vs-data-table__filter input"), input => Assert.Equal("", input.GetAttribute("value")));
        Assert.Empty(cut.FindAll(".vs-data-table__reset"));
        AssertCaptionFocused(cut);
    }

    [Fact]
    public void Shows_the_empty_text_when_there_are_no_rows_at_all()
    {
        var cut = RenderTable(items: []);

        Assert.Equal("Keine Einträge vorhanden.", cut.Find("tbody td").TextContent.Trim());
    }

    [Fact]
    public void Only_sortable_columns_get_a_sort_button_and_none_is_sorted_at_first()
    {
        var cut = RenderTable();

        var headers = cut.FindAll("thead th");
        Assert.Equal([true, true, false, false], headers.Select(header => header.QuerySelector("button") is not null));
        Assert.All(headers, header => Assert.Null(header.GetAttribute("aria-sort")));
        Assert.Equal(["Apfel", "Banane", "Haferflocken", "Ananas"], RowNames(cut));
    }

    [Fact]
    public void Sorting_goes_ascending_then_descending_and_leaves_the_direction_to_aria_sort_alone()
    {
        var cut = RenderTable();

        SortButton(cut, 0).Click();

        Assert.Equal("ascending", cut.FindAll("thead th")[0].GetAttribute("aria-sort"));
        Assert.Contains("aufsteigend", SortState(cut).TextContent, StringComparison.Ordinal);
        Assert.Equal("true", SortState(cut).GetAttribute("aria-hidden"));
        Assert.Equal(["Ananas", "Apfel", "Banane", "Haferflocken"], RowNames(cut));

        SortButton(cut, 0).Click();

        Assert.Equal("descending", cut.FindAll("thead th")[0].GetAttribute("aria-sort"));
        Assert.Contains("absteigend", SortState(cut).TextContent, StringComparison.Ordinal);
        Assert.Equal("true", SortState(cut).GetAttribute("aria-hidden"));
        Assert.Equal(["Haferflocken", "Banane", "Apfel", "Ananas"], RowNames(cut));
        Assert.Empty(Announcer.Announcements);
    }

    [Fact]
    public void Sorting_another_column_starts_it_ascending_and_unsorts_the_previous_one()
    {
        var cut = RenderTable();

        SortButton(cut, 0).Click();
        SortButton(cut, 0).Click();
        SortButton(cut, 1).Click();

        var headers = cut.FindAll("thead th");
        Assert.Null(headers[0].GetAttribute("aria-sort"));
        Assert.Equal("ascending", headers[1].GetAttribute("aria-sort"));
        Assert.Equal(["Ananas", "Apfel", "Banane", "Haferflocken"], RowNames(cut));
    }

    [Fact]
    public void Shows_one_page_without_navigation_when_everything_fits()
    {
        var cut = RenderTable();

        Assert.Empty(cut.FindAll("nav"));
        Assert.Equal("Lebensmittel", cut.Find("caption").TextContent.Trim());
    }

    [Fact]
    public void Splits_the_rows_into_pages_of_twenty_five_and_links_every_page()
    {
        var cut = RenderTable(items: Many(60));

        Assert.Equal(25, RowNames(cut).Length);
        Assert.Equal("Lebensmittel: Seiten", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal(["Seite 1", "Seite 2", "Seite 3"], PageLinks(cut).Select(link => link.TextContent.Trim()));
        Assert.Equal(["page", null, null], PageLinks(cut).Select(link => link.GetAttribute("aria-current")));
        Assert.Equal("http://localhost/?essen.page=2", PageLinks(cut)[1].GetAttribute("href"));
        Assert.Equal(["Nächste"], StepLinks(cut));
    }

    [Fact]
    public void Collapses_distant_pages_into_a_gap_hidden_from_screen_readers()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("?essen.page=5");

        var cut = RenderTable(items: Many(250));

        Assert.Equal(["1", "…", "4", "5", "6", "…", "10"], PageItems(cut));
        Assert.All(cut.FindAll(".vs-data-table__gap"), gap => Assert.Equal("true", gap.GetAttribute("aria-hidden")));
        Assert.Equal(["Vorherige", "Nächste"], StepLinks(cut));
        Assert.Equal("Lebensmittel 101", RowNames(cut)[0]);
    }

    [Fact]
    public void Shows_the_page_itself_instead_of_a_gap_of_one_page()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("?essen.page=4");

        var cut = RenderTable(items: Many(200));

        Assert.Equal(["1", "2", "3", "4", "5", "…", "8"], PageItems(cut));
    }

    [Fact]
    public async Task Going_to_a_page_clamps_it_and_moves_focus_to_the_caption_that_names_the_page()
    {
        var cut = RenderTable(items: Many(60));

        Assert.Equal("Lebensmittel, Seite 1 von 3", cut.Find("caption").TextContent.Trim());

        await cut.FindComponent<DataTable<Food>>().Instance.GoToPage(99);

        Assert.Equal("Lebensmittel, Seite 3 von 3", cut.Find("caption").TextContent.Trim());
        Assert.Equal("Seite 3", cut.Find("[aria-current=page]").TextContent.Trim());
        Assert.Equal(10, RowNames(cut).Length);
        Assert.Equal(["Vorherige"], StepLinks(cut));
        AssertCaptionFocused(cut);
    }

    [Fact]
    public async Task Going_to_a_page_focuses_a_new_caption_element_so_a_screen_reader_cannot_read_a_stale_page()
    {
        var cut = RenderTable(items: Many(60));
        var before = cut.Find("caption").GetAttribute("blazor:elementReference");

        await cut.FindComponent<DataTable<Food>>().Instance.GoToPage(2);

        var after = cut.Find("caption").GetAttribute("blazor:elementReference");
        Assert.NotEqual(before, after);
        cut.WaitForAssertion(() => Assert.Equal(
            after,
            Assert.IsType<ElementReference>(Assert.Single(JSInterop.Invocations[Focus]).Arguments[0]).Id));
    }

    [Fact]
    public void Filtering_returns_to_the_first_page()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("?essen.page=2");
        var cut = RenderTable(items: Many(60));

        Type(cut, 0, "Lebensmittel");
        FilterInput(cut, 0).KeyDown(Key.Enter);

        Assert.Equal("Seite 1", cut.Find("[aria-current=page]").TextContent.Trim());
    }

    [Fact]
    public void Restores_sorting_filters_and_page_from_the_address()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "?essen.sort=energie&essen.order=desc&essen.filter.name=mittel&essen.page=2&andere=1");

        var cut = RenderTable(items: Many(60));

        Assert.Equal("mittel", FilterInput(cut, 0).GetAttribute("value"));
        Assert.Equal("descending", cut.FindAll("thead th")[1].GetAttribute("aria-sort"));
        Assert.Equal("Lebensmittel 35", RowNames(cut)[0]);
        Assert.Equal(
            "http://localhost/?essen.sort=energie&essen.order=desc&andere=1",
            cut.Find(".vs-data-table__reset").GetAttribute("href"));
    }

    [Fact]
    public void Ignores_a_sort_key_in_the_address_that_no_sortable_column_has()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("?essen.sort=erfasst");

        var cut = RenderTable();

        Assert.All(cut.FindAll("thead th"), header => Assert.Null(header.GetAttribute("aria-sort")));
    }

    [Fact]
    public void Writes_the_applied_state_back_into_the_address()
    {
        var cut = RenderTable();

        Type(cut, 0, "an");
        FilterInput(cut, 0).KeyDown(Key.Enter);
        SortButton(cut, 1).Click();

        cut.WaitForAssertion(() =>
        {
            var entries = (IDictionary<string, string>)_module.Invocations["replaceUrl"][^1].Arguments[1]!;
            Assert.Equal("energie", entries["essen.sort"]);
            Assert.Equal("an", entries["essen.filter.name"]);
            Assert.False(entries.ContainsKey("essen.page"));
        });
    }

    [Fact]
    public void Asks_the_provider_for_the_filtered_sorted_page_and_shows_a_spinner_meanwhile()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "?essen.sort=energie&essen.order=desc&essen.filter.name=an&essen.filter.energie=<100&essen.page=2");
        var requests = new List<DataTableRequest>();
        var pending = new TaskCompletionSource<DataTableResult<Food>>(TaskCreationOptions.RunContinuationsAsynchronously);

        var cut = Render<DataTableTestHost>(parameters => parameters
            .Add(p => p.ItemsProvider, (request, _) =>
            {
                requests.Add(request);
                return new ValueTask<DataTableResult<Food>>(pending.Task);
            }));

        cut.WaitForAssertion(() => Assert.Single(requests));
        var request = requests[0];
        Assert.Equal(new DataTableSort("energie", Descending: true), request.Sort);
        Assert.Equal((2, 25, 25), (request.Page, request.PageSize, request.Skip));
        Assert.Equal<DataTableFilter>(
            [new DataTableTextFilter("name", "an"), new DataTableNumberFilter("energie", new(null, new(100m, false)))],
            request.Filters);
        Assert.NotNull(cut.Find("tbody .vs-spinner"));

        pending.SetResult(new DataTableResult<Food>([.. Foods.Take(2)], 30));

        cut.WaitForAssertion(() => Assert.Equal(["Apfel", "Banane"], RowNames(cut)));
        Assert.Empty(cut.FindAll(".vs-spinner"));
        Assert.Equal("Seite 2", cut.Find("[aria-current=page]").TextContent.Trim());
    }

    [Fact]
    public void Asks_the_provider_again_when_sorting_changes()
    {
        var requests = new List<DataTableRequest>();

        var cut = Render<DataTableTestHost>(parameters => parameters
            .Add(p => p.ItemsProvider, (request, _) =>
            {
                requests.Add(request);
                return ValueTask.FromResult(new DataTableResult<Food>(Foods, Foods.Length));
            }));
        cut.WaitForAssertion(() => Assert.Equal(4, RowNames(cut).Length));

        SortButton(cut, 0).Click();

        cut.WaitForAssertion(() => Assert.Equal(new DataTableSort("name", Descending: false), requests[^1].Sort));
    }

    [Fact]
    public void Throws_without_a_source_of_rows()
    {
        Assert.Throws<InvalidOperationException>(() => Render<DataTableTestHost>());
    }

    [Fact]
    public void Throws_with_both_items_and_a_provider()
    {
        Assert.Throws<InvalidOperationException>(() => Render<DataTableTestHost>(parameters => parameters
            .Add(p => p.Items, Foods)
            .Add(p => p.ItemsProvider, (_, _) => ValueTask.FromResult(new DataTableResult<Food>([], 0)))));
    }

    [Fact]
    public void Throws_when_a_sortable_column_has_no_key()
    {
        Assert.Throws<InvalidOperationException>(() => Render<DataTable<Food>>(parameters => parameters
            .Add(p => p.Caption, "Lebensmittel")
            .Add(p => p.Items, Foods)
            .AddChildContent<DataTableColumn<Food, int>>(column => column
                .Add(p => p.Title, "Energie")
                .Add(p => p.Value, food => food.Energy)
                .Add(p => p.Sortable, true))));
    }

    [Fact]
    public void Throws_when_two_columns_share_a_key()
    {
        Assert.Throws<InvalidOperationException>(() => Render<DataTable<Food>>(parameters => parameters
            .Add(p => p.Caption, "Lebensmittel")
            .Add(p => p.Items, Foods)
            .AddChildContent<DataTableColumn<Food, int>>(column => column
                .Add(p => p.Title, "Energie")
                .Add(p => p.Value, food => food.Energy)
                .Add(p => p.Key, "wert")
                .Add(p => p.Sortable, true))
            .AddChildContent<DataTableColumn<Food, string>>(column => column
                .Add(p => p.Title, "Name")
                .Add(p => p.Value, food => food.Name)
                .Add(p => p.Key, "wert")
                .Add(p => p.Sortable, true))));
    }

    [Fact]
    public void Throws_when_a_column_that_cannot_be_filtered_is_filterable()
    {
        Assert.Throws<InvalidOperationException>(() => Render<DataTable<Food>>(parameters => parameters
            .Add(p => p.Caption, "Lebensmittel")
            .Add(p => p.Items, Foods)
            .AddChildContent<DataTableColumn<Food, bool>>(column => column
                .Add(p => p.Title, "Bekannt")
                .Add(p => p.Value, food => food.Energy > 0)
                .Add(p => p.Key, "bekannt")
                .Add(p => p.Filterable, true))));
    }

    private static Food[] Many(int count) =>
        [.. Enumerable.Range(1, count).Select(i => new Food($"Lebensmittel {i:00}", i, new DateOnly(2026, 1, 1).AddDays(i)))];

    private static string[] RowNames(IRenderedComponent<DataTableTestHost> cut) =>
        [.. cut.FindAll("tbody tr > th").Select(cell => cell.TextContent)];

    private static IElement FilterInput(IRenderedComponent<DataTableTestHost> cut, int index) =>
        cut.FindAll(".vs-data-table__filter input")[index];

    private static void Type(IRenderedComponent<DataTableTestHost> cut, int index, string text) =>
        FilterInput(cut, index).Input(text);

    private static IElement SortButton(IRenderedComponent<DataTableTestHost> cut, int index) =>
        cut.FindAll("thead th")[index].QuerySelector("button")!;

    private static IElement SortState(IRenderedComponent<DataTableTestHost> cut) =>
        cut.Find(".vs-data-table__sort-state");

    private static IReadOnlyList<IElement> PageLinks(IRenderedComponent<DataTableTestHost> cut) =>
        cut.FindAll(".vs-data-table__page:not(.vs-data-table__step)");

    private static string[] PageItems(IRenderedComponent<DataTableTestHost> cut) =>
        [.. cut.FindAll(".vs-data-table__pages > li")
            .Where(item => item.QuerySelector(".vs-data-table__step") is null)
            .Select(item => (item.QuerySelector("a")?.LastChild ?? item).TextContent.Trim())];

    private static string[] StepLinks(IRenderedComponent<DataTableTestHost> cut) =>
        [.. cut.FindAll(".vs-data-table__step").Select(link => link.TextContent.Trim())];

    private void AssertCaptionFocused(IRenderedComponent<DataTableTestHost> cut)
    {
        Assert.Equal("-1", cut.Find("caption").GetAttribute("tabindex"));
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Focus]));
    }

    private IRenderedComponent<DataTableTestHost> RenderTable(IEnumerable<Food>? items = null) =>
        Render<DataTableTestHost>(parameters => parameters.Add(p => p.Items, items ?? Foods));

    private string Text(LiveAnnouncement announcement) =>
        string.Concat(Render(announcement.Content).Nodes.Select(node => node.TextContent));
}
