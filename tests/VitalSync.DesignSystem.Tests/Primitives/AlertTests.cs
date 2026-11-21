using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using VitalSync.DesignSystem.Primitives;

namespace VitalSync.DesignSystem.Tests.Primitives;

public sealed class AlertTests : BunitContext
{
    private const string FocusIdentifier = "Blazor._internal.domWrapper.focus";
    private const string InfoPathStart = "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm-4";
    private const string CheckCirclePathStart = "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm45.66";
    private const string WarningPathStart = "M236.8,188.09";
    private const string XCirclePathStart = "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm37.66";
    private const string XPathStart = "M208.49,191.51";

    [Theory]
    [InlineData(AlertVariant.Info, "Information – Neue Rezepte", "vs-alert--info", InfoPathStart)]
    [InlineData(AlertVariant.Success, "Erfolg – Neue Rezepte", "vs-alert--success", CheckCirclePathStart)]
    [InlineData(AlertVariant.Warning, "Warnung – Neue Rezepte", "vs-alert--warning", WarningPathStart)]
    [InlineData(AlertVariant.Critical, "Fehler – Neue Rezepte", "vs-alert--critical", XCirclePathStart)]
    public void Writes_the_kind_into_the_title_and_shows_its_fixed_icon(
        AlertVariant variant,
        string expectedTitle,
        string expectedClass,
        string expectedPathStart)
    {
        var cut = Render<Alert>(parameters => parameters
            .Add(p => p.Variant, variant)
            .Add(p => p.Title, "  Neue Rezepte  ")
            .Add(p => p.HeadingLevel, 2)
            .AddChildContent("Drei Rezepte passen zu deinem Plan."));

        Assert.Equal(expectedTitle, cut.Find("h2.vs-alert__title").TextContent);
        Assert.Contains(expectedClass, cut.Find(".vs-alert").ClassList);
        Assert.StartsWith(expectedPathStart, cut.Find(".vs-alert__icon svg path").GetAttribute("d"), StringComparison.Ordinal);
        Assert.Equal("Drei Rezepte passen zu deinem Plan.", cut.Find(".vs-alert__text").TextContent);
    }

    [Fact]
    public void Defaults_to_the_info_variant()
    {
        var cut = RenderAlert();

        Assert.Contains("vs-alert--info", cut.Find(".vs-alert").ClassList);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Renders_the_title_at_the_given_heading_level(int level)
    {
        var cut = Render<Alert>(parameters => parameters
            .Add(p => p.Title, "Gespeichert")
            .Add(p => p.HeadingLevel, level)
            .AddChildContent("Dein Plan ist gespeichert."));

        Assert.Equal($"H{level}", cut.Find(".vs-alert__title").TagName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void Throws_when_the_heading_level_is_outside_2_to_6(int level)
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Alert>(parameters => parameters
                .Add(p => p.Title, "Gespeichert")
                .Add(p => p.HeadingLevel, level)
                .AddChildContent("Dein Plan ist gespeichert.")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_Title_is_blank(string title)
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Alert>(parameters => parameters
                .Add(p => p.Title, title)
                .Add(p => p.HeadingLevel, 2)
                .AddChildContent("Dein Plan ist gespeichert.")));
    }

    [Fact]
    public void Throws_when_no_ChildContent_is_given()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Alert>(parameters => parameters
                .Add(p => p.Title, "Gespeichert")
                .Add(p => p.HeadingLevel, 2)));
    }

    [Fact]
    public void Hides_the_kind_icon_from_assistive_technology()
    {
        var cut = RenderAlert();

        Assert.Equal("true", cut.Find(".vs-alert__icon").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Is_not_announced_by_default()
    {
        var cut = RenderAlert(AlertVariant.Critical);

        Assert.False(cut.Find(".vs-alert").HasAttribute("role"));
    }

    [Theory]
    [InlineData(AlertVariant.Info, "status")]
    [InlineData(AlertVariant.Success, "status")]
    [InlineData(AlertVariant.Warning, "alert")]
    [InlineData(AlertVariant.Critical, "alert")]
    public void Announces_with_the_urgency_of_its_kind_when_asked_to(AlertVariant variant, string expectedRole)
    {
        var cut = Render<Alert>(parameters => parameters
            .Add(p => p.Variant, variant)
            .Add(p => p.Title, "Gespeichert")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Announce, true)
            .AddChildContent("Dein Plan ist gespeichert."));

        Assert.Equal(expectedRole, cut.Find(".vs-alert").GetAttribute("role"));
    }

    [Fact]
    public void Renders_the_action_below_the_text()
    {
        var cut = Render<Alert>(parameters => parameters
            .Add(p => p.Variant, AlertVariant.Warning)
            .Add(p => p.Title, "Zutaten fehlen")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Action, "<a href=\"/zutaten\">Zutaten ergänzen</a>")
            .AddChildContent("Zwei Rezepte brauchen Zutaten, die nicht erfasst sind."));

        var link = cut.Find(".vs-alert__content > .vs-alert__action a");
        Assert.Equal("Zutaten ergänzen", link.TextContent);
        var next = cut.Find(".vs-alert__text").NextElementSibling;
        Assert.NotNull(next);
        Assert.Contains("vs-alert__action", next.ClassList);
    }

