using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using VitalSync.DesignSystem.Primitives;

namespace VitalSync.DesignSystem.Tests.Primitives;

public sealed class SelectTests : BunitContext
{
    private enum Goal
    {
        Lose,
        Keep,
    }

    private sealed class TestModel
    {
        public string? Diet { get; set; }
    }

    private sealed class GoalModel
    {
        public Goal Goal { get; set; }
    }

    private static (EditContext EditContext, TestModel Model) CreateEditContext()
    {
        var model = new TestModel();
        return (new EditContext(model), model);
    }

    private static ValidationMessageStore AddError(EditContext editContext, TestModel model)
    {
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(FieldIdentifier.Create(() => model.Diet), "Bitte wähle eine Ernährungsform.");
        editContext.NotifyValidationStateChanged();
        return messageStore;
    }

    private void AddSelectParameters(
        ComponentParameterCollectionBuilder<Select<string?>> parameters,
        TestModel model,
        string label = "Ernährungsform")
    {
        Expression<Func<string?>> valueExpression = () => model.Diet;

        parameters
            .Add(p => p.Label, label)
            .Add(p => p.Value, model.Diet)
            .Add(p => p.ValueExpression, valueExpression)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Diet = value))
            .AddChildContent<SelectOption<string?>>(option => option
                .Add(p => p.Value, "mixed")
                .Add(p => p.Label, "Mischkost"))
            .AddChildContent<SelectOption<string?>>(option => option
                .Add(p => p.Value, "vegan")
                .Add(p => p.Label, "Vegan"));
    }

    private IRenderedComponent<Select<string?>> RenderSelect(
        EditContext editContext,
        TestModel model,
        Action<ComponentParameterCollectionBuilder<Select<string?>>>? configure = null) =>
        Render<Select<string?>>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            AddSelectParameters(parameters, model);
            configure?.Invoke(parameters);
        });

    [Fact]
    public void Renders_a_native_select_labelled_by_its_label()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderSelect(editContext, model);

        var select = cut.Find("select");
        Assert.Equal(select.Id, cut.Find("label").GetAttribute("for"));
        Assert.Contains("Ernährungsform", cut.Find("label").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Throws_when_Label_is_missing()
    {
        var (editContext, model) = CreateEditContext();

        Assert.Throws<InvalidOperationException>(() =>
            Render<Select<string?>>(parameters =>
            {
                parameters.AddCascadingValue(editContext);
                AddSelectParameters(parameters, model, label: "");
            }));
    }

    [Fact]
    public void Throws_when_an_option_has_no_Label_or_no_Value()
    {
        var (editContext, model) = CreateEditContext();

        Assert.Throws<InvalidOperationException>(() =>
            RenderSelect(editContext, model, parameters => parameters
                .AddChildContent<SelectOption<string?>>(option => option.Add(p => p.Value, "keto"))));
        Assert.Throws<InvalidOperationException>(() =>
            RenderSelect(editContext, model, parameters => parameters
                .AddChildContent<SelectOption<string?>>(option => option.Add(p => p.Label, "Leer"))));
    }

    [Fact]
    public void Throws_when_an_option_is_used_outside_a_select()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<SelectOption<string>>(parameters => parameters
                .Add(p => p.Value, "vegan")
                .Add(p => p.Label, "Vegan")));
    }

    [Fact]
    public void Starts_with_the_empty_choice_shown_as_placeholder()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderSelect(editContext, model);

        var options = cut.FindAll("option");
        Assert.Equal(3, options.Count);
        Assert.Equal("", options[0].GetAttribute("value"));
        Assert.Equal("Bitte wählen", options[0].TextContent);
        Assert.Equal("", cut.Find("select").GetAttribute("value"));
        Assert.Contains("vs-select__control--empty", cut.Find("select").ClassList);
    }

    [Fact]
    public void Shows_the_bound_value_and_a_custom_placeholder()
    {
        var (editContext, model) = CreateEditContext();
        model.Diet = "vegan";

        var cut = RenderSelect(editContext, model, parameters => parameters.Add(p => p.Placeholder, "Ernährungsform wählen"));

        Assert.Equal("Ernährungsform wählen", cut.FindAll("option")[0].TextContent);
        Assert.Equal("vegan", cut.Find("select").GetAttribute("value"));
        Assert.DoesNotContain("vs-select__control--empty", cut.Find("select").ClassList);
    }

    [Fact]
    public void Raises_ValueChanged_with_the_value_of_the_chosen_option_and_back_to_null_for_the_empty_choice()
    {
        var (editContext, model) = CreateEditContext();
        var cut = RenderSelect(editContext, model);

        cut.Find("select").Change("vegan");
        Assert.Equal("vegan", model.Diet);

        cut.Find("select").Change("");
        Assert.Null(model.Diet);
    }

    [Fact]
    public void Binds_an_enum_and_reports_the_empty_choice_as_an_error_when_the_type_cannot_be_empty()
    {
        var model = new GoalModel { Goal = Goal.Keep };
        var editContext = new EditContext(model);
        Expression<Func<Goal>> valueExpression = () => model.Goal;

        var cut = Render<Select<Goal>>(parameters => parameters
            .AddCascadingValue(editContext)
            .Add(p => p.Label, "Ziel")
            .Add(p => p.Value, model.Goal)
            .Add(p => p.ValueExpression, valueExpression)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<Goal>(this, value => model.Goal = value))
            .AddChildContent<SelectOption<Goal>>(option => option
                .Add(p => p.Value, Goal.Lose)
                .Add(p => p.Label, "Abnehmen"))
            .AddChildContent<SelectOption<Goal>>(option => option
                .Add(p => p.Value, Goal.Keep)
                .Add(p => p.Label, "Halten")));

        Assert.Equal("Keep", cut.Find("select").GetAttribute("value"));

        cut.Find("select").Change("Lose");
        Assert.Equal(Goal.Lose, model.Goal);

        cut.Find("select").Change("");
        Assert.Equal(Goal.Lose, model.Goal);
        Assert.Equal("Bitte wähle bei Ziel eine Option.", Assert.Single(editContext.GetValidationMessages()));
    }

    [Fact]
    public void Renders_the_arrow_as_decoration()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderSelect(editContext, model);

        Assert.Equal("true", cut.Find(".vs-select__icon").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Renders_the_required_marker_and_native_attribute_when_Required()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderSelect(editContext, model, parameters => parameters.Add(p => p.Required, true));

        Assert.True(cut.Find("select").HasAttribute("required"));
        Assert.Equal("true", cut.Find("label .vs-select__required-marker").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void States_not_invalid_while_a_required_select_is_still_untouched()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderSelect(editContext, model, parameters => parameters.Add(p => p.Required, true));

        Assert.Equal("false", cut.Find("select").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Renders_disabled_attribute_when_Disabled_is_true()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderSelect(editContext, model, parameters => parameters.Add(p => p.Disabled, true));

        Assert.True(cut.Find("select").HasAttribute("disabled"));
    }

    [Fact]
    public void Describes_the_select_with_its_helper_text()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderSelect(editContext, model, parameters => parameters
            .Add(p => p.HelperText, "Grundlage für deine Rezeptvorschläge."));

        var helper = cut.Find(".vs-select__helper");
        Assert.Equal("Grundlage für deine Rezeptvorschläge.", helper.TextContent);
        Assert.Equal(helper.Id, cut.Find("select").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Renders_no_aria_describedby_without_helper_or_error_text()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderSelect(editContext, model);

        Assert.False(cut.Find("select").HasAttribute("aria-describedby"));
    }

    [Fact]
    public void Shows_the_validation_message_instead_of_the_helper_text_when_the_select_is_invalid()
    {
        var (editContext, model) = CreateEditContext();
        AddError(editContext, model);

        var cut = RenderSelect(editContext, model, parameters => parameters
            .Add(p => p.HelperText, "Sollte nicht angezeigt werden."));

        var error = cut.Find(".vs-select__error");
        var select = cut.Find("select");
        Assert.Contains("Bitte wähle eine Ernährungsform.", error.TextContent, StringComparison.Ordinal);
        Assert.Equal("alert", error.GetAttribute("role"));
        Assert.Equal("true", cut.Find(".vs-select__error-icon").GetAttribute("aria-hidden"));
        Assert.Empty(cut.FindAll(".vs-select__helper"));
        Assert.Equal(error.Id, select.GetAttribute("aria-describedby"));
        Assert.Equal("true", select.GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Keeps_the_label_association_when_an_id_is_passed_as_an_attribute()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderSelect(editContext, model, parameters => parameters
            .AddUnmatched("id", "custom-id")
            .AddUnmatched("autocomplete", "off"));

        var select = cut.Find("select");
        Assert.Equal(cut.Find("label").GetAttribute("for"), select.GetAttribute("id"));
        Assert.Equal("off", select.GetAttribute("autocomplete"));
    }

    [Fact]
    public void Moves_focus_to_the_select_on_an_invalid_submit_inside_a_Form()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var model = new TestModel();
        var editContext = new EditContext(model);
        editContext.OnValidationRequested += (_, _) => AddError(editContext, model);

        var cut = Render<Form>(parameters => parameters
            .Add(p => p.EditContext, editContext)
            .AddChildContent<Select<string?>>(select =>
            {
                AddSelectParameters(select, model);
                select.Add(p => p.Required, true);
            }));

        Assert.Equal("* = Pflichtfeld", cut.Find(".vs-form__legend").TextContent);

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            var call = Assert.Single(JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
            Assert.Equal(
                cut.FindComponent<Select<string?>>().Instance.Element!.Value.Id,
                ((ElementReference)call.Arguments[0]!).Id);
        });
    }

    [Fact]
    public void Renders_a_help_button_beside_the_label_but_outside_it_when_Info_is_set()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderSelect(editContext, model, parameters => parameters.Add(p => p.Info, "Bestimmt, welche Rezepte du siehst."));

        Assert.Empty(cut.Find("label").QuerySelectorAll("button"));
        Assert.Equal("Hilfe zu Ernährungsform", cut.Find(".vs-select__label-row .vs-toggletip__name").TextContent);
    }
}
