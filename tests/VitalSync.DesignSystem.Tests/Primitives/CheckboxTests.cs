using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using VitalSync.DesignSystem.Primitives;

namespace VitalSync.DesignSystem.Tests.Primitives;

public sealed class CheckboxTests : BunitContext
{
    private sealed class TestModel
    {
        public bool Accepted { get; set; }
    }

    private static (EditContext EditContext, TestModel Model) CreateEditContext()
    {
        var model = new TestModel();
        return (new EditContext(model), model);
    }

    private static void AddError(EditContext editContext, TestModel model)
    {
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(FieldIdentifier.Create(() => model.Accepted), "Bitte bestätige die Datenschutzerklärung.");
        editContext.NotifyValidationStateChanged();
    }

    private IRenderedComponent<Checkbox> RenderCheckbox(
        EditContext editContext,
        TestModel model,
        Action<ComponentParameterCollectionBuilder<Checkbox>>? configure = null)
    {
        Expression<Func<bool>> valueExpression = () => model.Accepted;

        return Render<Checkbox>(parameters =>
        {
            parameters
                .AddCascadingValue(editContext)
                .Add(p => p.Label, "Datenschutz akzeptiert")
                .Add(p => p.Value, model.Accepted)
                .Add(p => p.ValueExpression, valueExpression)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, value => model.Accepted = value));

            configure?.Invoke(parameters);
        });
    }

    [Fact]
    public void Renders_a_native_checkbox_labelled_by_its_label()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderCheckbox(editContext, model);

        var input = cut.Find("input");
        Assert.Equal("checkbox", input.GetAttribute("type"));
        Assert.Equal(cut.Find("label").GetAttribute("for"), input.GetAttribute("id"));
        Assert.Contains("Datenschutz akzeptiert", cut.Find("label").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Throws_when_Label_is_missing()
    {
        var (editContext, model) = CreateEditContext();
        Expression<Func<bool>> valueExpression = () => model.Accepted;

        Assert.Throws<InvalidOperationException>(() =>
            Render<Checkbox>(parameters => parameters
                .AddCascadingValue(editContext)
                .Add(p => p.Value, model.Accepted)
                .Add(p => p.ValueExpression, valueExpression)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, _ => { }))));
    }

    [Fact]
    public void Reflects_the_bound_value_in_the_checked_state()
    {
        var (editContext, model) = CreateEditContext();
        model.Accepted = true;

        var cut = RenderCheckbox(editContext, model);

        Assert.True(cut.Find("input").HasAttribute("checked"));
    }

    [Fact]
    public void Raises_ValueChanged_when_toggled()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderCheckbox(editContext, model);

        cut.Find("input").Change(true);

        Assert.True(model.Accepted);
    }

    [Fact]
    public void Renders_the_check_mark_as_decoration()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderCheckbox(editContext, model);

        Assert.Equal("true", cut.Find(".vs-checkbox__mark").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Renders_the_required_marker_and_native_attribute_when_Required()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderCheckbox(editContext, model, parameters => parameters
            .Add(p => p.Required, true));

        Assert.True(cut.Find("input").HasAttribute("required"));
        Assert.Equal("true", cut.Find(".vs-checkbox__required-marker").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Renders_disabled_attribute_and_disabled_label_when_Disabled()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderCheckbox(editContext, model, parameters => parameters
            .Add(p => p.Disabled, true));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
        Assert.Contains("vs-checkbox__label--disabled", cut.Find("label").ClassList);
    }

    [Fact]
    public void Describes_the_checkbox_with_its_helper_text()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderCheckbox(editContext, model, parameters => parameters
            .Add(p => p.HelperText, "Du kannst das jederzeit widerrufen."));

        var helper = cut.Find(".vs-checkbox__helper");
        Assert.Equal("Du kannst das jederzeit widerrufen.", helper.TextContent);
        Assert.Equal(helper.GetAttribute("id"), cut.Find("input").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Renders_no_aria_describedby_without_helper_or_error_text()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderCheckbox(editContext, model);

        Assert.False(cut.Find("input").HasAttribute("aria-describedby"));
    }

    [Fact]
    public void Shows_the_validation_message_instead_of_the_helper_text_when_invalid()
    {
        var (editContext, model) = CreateEditContext();
        AddError(editContext, model);

        var cut = RenderCheckbox(editContext, model, parameters => parameters
            .Add(p => p.HelperText, "Sollte nicht angezeigt werden."));

        var error = cut.Find(".vs-checkbox__error");
        var input = cut.Find("input");
        Assert.Contains("Bitte bestätige die Datenschutzerklärung.", error.TextContent, StringComparison.Ordinal);
        Assert.Equal("alert", error.GetAttribute("role"));
        Assert.Equal("true", cut.Find(".vs-checkbox__error-icon").GetAttribute("aria-hidden"));
        Assert.Empty(cut.FindAll(".vs-checkbox__helper"));
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal(error.GetAttribute("id"), input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Registers_with_a_surrounding_Form_as_a_required_field()
    {
        var model = new TestModel();
        Expression<Func<bool>> valueExpression = () => model.Accepted;

        var cut = Render<Form>(parameters => parameters
            .Add(p => p.Model, model)
            .AddChildContent<Checkbox>(checkbox => checkbox
                .Add(p => p.Label, "Datenschutz akzeptiert")
                .Add(p => p.Required, true)
                .Add(p => p.Value, model.Accepted)
                .Add(p => p.ValueExpression, valueExpression)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, value => model.Accepted = value))));

        Assert.Equal("* = Pflichtfeld", cut.Find(".vs-form__legend").TextContent);
    }
}
