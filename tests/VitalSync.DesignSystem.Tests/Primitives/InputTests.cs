using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using VitalSync.DesignSystem.Primitives;

namespace VitalSync.DesignSystem.Tests.Primitives;

public sealed class InputTests : BunitContext
{
    private sealed class TestModel
    {
        public string Name { get; set; } = "";
    }

    private static (EditContext EditContext, TestModel Model) CreateEditContext()
    {
        var model = new TestModel();
        return (new EditContext(model), model);
    }

    private IRenderedComponent<Input<string>> RenderInput(
        EditContext editContext,
        TestModel model,
        Action<ComponentParameterCollectionBuilder<Input<string>>>? configure = null)
    {
        Expression<Func<string>> valueExpression = () => model.Name;

        return Render<Input<string>>(parameters =>
        {
            parameters
                .AddCascadingValue(editContext)
                .Add(p => p.Label, "Name")
                .Add(p => p.Value, model.Name)
                .Add(p => p.ValueExpression, valueExpression)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, value => model.Name = value));

            configure?.Invoke(parameters);
        });
    }

    [Fact]
    public void Renders_the_label_text()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderInput(editContext, model);

        Assert.Contains("Name", cut.Find(".vs-input__label").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Throws_when_Label_is_missing()
    {
        var (editContext, model) = CreateEditContext();
        Expression<Func<string>> valueExpression = () => model.Name;

        Assert.Throws<InvalidOperationException>(() =>
            Render<Input<string>>(parameters => parameters
                .AddCascadingValue(editContext)
                .Add(p => p.Value, model.Name)
                .Add(p => p.ValueExpression, valueExpression)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, _ => { }))));
    }

    [Fact]
    public void Renders_the_required_marker_and_native_attribute_when_Required()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderInput(editContext, model, parameters => parameters
            .Add(p => p.Required, true));

        Assert.True(cut.Find("input").HasAttribute("required"));
        Assert.NotNull(cut.Find(".vs-input__required-marker"));
    }

    [Fact]
    public void Raises_ValueChanged_when_the_user_types()
    {
        string? changedTo = null;
        var (editContext, model) = CreateEditContext();
        Expression<Func<string>> valueExpression = () => model.Name;

        var cut = Render<Input<string>>(parameters => parameters
            .AddCascadingValue(editContext)
            .Add(p => p.Label, "Name")
            .Add(p => p.Value, model.Name)
            .Add(p => p.ValueExpression, valueExpression)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, value => changedTo = value)));

        cut.Find("input").Input("Alex");

        Assert.Equal("Alex", changedTo);
    }

    [Fact]
    public void Renders_the_prefix_and_suffix_when_given()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderInput(editContext, model, parameters => parameters
            .Add(p => p.Prefix, "kg")
            .Add(p => p.Suffix, "kcal"));

        Assert.Equal("kg", cut.Find(".vs-input__affix--prefix").TextContent);
        Assert.Equal("kcal", cut.Find(".vs-input__affix--suffix").TextContent);
    }

    [Fact]
    public void Renders_a_visible_password_reveal_toggle_for_password_inputs()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderInput(editContext, model, parameters => parameters
            .Add(p => p.Type, "password"));

        var toggle = cut.Find(".vs-input__toggle");
        Assert.Equal("Anzeigen", toggle.TextContent);
        Assert.Equal("password", cut.Find("input").GetAttribute("type"));

        toggle.Click();

        Assert.Equal("Verbergen", cut.Find(".vs-input__toggle").TextContent);
        Assert.Equal("text", cut.Find("input").GetAttribute("type"));
    }

    [Fact]
    public void Does_not_render_a_toggle_for_non_password_inputs()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderInput(editContext, model);

        Assert.Empty(cut.FindAll(".vs-input__toggle"));
    }

    [Fact]
    public void Renders_disabled_attribute_when_Disabled_is_true()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderInput(editContext, model, parameters => parameters
            .Add(p => p.Disabled, true));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void Shows_the_validation_message_instead_of_the_helper_text_when_the_field_is_invalid()
    {
        var (editContext, model) = CreateEditContext();
        var fieldIdentifier = FieldIdentifier.Create(() => model.Name);
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(fieldIdentifier, "Bitte gib einen Namen ein.");
        editContext.NotifyValidationStateChanged();

        var cut = RenderInput(editContext, model, parameters => parameters
            .Add(p => p.HelperText, "Sollte nicht angezeigt werden."));

        var error = cut.Find(".vs-input__error");
        Assert.Contains("Bitte gib einen Namen ein.", error.TextContent, StringComparison.Ordinal);
        Assert.Equal("alert", error.GetAttribute("role"));
        Assert.Empty(cut.FindAll(".vs-input__helper"));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Shows_the_helper_text_when_there_is_no_error()
    {
        var (editContext, model) = CreateEditContext();

        var cut = RenderInput(editContext, model, parameters => parameters
            .Add(p => p.HelperText, "Wie du angesprochen werden möchtest."));

        Assert.Equal("Wie du angesprochen werden möchtest.", cut.Find(".vs-input__helper").TextContent);
        Assert.Empty(cut.FindAll(".vs-input__error"));
    }
}
