using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Accessibility;
using VitalSync.DesignSystem.Patterns.Feedback;

namespace VitalSync.DesignSystem.Tests.Patterns.Feedback;

public sealed class ToastTests : BunitContext
{
    private const string ModulePath = "./_content/VitalSync.DesignSystem/Patterns/Feedback/ToastRegion.razor.js";
    private const string FocusIdentifier = "Blazor._internal.domWrapper.focus";
    private const string InfoPathStart = "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm-4";
    private const string CheckCirclePathStart = "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm45.66";
    private const string XCirclePathStart = "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm37.66";
    private const string XPathStart = "M208.49,191.51";

    private readonly ToastService _service;
    private readonly BunitJSModuleInterop _module;

    public ToastTests()
    {
        Services.AddScoped<LiveAnnouncer>();
        Services.AddScoped<ToastService>();
        _service = Services.GetRequiredService<ToastService>();
        _module = JSInterop.SetupModule(ModulePath);
        _module.SetupVoid("focusMainContent").SetVoidResult();
        JSInterop.SetupVoid(FocusIdentifier, _ => true).SetVoidResult();
    }

    [Theory]
    [InlineData(ToastVariant.Info, "Information – Neue Rezepte", "vs-toast--info", InfoPathStart)]
    [InlineData(ToastVariant.Success, "Erfolg – Neue Rezepte", "vs-toast--success", CheckCirclePathStart)]
    [InlineData(ToastVariant.Critical, "Fehler – Neue Rezepte", "vs-toast--critical", XCirclePathStart)]
    public void Writes_the_kind_into_the_title_and_shows_its_fixed_icon(
        ToastVariant variant,
        string expectedTitle,
        string expectedClass,
        string expectedPathStart)
    {
        var cut = Render<ToastRegion>();

        Show(cut, variant, "  Neue Rezepte  ", "  Drei Rezepte passen zu deinem Plan.  ");

        var toast = cut.Find(".vs-toast");
        Assert.Contains(expectedClass, toast.ClassList);
        Assert.Equal(expectedTitle, cut.Find(".vs-toast__title").TextContent);
        Assert.Equal("Drei Rezepte passen zu deinem Plan.", cut.Find(".vs-toast__text").TextContent);
        Assert.Equal("true", cut.Find(".vs-toast__icon").GetAttribute("aria-hidden"));
        Assert.StartsWith(expectedPathStart, cut.Find(".vs-toast__icon svg path").GetAttribute("d"), StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_the_title_as_emphasised_text_rather_than_a_heading()
    {
        var cut = Render<ToastRegion>();

        Show(cut, ToastVariant.Success, "Plan gespeichert", "Dein Wochenplan ist gespeichert.");

        Assert.Equal("STRONG", cut.Find(".vs-toast__title").TagName);
        Assert.Empty(cut.FindAll("h1, h2, h3, h4, h5, h6"));
    }

    [Fact]
    public void Has_a_close_button_named_after_the_full_title_as_its_only_control()
    {
        var cut = Render<ToastRegion>();

        Show(cut, ToastVariant.Success, "Plan gespeichert", "Dein Wochenplan ist gespeichert.");

        var button = Assert.Single(cut.FindAll("button"));
        Assert.Empty(cut.FindAll("a"));
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Schliessen: Erfolg – Plan gespeichert", button.QuerySelector(".vs-toast__close-name")?.TextContent);
        Assert.Equal("true", button.QuerySelector(".vs-toast__close-icon")?.GetAttribute("aria-hidden"));
        Assert.StartsWith(XPathStart, button.QuerySelector("svg path")?.GetAttribute("d"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "Text")]
    [InlineData("   ", "Text")]
    [InlineData("Titel", "")]
    [InlineData("Titel", "   ")]
    public void Refuses_a_blank_title_or_text(string title, string text)
    {
        Assert.Throws<ArgumentException>(() => _service.ShowSuccess(title, text));
        Assert.Throws<ArgumentException>(() => _service.ShowInfo(title, text));
        Assert.Throws<ArgumentException>(() => _service.ShowCritical(title, text));
    }

    [Fact]
    public void Renders_the_notification_landmark_only_while_toasts_are_open()
    {
        var cut = Render<ToastRegion>();

        Assert.Empty(cut.FindAll("section"));

        Show(cut, ToastVariant.Info, "Neue Rezepte", "Drei neue Rezepte.");

        Assert.Equal("Benachrichtigungen", cut.Find("section.vs-toast-region").GetAttribute("aria-label"));

        cut.Find(".vs-toast__close").Click();

        Assert.Empty(cut.FindAll("section"));
    }

    [Fact]
    public void Stacks_the_newest_toast_last()
    {
        var cut = Render<ToastRegion>();

        Show(cut, ToastVariant.Info, "Erste", "Text.");
        Show(cut, ToastVariant.Info, "Zweite", "Text.");

        Assert.Equal(
            ["Information – Erste", "Information – Zweite"],
            cut.FindAll("ol.vs-toast-region__list > li .vs-toast__title").Select(title => title.TextContent));
    }

    [Fact]
    public void Keeps_at_most_three_toasts_by_dropping_the_oldest()
    {
        var cut = Render<ToastRegion>();

        foreach (var title in new[] { "Eins", "Zwei", "Drei", "Vier" })
        {
            Show(cut, ToastVariant.Info, title, "Text.");
        }

        Assert.Equal(["Zwei", "Drei", "Vier"], Titles(cut));
    }

    [Fact]
    public void Never_drops_the_toast_that_holds_focus()
    {
        var cut = Render<ToastRegion>();
        Show(cut, ToastVariant.Info, "Eins", "Text.");
        Show(cut, ToastVariant.Info, "Zwei", "Text.");
        Show(cut, ToastVariant.Info, "Drei", "Text.");

        cut.FindAll(".vs-toast__close")[0].Focus();
        Show(cut, ToastVariant.Info, "Vier", "Text.");

        Assert.Equal(["Eins", "Drei", "Vier"], Titles(cut));

        cut.FindAll(".vs-toast__close")[0].Blur();
        Show(cut, ToastVariant.Info, "Fünf", "Text.");

        Assert.Equal(["Drei", "Vier", "Fünf"], Titles(cut));
    }

    [Fact]
    public void Announces_success_and_information_politely_and_critical_immediately()
    {
        var cut = Render<ToastRegion>();
        var announcer = Render<LiveAnnouncerRegion>();

        Show(cut, ToastVariant.Success, "Plan gespeichert", "Dein Wochenplan ist gespeichert.");
        Show(cut, ToastVariant.Critical, "Speichern fehlgeschlagen", "Keine Verbindung.");

        Assert.Equal(
            ["Erfolg – Plan gespeichert", "Dein Wochenplan ist gespeichert."],
            announcer.FindAll("[role=status] p").Select(p => p.TextContent));
        Assert.Equal(
            ["Fehler – Speichern fehlgeschlagen", "Keine Verbindung."],
            announcer.FindAll("[role=alert] p").Select(p => p.TextContent));
        Assert.Empty(cut.FindAll("[role=status], [role=alert]"));
    }

    [Fact]
    public void Announces_every_toast_of_a_burst_once()
    {
        var cut = Render<ToastRegion>();
        var announcer = Render<LiveAnnouncerRegion>();

        Show(cut, ToastVariant.Info, "Mahlzeit 1", "Text.");
        Show(cut, ToastVariant.Info, "Mahlzeit 2", "Text.");
        Show(cut, ToastVariant.Info, "Mahlzeit 3", "Text.");
        Show(cut, ToastVariant.Info, "Mahlzeit 4", "Text.");

        Assert.Equal(
            ["Information – Mahlzeit 1", "Information – Mahlzeit 2", "Information – Mahlzeit 3", "Information – Mahlzeit 4"],
            announcer.FindAll("[role=status] > div > p:first-child").Select(p => p.TextContent));
    }

    [Fact]
    public async Task Shows_toasts_raised_outside_the_render_thread()
    {
        var cut = Render<ToastRegion>();

        await Task.Run(() => _service.ShowInfo("Neue Rezepte", "Drei neue Rezepte."), Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".vs-toast")));
    }

    [Fact]
    public void Moves_focus_to_the_toast_below_after_closing()
    {
        var cut = Render<ToastRegion>();
        Show(cut, ToastVariant.Info, "Eins", "Text.");
        Show(cut, ToastVariant.Info, "Zwei", "Text.");
        Show(cut, ToastVariant.Info, "Drei", "Text.");

        cut.FindAll(".vs-toast__close")[1].Click();

        AssertFocusedCloseButtonOf(cut, "Drei");
    }

    [Fact]
    public void Moves_focus_to_the_toast_above_after_closing_the_newest()
    {
        var cut = Render<ToastRegion>();
        Show(cut, ToastVariant.Info, "Eins", "Text.");
        Show(cut, ToastVariant.Info, "Zwei", "Text.");

        cut.FindAll(".vs-toast__close")[1].Click();

        AssertFocusedCloseButtonOf(cut, "Eins");
    }

    [Fact]
    public void Moves_focus_to_the_main_content_after_closing_the_last_toast()
    {
        var cut = Render<ToastRegion>();
        Show(cut, ToastVariant.Info, "Eins", "Text.");

        cut.Find(".vs-toast__close").Click();

        Assert.Single(_module.Invocations["focusMainContent"]);
        Assert.Empty(JSInterop.Invocations[FocusIdentifier]);
    }

    private static List<string> Titles(IRenderedComponent<ToastRegion> cut) =>
        [.. cut.FindAll(".vs-toast__title").Select(title => title.TextContent["Information – ".Length..])];

    private void Show(IRenderedComponent<ToastRegion> cut, ToastVariant variant, string title, string text) =>
        cut.InvokeAsync(() =>
        {
            switch (variant)
            {
                case ToastVariant.Success:
                    _service.ShowSuccess(title, text);
                    break;
                case ToastVariant.Critical:
                    _service.ShowCritical(title, text);
                    break;
                default:
                    _service.ShowInfo(title, text);
                    break;
            }
        }).GetAwaiter().GetResult();

    private void AssertFocusedCloseButtonOf(IRenderedComponent<ToastRegion> cut, string title)
    {
        var call = Assert.Single(JSInterop.Invocations[FocusIdentifier]);
        var toast = cut.FindComponents<Toast>().Single(t => t.Instance.Title == title);
        Assert.Equal(toast.Instance.CloseButton.Id, ((ElementReference)call.Arguments[0]!).Id);
        Assert.Empty(_module.Invocations["focusMainContent"]);
    }
}