    [Fact]
    public void Renders_neither_action_nor_close_button_unless_given()
    {
        var cut = RenderAlert();

        Assert.Empty(cut.FindAll(".vs-alert__action"));
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Names_the_close_button_after_the_title_and_hides_its_icon()
    {
        var cut = Render<Alert>(parameters => parameters
            .Add(p => p.Variant, AlertVariant.Success)
            .Add(p => p.Title, "Gespeichert")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.OnClose, () => { })
            .Add(p => p.CloseFocusTarget, () => new ElementReference("target"))
            .AddChildContent("Dein Plan ist gespeichert."));

        var button = cut.Find("button.vs-alert__close");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Schliessen: Erfolg – Gespeichert", button.QuerySelector(".vs-alert__close-name")?.TextContent);
        Assert.Equal("true", button.QuerySelector(".vs-alert__close-icon")?.GetAttribute("aria-hidden"));
        Assert.StartsWith(XPathStart, button.QuerySelector("svg path")?.GetAttribute("d"), StringComparison.Ordinal);
    }

    [Fact]
    public void Throws_when_closable_without_a_CloseFocusTarget()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Alert>(parameters => parameters
                .Add(p => p.Title, "Gespeichert")
                .Add(p => p.HeadingLevel, 2)
                .Add(p => p.OnClose, () => { })
                .AddChildContent("Dein Plan ist gespeichert.")));
    }

    [Fact]
    public void Moves_focus_to_the_target_before_reporting_the_close()
    {
        JSInterop.SetupVoid(FocusIdentifier, _ => true).SetVoidResult();
        var focusCallsWhenClosed = -1;
        var cut = Render<Alert>(parameters => parameters
            .Add(p => p.Title, "Gespeichert")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.OnClose, () => focusCallsWhenClosed = JSInterop.Invocations[FocusIdentifier].Count)
            .Add(p => p.CloseFocusTarget, () => new ElementReference("next-heading", new WebElementReferenceContext(JSInterop.JSRuntime)))
            .AddChildContent("Dein Plan ist gespeichert."));

        cut.Find(".vs-alert__close").Click();

        var call = Assert.Single(JSInterop.Invocations[FocusIdentifier]);
        Assert.Equal("next-heading", ((ElementReference)call.Arguments[0]!).Id);
        Assert.Equal(1, focusCallsWhenClosed);
    }

    [Fact]
    public void Keeps_its_own_role_and_appends_a_passed_class()
    {
        var cut = Render<Alert>(parameters => parameters
            .Add(p => p.Variant, AlertVariant.Critical)
            .Add(p => p.Title, "Speichern fehlgeschlagen")
            .Add(p => p.HeadingLevel, 2)
            .Add(p => p.Announce, true)
            .AddUnmatched("role", "note")
            .AddUnmatched("class", "plan-alert")
            .AddUnmatched("data-test", "x")
            .AddChildContent("Bitte versuche es erneut."));

        var alert = cut.Find(".vs-alert");
        Assert.Equal("alert", alert.GetAttribute("role"));
        Assert.Equal("vs-alert vs-alert--critical plan-alert", alert.GetAttribute("class"));
        Assert.Equal("x", alert.GetAttribute("data-test"));
    }

    private IRenderedComponent<Alert> RenderAlert(AlertVariant? variant = null) =>
        Render<Alert>(parameters =>
        {
            if (variant is not null)
            {
                parameters.Add(p => p.Variant, variant.Value);
            }

            parameters
                .Add(p => p.Title, "Gespeichert")
                .Add(p => p.HeadingLevel, 2)
                .AddChildContent("Dein Plan ist gespeichert.");
        });
}
