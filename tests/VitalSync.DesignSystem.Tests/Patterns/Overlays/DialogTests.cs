using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Accessibility;
using VitalSync.DesignSystem.Patterns.Overlays;

namespace VitalSync.DesignSystem.Tests.Patterns.Overlays;

public sealed class DialogTests : BunitContext
{
    private const string ModulePath = "./_content/VitalSync.DesignSystem/Patterns/Overlays/DialogFrame.razor.js";
    private const string XPathStart = "M208.49,191.51";

    private readonly BunitJSModuleInterop _module;

    public DialogTests()
    {
        Services.AddScoped<LiveAnnouncer>();
        _module = JSInterop.SetupModule(ModulePath);
        _module.SetupVoid("open", _ => true).SetVoidResult();
        _module.SetupVoid("close", _ => true).SetVoidResult();
        _module.SetupVoid("dispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Renders_no_content_and_calls_no_script_while_closed()
    {
        var cut = RenderDialog(open: false);

        var dialog = cut.Find("dialog.vs-dialog");
        Assert.Empty(dialog.Children);
        Assert.False(dialog.HasAttribute("aria-labelledby"));
        Assert.Empty(_module.Invocations);
    }

    [Fact]
    public void Opens_as_a_medium_dialog_named_by_its_h2_title()
    {
        var cut = RenderDialog();

        var dialog = cut.Find("dialog");
        var title = cut.Find("h2.vs-dialog__title");
        Assert.Equal("Mahlzeit bearbeiten", title.TextContent);
        Assert.Equal("-1", title.GetAttribute("tabindex"));
        Assert.Equal(title.Id, dialog.GetAttribute("aria-labelledby"));
        Assert.False(dialog.HasAttribute("role"));
        Assert.False(dialog.HasAttribute("aria-describedby"));
        Assert.Contains("vs-dialog--medium", dialog.ClassList);
        Assert.Equal("Passe die Menge an.", cut.Find(".vs-dialog__content").TextContent);
        Assert.Single(_module.Invocations["open"]);
    }

    [Fact]
    public void Can_be_shown_full_screen()
    {
        var cut = RenderDialog(extra: parameters => parameters.Add(p => p.Fullscreen, true));

        Assert.Contains("vs-dialog--fullscreen", cut.Find("dialog").ClassList);
    }

    [Fact]
    public void Always_has_an_X_named_Schliessen_that_asks_to_close()
    {
        var closeRequests = 0;
        var cut = RenderDialog(onClose: () => closeRequests++);

        var button = cut.Find("button.vs-dialog__close");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Schliessen", button.QuerySelector(".vs-dialog__close-name")?.TextContent);
        Assert.Equal("true", button.QuerySelector(".vs-dialog__close-icon")?.GetAttribute("aria-hidden"));
        Assert.StartsWith(XPathStart, button.QuerySelector("svg path")?.GetAttribute("d"), StringComparison.Ordinal);

        button.Click();

        Assert.Equal(1, closeRequests);
    }

    [Fact]
    public void Has_no_footer_without_Abbrechen_or_actions()
    {
        var cut = RenderDialog();

        Assert.Empty(cut.FindAll(".vs-dialog__footer"));
    }

    [Fact]
    public void Puts_Abbrechen_before_the_actions_and_lets_it_ask_to_close()
    {
        var closeRequests = 0;
        var cut = RenderDialog(
            onClose: () => closeRequests++,
            extra: parameters => parameters
                .Add(p => p.CancelButton, true)
                .Add(p => p.Actions, "<button type=\"button\" class=\"save\">Speichern</button>"));

        var buttons = cut.FindAll(".vs-dialog__footer button");
        Assert.Equal(2, buttons.Count);
        Assert.Equal("Abbrechen", buttons[0].TextContent.Trim());
        Assert.Contains("vs-button--secondary", buttons[0].ClassList);
        Assert.Contains("save", buttons[1].ClassList);

        buttons[0].Click();

        Assert.Equal(1, closeRequests);
    }

    [Fact]
    public async Task Reports_Escape_from_the_script_as_a_request_to_close()
    {
        var closeRequests = 0;
        var cut = RenderDialog(onClose: () => closeRequests++);
        var frame = cut.FindComponent<DialogFrame>();

        await cut.InvokeAsync(frame.Instance.RequestClose);

        Assert.Equal(1, closeRequests);
    }

    [Fact]
    public void Closes_through_the_script_and_hands_over_the_focus_target()
    {
        var cut = RenderDialog(extra: parameters => parameters
            .Add(p => p.CloseFocusTarget, () => new ElementReference("next-heading", new WebElementReferenceContext(JSInterop.JSRuntime))));

        cut.Render(parameters => parameters.Add(p => p.Open, false));

        var call = Assert.Single(_module.Invocations["close"]);
        Assert.Equal("next-heading", ((ElementReference)call.Arguments[1]!).Id);
        Assert.Empty(cut.Find("dialog").Children);
    }

    [Fact]
    public void Leaves_the_focus_target_to_the_script_when_none_is_given()
    {
        var cut = RenderDialog();

        cut.Render(parameters => parameters.Add(p => p.Open, false));

        Assert.Null(Assert.Single(_module.Invocations["close"]).Arguments[1]);
    }

    [Fact]
    public async Task Reopens_when_the_browser_closed_it_but_the_page_keeps_it_open()
    {
        var closeRequests = 0;
        var cut = RenderDialog(onClose: () => closeRequests++);
        var frame = cut.FindComponent<DialogFrame>();

        await cut.InvokeAsync(frame.Instance.ClosedByBrowser);

        Assert.Equal(1, closeRequests);
        Assert.Equal(2, _module.Invocations["open"].Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_Title_is_blank(string title)
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Dialog>(parameters => parameters
                .Add(p => p.Open, true)
                .Add(p => p.Title, title)
                .Add(p => p.OnClose, () => { })
                .AddChildContent("Passe die Menge an.")));
    }

    [Fact]
    public void Throws_without_ChildContent()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Dialog>(parameters => parameters
                .Add(p => p.Open, true)
                .Add(p => p.Title, "Mahlzeit bearbeiten")
                .Add(p => p.OnClose, () => { })));
    }

    [Fact]
    public void Throws_without_OnClose()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Dialog>(parameters => parameters
                .Add(p => p.Open, true)
                .Add(p => p.Title, "Mahlzeit bearbeiten")
                .AddChildContent("Passe die Menge an.")));
    }

    [Fact]
    public void Keeps_its_own_role_and_appends_a_passed_class()
    {
        var cut = RenderDialog(extra: parameters => parameters
            .AddUnmatched("role", "region")
            .AddUnmatched("class", "meal-dialog")
            .AddUnmatched("data-test", "x"));

        var dialog = cut.Find("dialog");
        Assert.False(dialog.HasAttribute("role"));
        Assert.Contains("vs-dialog", dialog.ClassList);
        Assert.Contains("meal-dialog", dialog.ClassList);
        Assert.Equal("x", dialog.GetAttribute("data-test"));
    }

    private IRenderedComponent<Dialog> RenderDialog(
        bool open = true,
        Action? onClose = null,
        Action<ComponentParameterCollectionBuilder<Dialog>>? extra = null) =>
        Render<Dialog>(parameters =>
        {
            parameters
                .Add(p => p.Open, open)
                .Add(p => p.Title, "Mahlzeit bearbeiten")
                .Add(p => p.OnClose, onClose ?? (() => { }))
                .AddChildContent("Passe die Menge an.");
            extra?.Invoke(parameters);
        });
}
