using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Primitives;

namespace VitalSync.DesignSystem.Tests.Primitives;

public sealed class FormTests : BunitContext
{
    private static string FocusedElementReferenceId(BunitJSInterop jsInterop, int invocation = 0)
    {
        var call = jsInterop.Invocations["Blazor._internal.domWrapper.focus"][invocation];
        return ((ElementReference)call.Arguments[0]!).Id;
    }

    private static string[] InputElementIds(IRenderedComponent<FormTestHost> cut) =>
        [.. cut.FindComponents<Input<string>>().Select(input => input.Instance.Element!.Value.Id)];

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
    public void Renders_an_empty_polite_live_region_before_the_first_submit_beyond_three_fields()
    {
        var cut = Render<FormTestHost>(parameters => parameters
            .Add(p => p.FieldCount, 4));

        var summary = cut.Find(".vs-form__summary");
        Assert.Equal("polite", summary.GetAttribute("aria-live"));
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
