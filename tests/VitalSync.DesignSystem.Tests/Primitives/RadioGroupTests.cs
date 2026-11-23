using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Primitives;

namespace VitalSync.DesignSystem.Tests.Primitives;

public sealed class RadioGroupTests : BunitContext
{
    private sealed class TestModel
    {
        [Required(ErrorMessage = "Bitte wähle ein Aktivitätsniveau.")]
        public string? Level { get; set; }
    }

    private static (EditContext EditContext, TestModel Model) CreateEditContext()
    {
        var model = new TestModel();
        return (new EditContext(model), model);
    }

    private static ValidationMessageStore AddError(EditContext editContext, TestModel model)
    {
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(FieldIdentifier.Create(() => model.Level), "Bitte wähle ein Aktivitätsniveau.");
        editContext.NotifyValidationStateChanged();
        return messageStore;
    }

    private static string OptionElementId(IRenderedComponent<Form> cut, int index) =>
        cut.FindComponents<RadioOption<string?>>()[index].Instance.Element!.Value.Id;

    private void AddGroupParameters(
        ComponentParameterCollectionBuilder<RadioGroup<string?>> parameters,
        TestModel model,
        string label = "Aktivitätsniveau")
    {
        Expression<Func<string?>> valueExpression = () => model.Level;

        parameters
            .Add(p => p.Label, label)
            .Add(p => p.Value, model.Level)
            .Add(p => p.ValueExpression, valueExpression)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Level = value))
            .AddChildContent<RadioOption<string?>>(option => option
                .Add(p => p.Value, "low")
                .Add(p => p.Label, "Gering"))
            .AddChildContent<RadioOption<string?>>(option => option
                .Add(p => p.Value, "high")
                .Add(p => p.Label, "Hoch"));
    }

    private IRenderedComponent<RadioGroup<string?>> RenderGroup(
        EditContext editContext,
        TestModel model,
        Action<ComponentParameterCollectionBuilder<RadioGroup<string?>>>? configure = null) =>
        Render<RadioGroup<string?>>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            AddGroupParameters(parameters, model);
            configure?.Invoke(parameters);
        });

    public RadioGroupTests()
    {
        Services.AddScoped<LiveAnnouncer>();
    }

    [Fact]
    public void Renders_a_fieldset_named_by_its_legend()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderGroup(editContext, model);

        Assert.Equal("Aktivitätsniveau", cut.Find("fieldset > legend").TextContent.Trim());
    }

    [Fact]
    public void Renders_native_radios_that_share_a_name_and_are_labelled_by_their_label()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderGroup(editContext, model);

        var inputs = cut.FindAll("input");
        var labels = cut.FindAll("label");
        Assert.Equal(2, inputs.Count);
        Assert.All(inputs, input => Assert.Equal("radio", input.GetAttribute("type")));
        Assert.Equal(inputs[0].GetAttribute("name"), inputs[1].GetAttribute("name"));
        Assert.NotEqual(inputs[0].Id, inputs[1].Id);
        Assert.Equal(inputs[1].Id, labels[1].GetAttribute("for"));
        Assert.Contains("Hoch", labels[1].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Throws_when_Label_is_missing()
    {
        var (editContext, model) = CreateEditContext();

        Assert.Throws<InvalidOperationException>(() =>
            Render<RadioGroup<string?>>(parameters =>
            {
                parameters.AddCascadingValue(editContext);
                AddGroupParameters(parameters, model, label: "");
            }));
    }

    [Fact]
    public void Throws_when_an_option_has_no_Label()
    {
        var (editContext, model) = CreateEditContext();

        Assert.Throws<InvalidOperationException>(() =>
            RenderGroup(editContext, model, parameters => parameters
                .AddChildContent<RadioOption<string?>>(option => option.Add(p => p.Value, "medium"))));
    }

    [Fact]
    public void Throws_when_an_option_is_used_outside_a_group()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<RadioOption<string>>(parameters => parameters
                .Add(p => p.Value, "low")
                .Add(p => p.Label, "Gering")));
    }

    [Fact]
    public void Checks_the_option_that_matches_the_bound_value()
    {
        var (editContext, model) = CreateEditContext();
        model.Level = "high";

        var cut = RenderGroup(editContext, model);

        var inputs = cut.FindAll("input");
        Assert.False(inputs[0].HasAttribute("checked"));
        Assert.True(inputs[1].HasAttribute("checked"));
    }

    [Fact]
    public void Raises_ValueChanged_with_the_value_of_the_chosen_option()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderGroup(editContext, model);

        cut.FindAll("input")[1].Change("on");

        Assert.Equal("high", model.Level);
    }

    [Fact]
    public void Renders_the_dot_as_decoration()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderGroup(editContext, model);

        Assert.All(cut.FindAll(".vs-radio__mark"), mark => Assert.Equal("true", mark.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void Renders_the_required_marker_on_the_legend_and_the_native_attribute_on_every_option_when_Required()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderGroup(editContext, model, parameters => parameters.Add(p => p.Required, true));

        Assert.All(cut.FindAll("input"), input => Assert.True(input.HasAttribute("required")));
        Assert.Equal("true", cut.Find("legend .vs-radio-group__required-marker").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Disables_the_fieldset_and_every_option_when_Disabled()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderGroup(editContext, model, parameters => parameters.Add(p => p.Disabled, true));

        Assert.True(cut.Find("fieldset").HasAttribute("disabled"));
        Assert.All(cut.FindAll("input"), input => Assert.True(input.HasAttribute("disabled")));
        Assert.All(cut.FindAll("label"), label => Assert.Contains("vs-radio__label--disabled", label.ClassList));
    }

    [Fact]
    public void Describes_the_group_with_its_helper_text()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderGroup(editContext, model, parameters => parameters
            .Add(p => p.HelperText, "Wie aktiv bist du im Alltag?"));

        var helper = cut.Find(".vs-radio-group__helper");
        Assert.Equal("Wie aktiv bist du im Alltag?", helper.TextContent);
        Assert.Equal(helper.Id, cut.Find("fieldset").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Renders_no_aria_describedby_without_helper_or_error_text()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderGroup(editContext, model);

        Assert.False(cut.Find("fieldset").HasAttribute("aria-describedby"));
    }

    [Fact]
    public void Shows_the_validation_message_on_the_group_and_marks_every_option_invalid()
    {
        var (editContext, model) = CreateEditContext();
        AddError(editContext, model);

        var cut = RenderGroup(editContext, model, parameters => parameters
            .Add(p => p.HelperText, "Sollte nicht angezeigt werden."));

        var error = cut.Find(".vs-radio-group__error");
        Assert.Contains("Bitte wähle ein Aktivitätsniveau.", error.TextContent, StringComparison.Ordinal);
        Assert.False(error.HasAttribute("role"));
        Assert.Equal("true", cut.Find(".vs-radio-group__error-icon").GetAttribute("aria-hidden"));
        Assert.Empty(cut.FindAll(".vs-radio-group__helper"));
        Assert.Equal(error.Id, cut.Find("fieldset").GetAttribute("aria-describedby"));
        Assert.All(cut.FindAll("input"), input => Assert.Equal("true", input.GetAttribute("aria-invalid")));
        Assert.False(cut.Find("fieldset").HasAttribute("aria-invalid"));
    }

    [Fact]
    public void Clears_the_error_state_on_every_option_once_the_error_is_gone()
    {
        var (editContext, model) = CreateEditContext();
        var messageStore = AddError(editContext, model);
        var cut = RenderGroup(editContext, model);

        cut.InvokeAsync(() =>
        {
            messageStore.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.Empty(cut.FindAll(".vs-radio-group__error"));
        Assert.All(cut.FindAll("input"), input => Assert.Equal("false", input.GetAttribute("aria-invalid")));
    }

    [Fact]
    public void Moves_focus_to_the_first_option_on_an_invalid_submit_inside_a_Form()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var model = new TestModel();
        var editContext = new EditContext(model);
        editContext.OnValidationRequested += (_, _) => AddError(editContext, model);

        var cut = Render<Form>(parameters => parameters
            .Add(p => p.EditContext, editContext)
            .AddChildContent<RadioGroup<string?>>(group =>
            {
                AddGroupParameters(group, model);
                group.Add(p => p.Required, true);
            }));

        Assert.Equal("* = Pflichtfeld", cut.Find(".vs-form__legend").TextContent);

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            var call = Assert.Single(JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
            Assert.Equal(
                OptionElementId(cut, 0),
                ((ElementReference)call.Arguments[0]!).Id);
        });
    }

    [Fact]
    public void Moves_focus_to_the_checked_option_on_an_invalid_submit_inside_a_Form()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var model = new TestModel { Level = "high" };
        var editContext = new EditContext(model);
        editContext.OnValidationRequested += (_, _) => AddError(editContext, model);

        var cut = Render<Form>(parameters => parameters
            .Add(p => p.EditContext, editContext)
            .AddChildContent<RadioGroup<string?>>(group => AddGroupParameters(group, model)));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            var call = Assert.Single(JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
            Assert.Equal(
                OptionElementId(cut, 1),
                ((ElementReference)call.Arguments[0]!).Id);
        });
    }

    [Fact]
    public void Announces_that_the_group_is_in_order_once_a_choice_clears_its_error()
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var region = Render<LiveAnnouncerRegion>();
        var model = new TestModel();
        var cut = Render<Form>(parameters => parameters
            .Add(p => p.Model, model)
            .AddChildContent<DataAnnotationsValidator>()
            .AddChildContent<RadioGroup<string?>>(group => AddGroupParameters(group, model)));
        cut.Find("form").Submit();

        cut.FindAll("input")[1].Change("on");

        var message = Assert.Single(region.FindAll("[role=status] > div"));
        Assert.Equal("Aktivitätsniveau: in Ordnung.", message.TextContent);
        Assert.Empty(region.FindAll("[role=alert] > div"));
    }

    [Fact]
    public void Names_the_group_by_its_label_alone_when_Info_puts_a_help_button_into_the_legend()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderGroup(editContext, model, parameters => parameters.Add(p => p.Info, "Wie viel du dich im Alltag bewegst."));

        var name = cut.Find($"#{cut.Find("fieldset").GetAttribute("aria-labelledby")}");
        Assert.Equal("Aktivitätsniveau", name.TextContent.Trim());
        Assert.Empty(name.QuerySelectorAll("button"));
        Assert.Single(cut.FindAll("fieldset > legend .vs-toggletip__button"));
    }
}
