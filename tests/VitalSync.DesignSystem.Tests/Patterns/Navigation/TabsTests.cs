using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Patterns.Navigation;

namespace VitalSync.DesignSystem.Tests.Patterns.Navigation;

public sealed class TabsTests : BunitContext
{
    private const string ModulePath = "./_content/VitalSync.DesignSystem/Patterns/Navigation/Tabs.razor.js";

    private static readonly (string Key, string Title)[] Days =
    [
        ("mo", "Montag"),
        ("di", "Dienstag"),
        ("mi", "Mittwoch"),
        ("do", "Donnerstag"),
        ("fr", "Freitag"),
        ("sa", "Samstag"),
    ];

    private readonly BunitJSModuleInterop _module;

    public TabsTests()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _module = JSInterop.SetupModule(ModulePath);
        _module.SetupVoid("attach", _ => true).SetVoidResult();
        _module.SetupVoid("replaceUrl", _ => true).SetVoidResult();
        _module.SetupVoid("detach", _ => true).SetVoidResult();
    }

    [Fact]
    public void Renders_a_named_tablist_whose_tabs_and_panels_point_at_each_other()
    {
        var cut = RenderTabs();

        Assert.Equal("Wochentage", cut.Find("[role=tablist]").GetAttribute("aria-label"));
        var tabs = cut.FindAll("a[role=tab]");
        var panels = cut.FindAll("[role=tabpanel]");
        Assert.Equal(["Montag", "Dienstag", "Mittwoch"], tabs.Select(tab => tab.QuerySelector(".vs-tabs__title")!.TextContent));
        Assert.Equal(3, panels.Count);

        for (var i = 0; i < tabs.Count; i++)
        {
            Assert.Equal(panels[i].Id, tabs[i].GetAttribute("aria-controls"));
            Assert.Equal(tabs[i].Id, panels[i].GetAttribute("aria-labelledby"));
            Assert.Equal("0", panels[i].GetAttribute("tabindex"));
        }
    }

    [Fact]
    public void Selects_the_first_tab_and_hides_the_other_panels()
    {
        var cut = RenderTabs();

        Assert.Equal(["true", "false", "false"], cut.FindAll("a[role=tab]").Select(tab => tab.GetAttribute("aria-selected")));
        Assert.Equal([false, true, true], cut.FindAll("[role=tabpanel]").Select(panel => panel.HasAttribute("hidden")));
    }

    [Fact]
    public void Selects_the_tab_named_by_Active()
    {
        var cut = RenderTabs(parameters => parameters.Add(p => p.Active, "di"));

        AssertSelected(cut, "Dienstag");
    }

    [Fact]
    public void The_address_wins_over_Active_so_a_reload_restores_the_tab()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("wochenplan?tab=mi");

        var cut = RenderTabs(parameters => parameters.Add(p => p.Active, "di"));

        AssertSelected(cut, "Mittwoch");
    }

    [Fact]
    public void Ignores_an_unknown_key_in_the_address()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("wochenplan?tab=so");

        var cut = RenderTabs(parameters => parameters.Add(p => p.Active, "di"));

        AssertSelected(cut, "Dienstag");
    }

    [Fact]
    public void Every_tab_links_to_its_own_address()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("wochenplan?woche=12");

        var cut = RenderTabs(parameters => parameters.Add(p => p.QueryName, "tag"));

        Assert.Equal(
            "http://localhost/wochenplan?woche=12&tag=di",
            cut.FindAll("a[role=tab]")[1].GetAttribute("href"));
    }

    [Fact]
    public void Every_tab_is_a_plain_tabbable_link_before_the_page_is_interactive()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderTabs();

        Assert.All(cut.FindAll("a[role=tab]"), tab => Assert.False(tab.HasAttribute("tabindex")));
        Assert.Empty(_module.Invocations);
    }

    [Fact]
    public void Only_the_selected_tab_is_a_tab_stop_once_interactive()
    {
        var cut = RenderTabs(parameters => parameters.Add(p => p.Active, "di"));

        Assert.Equal(["-1", "0", "-1"], cut.FindAll("a[role=tab]").Select(tab => tab.GetAttribute("tabindex")));
        Assert.Single(_module.Invocations["attach"]);
    }

    [Fact]
    public async Task Selecting_a_tab_reports_it_and_replaces_the_address()
    {
        string? reported = null;
        var cut = RenderTabs(parameters => parameters
            .Add(p => p.ActiveChanged, EventCallback.Factory.Create<string?>(this, key => reported = key)));
        Assert.Empty(_module.Invocations["replaceUrl"]);

        await cut.InvokeAsync(() => cut.Instance.Select("mi"));

        Assert.Equal("mi", reported);
        AssertSelected(cut, "Mittwoch");
        var call = Assert.Single(_module.Invocations["replaceUrl"]);
        Assert.Equal(["tab", "mi"], call.Arguments);
    }

    [Fact]
    public async Task Ignores_a_selection_of_an_unknown_key()
    {
        var cut = RenderTabs();

        await cut.InvokeAsync(() => cut.Instance.Select("so"));

        AssertSelected(cut, "Montag");
        Assert.Empty(_module.Invocations["replaceUrl"]);
    }

    [Fact]
    public void Tells_the_page_which_tab_the_address_selected()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("wochenplan?tab=mi");
        string? reported = null;

        RenderTabs(parameters => parameters
            .Add(p => p.ActiveChanged, EventCallback.Factory.Create<string?>(this, key => reported = key)));

        Assert.Equal("mi", reported);
    }

    [Fact]
    public void Follows_a_new_Active_from_the_page_and_updates_the_address()
    {
        var cut = RenderTabs(parameters => parameters.Add(p => p.Active, "mo"));

        cut.Render(parameters => parameters.Add(p => p.Active, "mi"));

        AssertSelected(cut, "Mittwoch");
        var call = Assert.Single(_module.Invocations["replaceUrl"]);
        Assert.Equal(["tab", "mi"], call.Arguments);
    }

    [Fact]
    public void Hides_the_icon_and_completes_the_badge_for_screen_readers()
    {
        var cut = Render<Tabs>(parameters => parameters
            .Add(p => p.Label, "Plan")
            .AddChildContent<Tab>(tab => tab
                .Add(p => p.Key, "einkauf")
                .Add(p => p.Title, "Einkauf")
                .Add(p => p.Icon, "<svg></svg>")
                .Add(p => p.Badge, "3")
                .Add(p => p.BadgeDescription, "offen")
                .AddChildContent("Liste"))
            .AddChildContent<Tab>(tab => tab
                .Add(p => p.Key, "rezepte")
                .Add(p => p.Title, "Rezepte")
                .AddChildContent("Rezepte")));

        var tabs = cut.FindAll("a[role=tab]");
        Assert.Equal("true", tabs[0].QuerySelector(".vs-tabs__icon")!.GetAttribute("aria-hidden"));
        Assert.Equal("3", tabs[0].QuerySelector(".vs-tabs__badge")!.TextContent);
        Assert.Equal("offen", tabs[0].QuerySelector(".vs-tabs__hidden")!.TextContent);
        Assert.Null(tabs[1].QuerySelector(".vs-tabs__icon"));
        Assert.Null(tabs[1].QuerySelector(".vs-tabs__badge"));
    }

    [Fact]
    public void Shows_a_changed_badge_in_the_tab()
    {
        var cut = Render<BadgeHost>(parameters => parameters.Add(p => p.Count, "2"));

        cut.Render(parameters => parameters.Add(p => p.Count, "5"));

        Assert.Equal("5", cut.Find(".vs-tabs__badge").TextContent);
    }

    [Fact]
    public void Reserves_the_width_of_the_semibold_title()
    {
        var cut = RenderTabs();

        Assert.All(
            cut.FindAll(".vs-tabs__title"),
            title => Assert.Equal(title.TextContent, title.GetAttribute("data-text")));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void Throws_outside_two_to_five_tabs(int count)
    {
        Assert.Throws<InvalidOperationException>(() => RenderTabs(count: count));
    }

    [Fact]
    public void Throws_on_a_duplicate_key()
    {
        Assert.Throws<InvalidOperationException>(() => Render<Tabs>(parameters => parameters
            .Add(p => p.Label, "Wochentage")
            .AddChildContent<Tab>(tab => tab.Add(p => p.Key, "mo").Add(p => p.Title, "Montag").AddChildContent("a"))
            .AddChildContent<Tab>(tab => tab.Add(p => p.Key, "mo").Add(p => p.Title, "Dienstag").AddChildContent("b"))));
    }

    [Theory]
    [InlineData(" ", "tab", "mo", "Montag", null, null)]
    [InlineData("Wochentage", " ", "mo", "Montag", null, null)]
    [InlineData("Wochentage", "tab", " ", "Montag", null, null)]
    [InlineData("Wochentage", "tab", "mo", " ", null, null)]
    [InlineData("Wochentage", "tab", "mo", "Montag", " ", null)]
    [InlineData("Wochentage", "tab", "mo", "Montag", null, "offen")]
    [InlineData("Wochentage", "tab", "mo", "Montag", "3", " ")]
    public void Throws_on_blank_or_incomplete_values(
        string label, string queryName, string key, string title, string? badge, string? badgeDescription)
    {
        Assert.Throws<InvalidOperationException>(() => Render<Tabs>(parameters => parameters
            .Add(p => p.Label, label)
            .Add(p => p.QueryName, queryName)
            .AddChildContent<Tab>(tab => tab
                .Add(p => p.Key, key)
                .Add(p => p.Title, title)
                .Add(p => p.Badge, badge)
                .Add(p => p.BadgeDescription, badgeDescription)
                .AddChildContent("a"))
            .AddChildContent<Tab>(tab => tab.Add(p => p.Key, "di").Add(p => p.Title, "Dienstag").AddChildContent("b"))));
    }

    [Fact]
    public void Throws_for_a_tab_outside_Tabs()
    {
        Assert.Throws<InvalidOperationException>(() => Render<Tab>(parameters => parameters
            .Add(p => p.Key, "mo")
            .Add(p => p.Title, "Montag")
            .AddChildContent("a")));
    }

    private IRenderedComponent<Tabs> RenderTabs(
        Action<ComponentParameterCollectionBuilder<Tabs>>? extra = null,
        int count = 3) =>
        Render<Tabs>(parameters =>
        {
            parameters.Add(p => p.Label, "Wochentage");

            foreach (var (key, title) in Days.Take(count))
            {
                parameters.AddChildContent<Tab>(tab => tab
                    .Add(p => p.Key, key)
                    .Add(p => p.Title, title)
                    .AddChildContent($"Plan für {title}"));
            }

            extra?.Invoke(parameters);
        });

    private static void AssertSelected(IRenderedComponent<Tabs> cut, string title)
    {
        var tabs = cut.FindAll("a[role=tab]");
        var panels = cut.FindAll("[role=tabpanel]");
        var index = tabs.ToList().FindIndex(tab => tab.QuerySelector(".vs-tabs__title")!.TextContent == title);

        Assert.True(index >= 0);
        Assert.Equal("true", tabs[index].GetAttribute("aria-selected"));
        Assert.Equal(1, tabs.Count(tab => tab.GetAttribute("aria-selected") == "true"));
        Assert.False(panels[index].HasAttribute("hidden"));
        Assert.Equal(panels.Count - 1, panels.Count(panel => panel.HasAttribute("hidden")));
    }

    private sealed class BadgeHost : ComponentBase
    {
        [Parameter]
        public string Count { get; set; } = "";

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<Tabs>(0);
            builder.AddComponentParameter(1, nameof(Tabs.Label), "Plan");
            builder.AddComponentParameter(2, nameof(Tabs.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<Tab>(0);
                content.AddComponentParameter(1, nameof(Tab.Key), "einkauf");
                content.AddComponentParameter(2, nameof(Tab.Title), "Einkauf");
                content.AddComponentParameter(3, nameof(Tab.Badge), Count);
                content.AddComponentParameter(4, nameof(Tab.ChildContent), (RenderFragment)(body => body.AddContent(0, "Liste")));
                content.CloseComponent();
                content.OpenComponent<Tab>(5);
                content.AddComponentParameter(6, nameof(Tab.Key), "rezepte");
                content.AddComponentParameter(7, nameof(Tab.Title), "Rezepte");
                content.AddComponentParameter(8, nameof(Tab.ChildContent), (RenderFragment)(body => body.AddContent(0, "Rezepte")));
                content.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
