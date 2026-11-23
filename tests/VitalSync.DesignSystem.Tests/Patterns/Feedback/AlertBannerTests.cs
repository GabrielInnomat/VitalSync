using Bunit;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Accessibility;
using VitalSync.DesignSystem.Patterns.Feedback;

namespace VitalSync.DesignSystem.Tests.Patterns.Feedback;

public sealed class AlertBannerTests : BunitContext
{
    private const string ModulePath = "./_content/VitalSync.DesignSystem/Patterns/Feedback/AlertBannerRegion.razor.js";
    private const string InfoPathStart = "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm-4";
    private const string WarningPathStart = "M236.8,188.09";
    private const string XCirclePathStart = "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm37.66";
    private const string XPathStart = "M208.49,191.51";

    private readonly AlertBannerService _service;
    private readonly BunitJSModuleInterop _module;

    public AlertBannerTests()
    {
        Services.AddScoped<LiveAnnouncer>();
        Services.AddScoped<AlertBannerService>();
        _service = Services.GetRequiredService<AlertBannerService>();
        _module = JSInterop.SetupModule(ModulePath);
        _module.SetupVoid("focusMainContent").SetVoidResult();
    }

    [Theory]
    [InlineData(AlertBannerVariant.Info, "Information – Wartung", "vs-alert-banner--info", InfoPathStart)]
    [InlineData(AlertBannerVariant.Warning, "Warnung – Wartung", "vs-alert-banner--warning", WarningPathStart)]
    [InlineData(AlertBannerVariant.Critical, "Fehler – Wartung", "vs-alert-banner--critical", XCirclePathStart)]
    public void Writes_the_kind_into_a_second_level_heading_and_shows_its_fixed_icon(
        AlertBannerVariant variant,
        string expectedHeading,
        string expectedClass,
        string expectedPathStart)
    {
        var cut = Render<AlertBannerRegion>();

        Show(cut, variant, "wartung", "  Wartung  ", "  Am Sonntag nicht erreichbar.  ");

        var banner = cut.Find(".vs-alert-banner");
        Assert.Contains(expectedClass, banner.ClassList);
        var heading = cut.Find(".vs-alert-banner__title");
        Assert.Equal("H2", heading.TagName);
        Assert.Equal(expectedHeading, heading.TextContent);
        Assert.Equal("Am Sonntag nicht erreichbar.", cut.Find(".vs-alert-banner__text").TextContent);
        Assert.Equal("true", cut.Find(".vs-alert-banner__icon").GetAttribute("aria-hidden"));
        Assert.StartsWith(expectedPathStart, cut.Find(".vs-alert-banner__icon svg path").GetAttribute("d"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "Titel", "Text")]
    [InlineData("   ", "Titel", "Text")]
    [InlineData("key", "", "Text")]
    [InlineData("key", "   ", "Text")]
    [InlineData("key", "Titel", "")]
    [InlineData("key", "Titel", "   ")]
    public void Refuses_a_blank_key_title_or_text(string key, string title, string text)
    {
        Assert.Throws<ArgumentException>(() => _service.ShowInfo(key, title, text));
        Assert.Throws<ArgumentException>(() => _service.ShowWarning(key, title, text));
        Assert.Throws<ArgumentException>(() => _service.ShowCritical(key, title, text));
    }

    [Theory]
    [InlineData("", "wartung")]
    [InlineData("   ", "wartung")]
    [InlineData("Mehr erfahren", "")]
    [InlineData("Mehr erfahren", "   ")]
    public void Refuses_a_link_without_text_or_target(string text, string href) =>
        Assert.Throws<ArgumentException>(() => new AlertBannerLink(text, href));

    [Fact]
    public void Refuses_a_blank_key_when_removing() =>
        Assert.Throws<ArgumentException>(() => _service.Remove(" "));

    [Fact]
    public void Renders_the_landmark_only_while_banners_are_shown()
    {
        var cut = Render<AlertBannerRegion>();

        Assert.Empty(cut.FindAll("section"));

        Show(cut, AlertBannerVariant.Warning, "offline", "Du bist offline", "Keine Verbindung.");

        Assert.Equal("Systemmeldungen", cut.Find("section.vs-alert-banner-region").GetAttribute("aria-label"));

        Run(cut, () => _service.Remove("offline"));

        Assert.Empty(cut.FindAll("section"));
    }

    [Fact]
    public void Orders_banners_by_severity_and_then_by_first_appearance()
    {
        var cut = Render<AlertBannerRegion>();

        Show(cut, AlertBannerVariant.Info, "a", "Info A", "Text.");
        Show(cut, AlertBannerVariant.Warning, "b", "Warnung B", "Text.");
        Show(cut, AlertBannerVariant.Info, "c", "Info C", "Text.");
        Show(cut, AlertBannerVariant.Critical, "d", "Fehler D", "Text.");
        Show(cut, AlertBannerVariant.Warning, "e", "Warnung E", "Text.");

        Assert.Equal(
            ["Fehler – Fehler D", "Warnung – Warnung B", "Warnung – Warnung E", "Information – Info A", "Information – Info C"],
            Headings(cut));
    }

    [Fact]
    public void Replaces_a_banner_shown_again_under_the_same_key_in_place()
    {
        var cut = Render<AlertBannerRegion>();
        Show(cut, AlertBannerVariant.Info, "a", "Erste", "Text.");
        Show(cut, AlertBannerVariant.Info, "b", "Zweite", "Text.");

        Show(cut, AlertBannerVariant.Info, "a", "Erste neu", "Neuer Text.");

        Assert.Equal(["Information – Erste neu", "Information – Zweite"], Headings(cut));
        Assert.Equal("Neuer Text.", cut.Find(".vs-alert-banner__text").TextContent);
    }

    [Fact]
    public void Removes_only_the_banner_with_the_given_key()
    {
        var cut = Render<AlertBannerRegion>();
        Show(cut, AlertBannerVariant.Warning, "offline", "Du bist offline", "Text.");
        Show(cut, AlertBannerVariant.Info, "wartung", "Wartung", "Text.");

        Run(cut, () => _service.Remove("offline"));
        Run(cut, () => _service.Remove("unbekannt"));

        Assert.Equal(["Information – Wartung"], Headings(cut));
    }

    [Fact]
    public void Shows_a_removed_banner_again_when_it_is_shown_again()
    {
        var cut = Render<AlertBannerRegion>();
        Show(cut, AlertBannerVariant.Warning, "offline", "Du bist offline", "Text.");
        Run(cut, () => _service.Remove("offline"));

        Show(cut, AlertBannerVariant.Warning, "offline", "Du bist offline", "Text.");

        Assert.Equal(["Warnung – Du bist offline"], Headings(cut));
    }

    [Fact]
    public void Has_no_close_button_unless_the_banner_is_closable()
    {
        var cut = Render<AlertBannerRegion>();

        Show(cut, AlertBannerVariant.Critical, "abgleich", "Abgleich fehlgeschlagen", "Text.");

        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Has_a_close_button_named_after_the_full_heading_when_closable()
    {
        var cut = Render<AlertBannerRegion>();

        Run(cut, () => _service.ShowInfo("wartung", "Wartung", "Text.", closable: true));

        var button = cut.Find("button.vs-alert-banner__close");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Schliessen: Information – Wartung", button.QuerySelector(".vs-alert-banner__close-name")?.TextContent);
        Assert.Equal("true", button.QuerySelector(".vs-alert-banner__close-icon")?.GetAttribute("aria-hidden"));
        Assert.StartsWith(XPathStart, button.QuerySelector("svg path")?.GetAttribute("d"), StringComparison.Ordinal);
    }

    [Fact]
    public void Moves_focus_to_the_main_content_after_closing()
    {
        var cut = Render<AlertBannerRegion>();
        Run(cut, () => _service.ShowInfo("wartung", "Wartung", "Text.", closable: true));

        cut.Find(".vs-alert-banner__close").Click();

        Assert.Empty(cut.FindAll(".vs-alert-banner"));
        Assert.Single(_module.Invocations["focusMainContent"]);
    }

    [Fact]
    public void Keeps_a_closed_banner_closed_even_when_it_is_shown_again()
    {
        var cut = Render<AlertBannerRegion>();
        Run(cut, () => _service.ShowInfo("wartung", "Wartung", "Text.", closable: true));
        cut.Find(".vs-alert-banner__close").Click();

        Run(cut, () => _service.ShowInfo("wartung", "Wartung", "Text.", closable: true));
        Run(cut, () => _service.ShowCritical("wartung", "Wartung verlängert", "Text."));

        Assert.Empty(cut.FindAll(".vs-alert-banner"));
    }

    [Fact]
    public void Renders_the_action_as_a_link()
    {
        var cut = Render<AlertBannerRegion>();

        Run(cut, () => _service.ShowInfo("wartung", "Wartung", "Text.", new AlertBannerLink("  Mehr zur Wartung  ", "  wartung  ")));

        var link = cut.Find(".vs-alert-banner__action a.vs-alert-banner__link");
        Assert.Equal("Mehr zur Wartung", link.TextContent);
        Assert.Equal("wartung", link.GetAttribute("href"));
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Announces_information_politely_and_warnings_and_errors_immediately()
    {
        var cut = Render<AlertBannerRegion>();
        var announcer = Render<LiveAnnouncerRegion>();

        Show(cut, AlertBannerVariant.Info, "wartung", "Wartung", "Am Sonntag.");
        Show(cut, AlertBannerVariant.Warning, "offline", "Du bist offline", "Keine Verbindung.");

        Assert.Equal(["Information – Wartung", "Am Sonntag."], Announcement(announcer, "status"));
        Assert.Equal(["Warnung – Du bist offline", "Keine Verbindung."], Announcement(announcer, "alert"));

        Show(cut, AlertBannerVariant.Critical, "abgleich", "Abgleich fehlgeschlagen", "Daten bleiben lokal.");

        Assert.Equal(
            ["Warnung – Du bist offline", "Keine Verbindung.", "Fehler – Abgleich fehlgeschlagen", "Daten bleiben lokal."],
            Announcement(announcer, "alert"));
        Assert.Empty(cut.FindAll("[role=status], [role=alert]"));
    }

    [Fact]
    public void Announces_a_replaced_banner_again_but_not_an_unchanged_repeat()
    {
        var cut = Render<AlertBannerRegion>();
        var announcer = Render<LiveAnnouncerRegion>();
        Show(cut, AlertBannerVariant.Warning, "offline", "Du bist offline", "Keine Verbindung.");
        var renders = cut.RenderCount;

        Show(cut, AlertBannerVariant.Warning, "offline", "Du bist offline", "Keine Verbindung.");

        Assert.Equal(renders, cut.RenderCount);
        Assert.Equal(["Warnung – Du bist offline", "Keine Verbindung."], Announcement(announcer, "alert"));

        Show(cut, AlertBannerVariant.Warning, "offline", "Du bist offline", "Seit fünf Minuten keine Verbindung.");

        Assert.Equal(
            ["Warnung – Du bist offline", "Keine Verbindung.", "Warnung – Du bist offline", "Seit fünf Minuten keine Verbindung."],
            Announcement(announcer, "alert"));
    }

    [Fact]
    public async Task Shows_banners_raised_outside_the_render_thread()
    {
        var cut = Render<AlertBannerRegion>();

        await Task.Run(() => _service.ShowWarning("offline", "Du bist offline", "Text."), Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".vs-alert-banner")));
    }

    [Fact]
    public void Refuses_to_render_a_banner_without_title_or_text()
    {
        Assert.Throws<InvalidOperationException>(() => Render<AlertBanner>(parameters => parameters
            .Add(banner => banner.Title, " ")
            .Add(banner => banner.Text, "Text.")));
        Assert.Throws<InvalidOperationException>(() => Render<AlertBanner>(parameters => parameters
            .Add(banner => banner.Title, "Titel")
            .Add(banner => banner.Text, " ")));
    }

    private static List<string> Headings(IRenderedComponent<AlertBannerRegion> cut) =>
        [.. cut.FindAll(".vs-alert-banner__title").Select(title => title.TextContent)];

    private static List<string> Announcement(IRenderedComponent<LiveAnnouncerRegion> announcer, string role) =>
        [.. announcer.FindAll($"[role={role}] p").Select(p => p.TextContent)];

    private static void Run(IRenderedComponent<AlertBannerRegion> cut, Action action) =>
        cut.InvokeAsync(action).GetAwaiter().GetResult();

    private void Show(IRenderedComponent<AlertBannerRegion> cut, AlertBannerVariant variant, string key, string title, string text) =>
        Run(cut, () =>
        {
            switch (variant)
            {
                case AlertBannerVariant.Warning:
                    _service.ShowWarning(key, title, text);
                    break;
                case AlertBannerVariant.Critical:
                    _service.ShowCritical(key, title, text);
                    break;
                default:
                    _service.ShowInfo(key, title, text);
                    break;
            }
        });
}
