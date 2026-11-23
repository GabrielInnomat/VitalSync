using Bunit;
using Microsoft.AspNetCore.Components;
using VitalSync.DesignSystem.Primitives.Inputs;

namespace VitalSync.DesignSystem.Tests.Primitives.Inputs;

public sealed class ToggleTests : BunitContext
{
    private bool _value;

    private IRenderedComponent<Toggle> RenderToggle(Action<ComponentParameterCollectionBuilder<Toggle>>? configure = null) =>
        Render<Toggle>(parameters =>
        {
            parameters
                .Add(p => p.Label, "Benachrichtigungen")
                .Add(p => p.Value, _value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, value => _value = value));

            configure?.Invoke(parameters);
        });

    [Fact]
    public void Renders_a_native_checkbox_with_the_switch_role_labelled_by_its_label()
    {
        var cut = RenderToggle();

        var input = cut.Find("input");
        Assert.Equal("checkbox", input.GetAttribute("type"));
        Assert.Equal("switch", input.GetAttribute("role"));
        Assert.Equal(cut.Find("label").GetAttribute("for"), input.GetAttribute("id"));
        Assert.Equal("Benachrichtigungen", cut.Find(".vs-toggle__text").TextContent);
    }

    [Fact]
    public void Throws_when_Label_is_missing()
    {
        Assert.Throws<InvalidOperationException>(() => Render<Toggle>());
    }

    [Fact]
    public void Shows_off_state_as_hidden_text_when_off()
    {
        var cut = RenderToggle();

        var state = cut.Find(".vs-toggle__state");
        Assert.False(cut.Find("input").HasAttribute("checked"));
        Assert.Equal("Aus", state.TextContent);
        Assert.Equal("true", state.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Shows_on_state_as_hidden_text_when_on()
    {
        _value = true;

        var cut = RenderToggle();

        Assert.True(cut.Find("input").HasAttribute("checked"));
        Assert.Equal("Ein", cut.Find(".vs-toggle__state").TextContent);
    }

    [Fact]
    public void Raises_ValueChanged_and_updates_the_state_text_when_switched()
    {
        var cut = RenderToggle();

        cut.Find("input").Change(true);

        Assert.True(_value);
        Assert.Equal("Ein", cut.Find(".vs-toggle__state").TextContent);
    }

    [Fact]
    public void Renders_thumb_and_check_mark_as_decoration()
    {
        var cut = RenderToggle();

        Assert.Equal("true", cut.Find(".vs-toggle__thumb").GetAttribute("aria-hidden"));
        Assert.Equal("true", cut.Find(".vs-toggle__mark").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Renders_disabled_attribute_and_disabled_label_when_Disabled()
    {
        var cut = RenderToggle(parameters => parameters.Add(p => p.Disabled, true));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
        Assert.Contains("vs-toggle__label--disabled", cut.Find("label").ClassList);
    }

    [Fact]
    public void Describes_the_toggle_with_its_helper_text()
    {
        var cut = RenderToggle(parameters => parameters.Add(p => p.HelperText, "Gilt für alle Geräte."));

        var helper = cut.Find(".vs-toggle__helper");
        Assert.Equal("Gilt für alle Geräte.", helper.TextContent);
        Assert.Equal(helper.GetAttribute("id"), cut.Find("input").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Renders_no_aria_describedby_without_helper_text()
    {
        var cut = RenderToggle();

        Assert.False(cut.Find("input").HasAttribute("aria-describedby"));
    }

    [Fact]
    public void Keeps_its_own_id_and_role_over_additional_attributes()
    {
        var cut = RenderToggle(parameters => parameters
            .AddUnmatched("id", "fremd")
            .AddUnmatched("role", "checkbox")
            .AddUnmatched("data-test", "toggle"));

        var input = cut.Find("input");
        Assert.Equal(cut.Find("label").GetAttribute("for"), input.GetAttribute("id"));
        Assert.Equal("switch", input.GetAttribute("role"));
        Assert.Equal("toggle", input.GetAttribute("data-test"));
    }

    [Fact]
    public void Labels_the_switch_from_its_text_and_its_row_and_keeps_the_help_button_outside_both()
    {
        var cut = RenderToggle(parameters => parameters.Add(p => p.Info, "Erinnert dich an geplante Trainings."));

        var input = cut.Find("input");
        var labels = cut.FindAll("label");
        Assert.Equal(2, labels.Count);
        Assert.All(labels, label => Assert.Equal(input.Id, label.GetAttribute("for")));
        Assert.All(labels, label => Assert.Empty(label.QuerySelectorAll("button")));
        Assert.Equal("Hilfe zu Benachrichtigungen", cut.Find(".vs-toggletip__name").TextContent);
    }
}
