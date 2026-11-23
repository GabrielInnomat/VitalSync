using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Accessibility;
using VitalSync.DesignSystem.Primitives.Inputs;

namespace VitalSync.DesignSystem.Tests.Primitives.Inputs;

public sealed class FormTests : BunitContext
{
    private static string FocusedElementReferenceId(BunitJSInterop jsInterop, int invocation = 0)
    {
        var call = jsInterop.Invocations["Blazor._internal.domWrapper.focus"][invocation];
        return ((ElementReference)call.Arguments[0]!).Id;
    }

    private static string[] InputElementIds(IRenderedComponent<FormTestHost> cut) =>
        [.. cut.FindComponents<Input<string>>().Select(input => input.Instance.Element!.Value.Id)];

    public FormTests()
    {
        Services.AddScoped<LiveAnnouncer>();
    }

    [Fact]
    public void Turns_off_native_validation_and_passes_attributes_to_the_form()
    {
        var cut = Render<FormTestHost>();

        var form = cut.Find("form");
        Assert.True(form.HasAttribute("novalidate"));
        Assert.Contains("test-form", form.ClassList);
    }

    [Fact]
    public void Renders_the_required_note_once_when_a_field_is_required()
    {
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.RequiredFields, true));

        var legend = Assert.Single(cut.FindAll(".vs-form__legend"));
        Assert.Equal("* = Pflichtfeld", legend.TextContent);
    }

    [Fact]
    public void Renders_no_required_note_without_required_fields()
    {
        var cut = Render<FormTestHost>();

        Assert.Empty(cut.FindAll(".vs-form__legend"));
    }

    [Fact]
    public void Moves_focus_to_the_first_field_in_error_on_an_invalid_submit()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.First, "Alex"));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            Assert.Equal(InputElementIds(cut)[1], FocusedElementReferenceId(JSInterop)));
    }

    [Fact]
    public void Renders_no_summary_up_to_three_fields()
    {
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 3));

        cut.Find("form").Submit();

        Assert.Empty(cut.FindAll(".vs-form__summary"));
    }

    [Fact]
    public void Renders_an_empty_focusable_summary_that_is_not_a_live_region_before_the_first_submit_beyond_three_fields()
    {
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4));

        var summary = cut.Find(".vs-form__summary");
        Assert.False(summary.HasAttribute("aria-live"));
        Assert.Equal("-1", summary.GetAttribute("tabindex"));
        Assert.Empty(summary.Children);
    }

    [Fact]
    public void Lists_every_field_in_error_with_a_link_to_it_beyond_three_fields()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4)
            .Add(p => p.First, "Alex"));

        cut.Find("form").Submit();

        var links = cut.FindAll(".vs-form__summary-link");
        var inputs = cut.FindAll("input");
        Assert.Equal(3, links.Count);
        Assert.Equal($"#{inputs[1].Id}", links[0].GetAttribute("href"));
        Assert.Equal("Nachname: Bitte gib einen Nachnamen ein.", links[0].TextContent);
        Assert.Contains("Bitte prüfe 3 Angaben:", cut.Find(".vs-form__summary-title").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Marks_the_summary_title_with_a_decorative_x_circle_icon()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4));

        cut.Find("form").Submit();

        var icon = cut.Find(".vs-form__summary-icon");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        Assert.StartsWith(
            "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm37.66",
            icon.QuerySelector("svg path")?.GetAttribute("d") ?? "",
            StringComparison.Ordinal);
    }

    [Fact]
    public void Links_summary_entries_to_the_field_on_the_current_page()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        Services.GetRequiredService<NavigationManager>().NavigateTo("profil/bearbeiten?schritt=2");
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4));

        cut.Find("form").Submit();

        var input = cut.FindAll("input")[0];
        Assert.Equal($"profil/bearbeiten#{input.Id}", cut.FindAll(".vs-form__summary-link")[0].GetAttribute("href"));
    }

    [Fact]
    public void Moves_focus_to_the_summary_on_an_invalid_submit_beyond_three_fields()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            var focused = Assert.Single(JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
            var id = ((ElementReference)focused.Arguments[0]!).Id;
            Assert.DoesNotContain(id, InputElementIds(cut));
        });
    }

    [Fact]
    public void Focuses_the_field_when_its_summary_link_is_followed()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4));
        cut.Find("form").Submit();

        cut.FindAll(".vs-form__summary-link")[2].Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(InputElementIds(cut)[2], FocusedElementReferenceId(JSInterop, 1)));
    }

    [Fact]
    public void Drops_a_field_from_the_summary_once_it_is_corrected()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4));
        cut.Find("form").Submit();

        cut.FindAll("input")[0].Change("Alex");

        var links = cut.FindAll(".vs-form__summary-link");
        Assert.Equal(3, links.Count);
        Assert.DoesNotContain(links, link => link.TextContent.StartsWith("Vorname", StringComparison.Ordinal));
    }

    [Fact]
    public void Keeps_the_summary_unchanged_while_typing_and_announces_the_fix_with_the_remaining_count_once_committed()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var region = Render<LiveAnnouncerRegion>();
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4));
        cut.Find("form").Submit();

        cut.FindAll("input")[0].Input("Alex");
        Assert.Equal(4, cut.FindAll(".vs-form__summary-link").Count);
        Assert.Empty(region.FindAll("[role=status] > div"));

        cut.FindAll("input")[0].Change("Alex");
        Assert.Equal(3, cut.FindAll(".vs-form__summary-link").Count);
        var message = Assert.Single(region.FindAll("[role=status] > div"));
        Assert.Equal("Vorname: in Ordnung. Noch 3 Angaben.", message.TextContent);
    }

    [Fact]
    public void Announces_a_new_summary_entry_with_its_message_and_the_remaining_count()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var region = Render<LiveAnnouncerRegion>();
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4)
            .Add(p => p.First, "Alex"));
        cut.Find("form").Submit();

        cut.FindAll("input")[0].Change("");

        var message = Assert.Single(region.FindAll("[role=status] > div"));
        Assert.Equal("Vorname: Bitte gib einen Vornamen ein. Noch 4 Angaben.", message.TextContent);
    }

    [Fact]
    public void Announces_that_all_entries_are_in_order_once_the_last_summary_entry_is_fixed()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var region = Render<LiveAnnouncerRegion>();
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4));
        cut.Find("form").Submit();

        string[] values = ["Alex", "Muster", "Bern", "Schweiz"];
        for (var i = 0; i < values.Length; i++)
        {
            cut.FindAll("input")[i].Change(values[i]);
        }

        var messages = region.FindAll("[role=status] > div");
        Assert.Equal(4, messages.Count);
        Assert.Equal("Alle Angaben in Ordnung.", messages[^1].TextContent);
        Assert.Empty(cut.FindAll(".vs-form__summary-link"));
    }

    [Fact]
    public void Announces_nothing_when_a_committed_change_leaves_the_summary_as_it_was()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4));
        cut.Find("form").Submit();

        cut.FindAll("input")[0].Change(" ");

        Assert.Empty(Services.GetRequiredService<LiveAnnouncer>().Announcements);
    }

    [Fact]
    public void Announces_an_error_when_a_field_is_left_after_typing_even_if_its_content_ends_where_it_started()
    {
        var region = Render<LiveAnnouncerRegion>();
        var cut = Render<FormTestHost>();

        cut.FindAll("input")[0].Input("Alex");
        cut.FindAll("input")[0].Input("");
        cut.FindAll("input")[0].Blur();

        Assert.Equal("true", cut.FindAll("input")[0].GetAttribute("aria-invalid"));
        var message = Assert.Single(region.FindAll("[role=status] > div"));
        Assert.Equal("Vorname: Bitte gib einen Vornamen ein.", message.TextContent);
    }

    [Fact]
    public void Announces_a_new_field_error_with_its_label_once_the_field_is_committed()
    {
        var region = Render<LiveAnnouncerRegion>();
        var cut = Render<FormTestHost>();
        cut.FindAll("input")[0].Change("Alex");

        cut.FindAll("input")[0].Change("");

        var message = Assert.Single(region.FindAll("[role=status] > div"));
        Assert.Equal("Vorname: Bitte gib einen Vornamen ein.", message.TextContent);
        Assert.Empty(region.FindAll("[role=alert] > div"));
    }

    [Fact]
    public void Does_not_announce_an_error_that_is_already_shown()
    {
        var cut = Render<FormTestHost>();
        cut.FindAll("input")[0].Change("Alex");
        cut.FindAll("input")[0].Change("");

        cut.FindAll("input")[0].Change(" ");

        Assert.Single(Services.GetRequiredService<LiveAnnouncer>().Announcements);
    }

    [Fact]
    public void Submits_a_valid_form_and_clears_the_summary()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4));
        cut.Find("form").Submit();

        var inputs = cut.FindAll("input");
        string[] values = ["Alex", "Muster", "Bern", "Schweiz"];
        for (var i = 0; i < values.Length; i++)
        {
            inputs[i].Change(values[i]);
        }

        cut.Find("form").Submit();

        Assert.Equal(1, cut.Instance.ValidSubmits);
        Assert.Empty(cut.Find(".vs-form__summary").Children);
    }

    [Fact]
    public void Shows_the_clicked_submit_button_as_loading_while_a_valid_submit_runs()
    {
        var cut = Render<FormSubmitTestHost>();

        _ = cut.Find(".draft").ClickAsync();

        Assert.Equal(1, cut.Instance.ValidSubmits);
        Assert.Contains("vs-button--loading", cut.Find(".draft").ClassList);
        Assert.Equal("button", cut.Find(".draft").GetAttribute("type"));
        Assert.DoesNotContain("vs-button--loading", cut.Find(".save").ClassList);
    }

    [Fact]
    public async Task Ends_the_loading_state_when_the_valid_submit_completes()
    {
        var cut = Render<FormSubmitTestHost>();

        _ = cut.Find(".save").ClickAsync();
        await cut.InvokeAsync(() => cut.Instance.Pending.SetResult());

        cut.WaitForAssertion(() => Assert.DoesNotContain("vs-button--loading", cut.Find(".save").ClassList));
        Assert.Equal("submit", cut.Find(".save").GetAttribute("type"));
    }

    [Fact]
    public async Task Ends_the_loading_state_and_lets_the_exception_through_when_the_valid_submit_fails()
    {
        var cut = Render<FormSubmitTestHost>();

        _ = cut.Find(".save").ClickAsync();
        await cut.InvokeAsync(() => cut.Instance.Pending.SetException(new InvalidOperationException("Speichern fehlgeschlagen")));

        Assert.IsType<InvalidOperationException>(await Renderer.UnhandledException);
        Assert.DoesNotContain("vs-button--loading", cut.Find(".save").ClassList);
    }

    [Fact]
    public void Ignores_a_second_submit_while_the_first_still_runs()
    {
        var cut = Render<FormSubmitTestHost>();

        _ = cut.Find(".save").ClickAsync();
        _ = cut.Find(".draft").ClickAsync();
        _ = cut.Find("form").SubmitAsync();

        Assert.Equal(1, cut.Instance.ValidSubmits);
        Assert.Contains("vs-button--loading", cut.Find(".save").ClassList);
        Assert.DoesNotContain("vs-button--loading", cut.Find(".draft").ClassList);
    }

    [Fact]
    public void Shows_no_loading_state_on_an_invalid_submit_and_forgets_the_clicked_button()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var cut = Render<FormSubmitTestHost>(parameters => parameters
            .Add(p => p.Name, null));

        cut.Find(".save").Click();

        Assert.Equal(0, cut.Instance.ValidSubmits);
        Assert.DoesNotContain("vs-button--loading", cut.Find(".save").ClassList);

        cut.Find("input").Change("Anna");
        _ = cut.Find("form").SubmitAsync();

        Assert.Equal(1, cut.Instance.ValidSubmits);
        Assert.Empty(cut.FindAll(".vs-button--loading"));
    }

    [Fact]
    public void Shows_no_loading_state_when_a_submit_was_not_started_by_a_button()
    {
        var cut = Render<FormSubmitTestHost>();

        _ = cut.Find("form").SubmitAsync();

        Assert.Equal(1, cut.Instance.ValidSubmits);
        Assert.Empty(cut.FindAll(".vs-button--loading"));
    }

    [Fact]
    public void Throws_without_a_Model_or_an_EditContext()
    {
        Assert.Throws<InvalidOperationException>(() => Render<Form>());
    }

    [Fact]
    public void Throws_with_both_a_Model_and_an_EditContext()
    {
        var model = new object();

        Assert.Throws<InvalidOperationException>(() => Render<Form>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.EditContext, new EditContext(model))));
    }
}
