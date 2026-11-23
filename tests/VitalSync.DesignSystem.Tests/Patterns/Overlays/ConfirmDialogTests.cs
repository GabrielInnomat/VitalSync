using Bunit;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Accessibility;
using VitalSync.DesignSystem.Patterns.Overlays;
using VitalSync.DesignSystem.Primitives.Actions;

namespace VitalSync.DesignSystem.Tests.Patterns.Overlays;

public sealed class ConfirmDialogTests : BunitContext
{
    private const string ModulePath = "./_content/VitalSync.DesignSystem/Patterns/Overlays/DialogFrame.razor.js";

    public ConfirmDialogTests()
    {
        Services.AddScoped<LiveAnnouncer>();
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupVoid("open", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();
        module.SetupVoid("dispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Opens_as_a_small_alertdialog_described_by_its_text()
    {
        var cut = RenderConfirm();

        var dialog = cut.Find("dialog");
        Assert.Equal("alertdialog", dialog.GetAttribute("role"));
        Assert.Equal(cut.Find("h2.vs-dialog__title").Id, dialog.GetAttribute("aria-labelledby"));
        Assert.Equal(cut.Find(".vs-dialog__content").Id, dialog.GetAttribute("aria-describedby"));
        Assert.Contains("vs-dialog--small", dialog.ClassList);
    }

    [Fact]
    public void Has_no_X()
    {
        var cut = RenderConfirm();

        Assert.Empty(cut.FindAll(".vs-dialog__close"));
    }

    [Fact]
    public void Puts_Abbrechen_first_and_marks_it_as_the_start_of_focus()
    {
        var cut = RenderConfirm(extra: parameters => parameters.Add(p => p.ConfirmVariant, ButtonVariant.Critical));

        var buttons = cut.FindAll(".vs-dialog__footer button");
        Assert.Equal(2, buttons.Count);
        Assert.Equal("Abbrechen", buttons[0].TextContent.Trim());
        Assert.Contains("vs-button--secondary", buttons[0].ClassList);
        Assert.True(buttons[0].HasAttribute("data-vs-dialog-start"));
        Assert.Equal("Löschen", buttons[1].TextContent.Trim());
        Assert.Contains("vs-button--critical", buttons[1].ClassList);
        Assert.False(buttons[1].HasAttribute("data-vs-dialog-start"));
    }

    [Fact]
    public void Defaults_to_a_primary_confirm_button()
    {
        var cut = RenderConfirm();

        Assert.Contains("vs-button--primary", cut.FindAll(".vs-dialog__footer button")[1].ClassList);
    }

    [Fact]
    public void Abbrechen_cancels_and_the_other_button_confirms()
    {
        var cancels = 0;
        var confirms = 0;
        var cut = RenderConfirm(onCancel: () => cancels++, onConfirm: () => confirms++);

        cut.FindAll(".vs-dialog__footer button")[0].Click();
        cut.FindAll(".vs-dialog__footer button")[1].Click();

        Assert.Equal(1, cancels);
        Assert.Equal(1, confirms);
    }

    [Fact]
    public async Task Reports_Escape_from_the_script_as_cancelling()
    {
        var cancels = 0;
        var confirms = 0;
        var cut = RenderConfirm(onCancel: () => cancels++, onConfirm: () => confirms++);
        var frame = cut.FindComponent<DialogFrame>();

        await cut.InvokeAsync(frame.Instance.RequestClose);

        Assert.Equal(1, cancels);
        Assert.Equal(0, confirms);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_ConfirmLabel_is_blank(string label)
    {
        Assert.Throws<InvalidOperationException>(() => RenderConfirm(confirmLabel: label));
    }

    [Fact]
    public void Throws_when_the_confirm_button_is_secondary()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RenderConfirm(extra: parameters => parameters.Add(p => p.ConfirmVariant, ButtonVariant.Secondary)));
    }

    [Fact]
    public void Throws_without_OnConfirm()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<ConfirmDialog>(parameters => parameters
                .Add(p => p.Open, true)
                .Add(p => p.Title, "Mahlzeit löschen?")
                .Add(p => p.ConfirmLabel, "Löschen")
                .Add(p => p.OnCancel, () => { })
                .AddChildContent("Die Mahlzeit wird aus dem Plan entfernt.")));
    }

    [Fact]
    public void Throws_without_OnCancel()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<ConfirmDialog>(parameters => parameters
                .Add(p => p.Open, true)
                .Add(p => p.Title, "Mahlzeit löschen?")
                .Add(p => p.ConfirmLabel, "Löschen")
                .Add(p => p.OnConfirm, () => { })
                .AddChildContent("Die Mahlzeit wird aus dem Plan entfernt.")));
    }

    [Fact]
    public void Throws_when_the_Title_is_blank()
    {
        Assert.Throws<InvalidOperationException>(() => RenderConfirm(title: " "));
    }

    [Fact]
    public void Throws_without_ChildContent()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<ConfirmDialog>(parameters => parameters
                .Add(p => p.Open, true)
                .Add(p => p.Title, "Mahlzeit löschen?")
                .Add(p => p.ConfirmLabel, "Löschen")
                .Add(p => p.OnCancel, () => { })
                .Add(p => p.OnConfirm, () => { })));
    }

    private IRenderedComponent<ConfirmDialog> RenderConfirm(
        string title = "Mahlzeit löschen?",
        string confirmLabel = "Löschen",
        Action? onCancel = null,
        Action? onConfirm = null,
        Action<ComponentParameterCollectionBuilder<ConfirmDialog>>? extra = null) =>
        Render<ConfirmDialog>(parameters =>
        {
            parameters
                .Add(p => p.Open, true)
                .Add(p => p.Title, title)
                .Add(p => p.ConfirmLabel, confirmLabel)
                .Add(p => p.OnCancel, onCancel ?? (() => { }))
                .Add(p => p.OnConfirm, onConfirm ?? (() => { }))
                .AddChildContent("Die Mahlzeit wird aus dem Plan entfernt.");
            extra?.Invoke(parameters);
        });
}
