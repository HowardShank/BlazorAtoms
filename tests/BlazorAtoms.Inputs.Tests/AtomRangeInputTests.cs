using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorAtoms.Inputs.Tests;

public class AtomRangeInputTests : TestContext
{
    private sealed class TestModel
    {
        [Range(5, 15, ErrorMessage = "Out of range")]
        public int Count { get; set; }
    }

    // ---- structure -----------------------------------------------------------------------

    [Fact]
    public void Renders_min_max_step_value_on_native_input()
    {
        var cut = RenderComponent<AtomRangeInput<double>>(p => p
            .Add(c => c.Value, 2.5)
            .Add(c => c.Min, 0.0)
            .Add(c => c.Max, 10.0)
            .Add(c => c.Step, 0.5));

        var input = cut.Find("input");
        Assert.Equal("0", input.GetAttribute("min"));
        Assert.Equal("10", input.GetAttribute("max"));
        Assert.Equal("0.5", input.GetAttribute("step"));
        Assert.Equal("2.5", input.GetAttribute("value"));
    }

    [Fact]
    public void Value_updates_and_ValueChanged_invoked_on_input()
    {
        int? changedTo = null;
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Value, 5)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<int>(this, v => changedTo = v)));

        cut.Find("input").Input("8");

        Assert.Equal(8, changedTo);
        Assert.Equal(8, cut.Instance.Value);
    }

    // ---- Disabled vs ReadOnly --------------------------------------------------------------

    [Fact]
    public void Disabled_renders_nothing()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.Label, "L")
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Equal("", cut.Markup.Trim());
    }

    [Fact]
    public void ReadOnly_renders_greyed_and_blocks_input()
    {
        int? changedTo = null;
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.ReadOnly, true)
            .Add(c => c.Value, 5)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<int>(this, v => changedTo = v)));

        var root = cut.Find(".atom-range-input");
        Assert.Equal("readonly", root.GetAttribute("data-state"));

        var input = cut.Find("input");
        Assert.NotNull(input.GetAttribute("disabled"));

        input.Input("8");

        Assert.Null(changedTo);
        Assert.Equal(5, cut.Instance.Value);
    }

    // ---- min/max guard --------------------------------------------------------------------

    [Fact]
    public void Min_greater_than_or_equal_to_max_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RenderComponent<AtomRangeInput<int>>(p => p
                .Add(c => c.Min, 10)
                .Add(c => c.Max, 5)));
    }

    // ---- EditContext / validation -----------------------------------------------------------

    [Fact]
    public void Error_state_shows_icon_aria_invalid_and_replaces_help_text()
    {
        var model = new TestModel { Count = 20 };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Count)), "Out of range");
        editContext.NotifyValidationStateChanged();

        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Count)
            .Add(c => c.Min, 1)
            .Add(c => c.Max, 20)
            .Add(c => c.HelpText, "Helper text")
            .Add(c => c.ValidationFor, () => model.Count));

        Assert.NotNull(cut.Find(".atom-range-input-error-icon"));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Contains("Out of range", cut.Find(".atom-range-input-subtext").TextContent);
    }

    [Fact]
    public void No_error_shows_help_text_instead()
    {
        var model = new TestModel { Count = 10 };
        var editContext = new EditContext(model);

        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Count)
            .Add(c => c.Min, 1)
            .Add(c => c.Max, 20)
            .Add(c => c.HelpText, "Helper text")
            .Add(c => c.ValidationFor, () => model.Count));

        Assert.Empty(cut.FindAll(".atom-range-input-error-icon"));
        Assert.Equal("Helper text", cut.Find(".atom-range-input-subtext").TextContent);
    }

    [Fact]
    public void ValidationFor_falls_back_to_ValueExpression()
    {
        var model = new TestModel { Count = 20 };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Count)), "Out of range");
        editContext.NotifyValidationStateChanged();

        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Count)
            .Add(c => c.Min, 1)
            .Add(c => c.Max, 20)
            .Add(c => c.ValueExpression, () => model.Count));

        Assert.NotNull(cut.Find(".atom-range-input-error-icon"));
    }
}
