using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorAtoms.Inputs.Tests;

public class AtomNumberFieldTests : BunitContext
{
    private sealed class TestModel
    {
        [Range(1, 10, ErrorMessage = "Out of range")]
        public int Quantity { get; set; }
    }

    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_native_number_input()
    {
        var cut = Render<AtomNumberField<int>>();
        Assert.Equal("number", cut.Find("input.atom-number-field-field").GetAttribute("type"));
    }

    [Fact]
    public void Min_max_step_render_when_set()
    {
        var cut = Render<AtomNumberField<double>>(p => p
            .Add(c => c.Min, 0d)
            .Add(c => c.Max, 10d)
            .Add(c => c.Step, 0.25));

        var input = cut.Find("input");
        Assert.Equal("0", input.GetAttribute("min"));
        Assert.Equal("10", input.GetAttribute("max"));
        Assert.Equal("0.25", input.GetAttribute("step"));
    }

    [Fact]
    public void Unset_bounds_omit_their_attributes()
    {
        // A number field's bounds are genuinely optional (unlike a slider's), so "unset" has to mean
        // no attribute rather than a defaulted 0/100.
        var input = Render<AtomNumberField<int>>().Find("input");

        Assert.Null(input.GetAttribute("min"));
        Assert.Null(input.GetAttribute("max"));
        Assert.Null(input.GetAttribute("step"));
    }

    [Fact]
    public void Prefix_and_suffix_text_render_in_their_own_slots()
    {
        var cut = Render<AtomNumberField<decimal>>(p => p
            .Add(c => c.PrefixText, "$")
            .Add(c => c.SuffixText, "/mo"));

        Assert.Equal("$", cut.Find(".atom-number-field-prefix").TextContent);
        Assert.Equal("/mo", cut.Find(".atom-number-field-suffix").TextContent);
    }

    [Fact]
    public void ShowSpinners_false_flags_the_input_and_true_emits_nothing()
    {
        Assert.Equal("hidden", Render<AtomNumberField<int>>(p => p.Add(c => c.ShowSpinners, false))
            .Find("input").GetAttribute("data-spinners"));

        Assert.Null(Render<AtomNumberField<int>>().Find("input").GetAttribute("data-spinners"));
    }

    // ---- value formatting --------------------------------------------------------------------

    [Fact]
    public void Value_renders_invariant_regardless_of_ambient_culture()
    {
        // The HTML spec fixes a number input's value to a '.'-separated literal, so a de-DE thread
        // must not render "1,5" — the browser would reject it.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var cut = Render<AtomNumberField<double>>(p => p.Add(c => c.Value, 1.5));
            Assert.Equal("1.5", cut.Find("input").GetAttribute("value"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Null_value_renders_an_empty_box()
    {
        var cut = Render<AtomNumberField<int?>>(p => p.Add(c => c.Value, null));
        Assert.Equal("", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Decimal_value_keeps_its_scale()
    {
        var cut = Render<AtomNumberField<decimal>>(p => p.Add(c => c.Value, 19.90m));
        Assert.Equal("19.90", cut.Find("input").GetAttribute("value"));
    }

    // ---- value flow --------------------------------------------------------------------------

    [Fact]
    public void Input_event_commits_a_parsed_value()
    {
        int? changedTo = null;
        var cut = Render<AtomNumberField<int>>(p => p
            .Add(c => c.Value, 1)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<int>(this, v => changedTo = v)));

        cut.Find("input").Input("42");

        Assert.Equal(42, changedTo);
        Assert.Equal(42, cut.Instance.Value);
    }

    [Fact]
    public void Clearing_a_nullable_field_commits_null()
    {
        var cut = Render<AtomNumberField<int?>>(p => p.Add(c => c.Value, 5));

        cut.Find("input").Input("");

        Assert.Null(cut.Instance.Value);
    }

    [Fact]
    public void Clearing_a_non_nullable_field_leaves_the_value_alone()
    {
        // int has no representation for "empty", so the alternative would be silently zeroing the
        // value mid-edit.
        var cut = Render<AtomNumberField<int>>(p => p.Add(c => c.Value, 5));

        cut.Find("input").Input("");

        Assert.Equal(5, cut.Instance.Value);
    }

    [Theory]
    [InlineData("3.7")]      // fractional into an int
    [InlineData("abc")]      // not a number at all
    [InlineData("1e400")]    // overflows every target type
    public void Unrepresentable_input_is_dropped_rather_than_throwing(string raw)
    {
        var cut = Render<AtomNumberField<int>>(p => p.Add(c => c.Value, 7));

        cut.Find("input").Input(raw);

        Assert.Equal(7, cut.Instance.Value);
    }

    [Fact]
    public void Fractional_input_commits_when_TValue_can_hold_it()
    {
        var cut = Render<AtomNumberField<double>>(p => p.Add(c => c.Value, 1d));

        cut.Find("input").Input("3.7");

        Assert.Equal(3.7, cut.Instance.Value);
    }

    [Fact]
    public void Negative_values_commit()
    {
        var cut = Render<AtomNumberField<int>>(p => p.Add(c => c.Value, 0));

        cut.Find("input").Input("-12");

        Assert.Equal(-12, cut.Instance.Value);
    }

    [Fact]
    public void UpdateOn_Change_ignores_input_and_commits_on_change()
    {
        var cut = Render<AtomNumberField<int>>(p => p
            .Add(c => c.Value, 1)
            .Add(c => c.UpdateOn, InputUpdateOn.Change));

        cut.Find("input").Input("2");
        Assert.Equal(1, cut.Instance.Value);

        cut.Find("input").Change("3");
        Assert.Equal(3, cut.Instance.Value);
    }

    // ---- state -------------------------------------------------------------------------------

    [Fact]
    public void Disabled_and_ReadOnly_map_to_their_own_native_attributes()
    {
        var disabled = Render<AtomNumberField<int>>(p => p.Add(c => c.Disabled, true)).Find("input");
        Assert.NotNull(disabled.GetAttribute("disabled"));
        Assert.Null(disabled.GetAttribute("readonly"));

        var readOnly = Render<AtomNumberField<int>>(p => p.Add(c => c.ReadOnly, true)).Find("input");
        Assert.NotNull(readOnly.GetAttribute("readonly"));
        Assert.Null(readOnly.GetAttribute("disabled"));
    }

    [Fact]
    public void Blocked_input_does_not_commit()
    {
        var cut = Render<AtomNumberField<int>>(p => p
            .Add(c => c.Value, 5)
            .Add(c => c.ReadOnly, true));

        cut.Find("input").Input("9");

        Assert.Equal(5, cut.Instance.Value);
    }

    [Fact]
    public void Visible_false_hides_via_display_none()
    {
        var cut = Render<AtomNumberField<int>>(p => p.Add(c => c.Visible, false));
        Assert.Contains("display:none", cut.Find(".atom-number-field").GetAttribute("style")!);
    }

    [Fact]
    public void AriaLabel_defaults_to_number_field()
    {
        Assert.Equal("Number field", Render<AtomNumberField<int>>().Find("input").GetAttribute("aria-label"));
    }

    // ---- styling axes ------------------------------------------------------------------------

    [Fact]
    public void Variant_size_and_effect_emit_their_data_attributes()
    {
        var cut = Render<AtomNumberField<int>>(p => p
            .Add(c => c.Variant, InputVariant.Filled)
            .Add(c => c.Size, InputSize.Small)
            .Add(c => c.Effect, InputEffect.FocusGlow));

        var root = cut.Find(".atom-number-field");
        Assert.Equal("filled", root.GetAttribute("data-variant"));
        Assert.Equal("small", root.GetAttribute("data-size"));
        Assert.Equal("focus-glow", root.GetAttribute("data-effect"));
    }

    [Fact]
    public void Theming_parameters_emit_field_custom_properties()
    {
        var cut = Render<AtomNumberField<int>>(p => p
            .Add(c => c.Width, 140d)
            .Add(c => c.AccentColor, "seagreen"));

        var style = cut.Find(".atom-number-field").GetAttribute("style")!;
        Assert.Contains("--field-width:140px;", style);
        Assert.Contains("--field-accent:seagreen;", style);
    }

    // ---- EditContext / validation ------------------------------------------------------------

    [Fact]
    public void Error_state_sets_aria_invalid_and_replaces_help_text()
    {
        var model = new TestModel { Quantity = 99 };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Quantity)), "Out of range");
        editContext.NotifyValidationStateChanged();

        var cut = Render<AtomNumberField<int>>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Quantity)
            .Add(c => c.HelpText, "How many")
            .Add(c => c.ValidationFor, () => model.Quantity));

        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Equal("error", cut.Find(".atom-number-field").GetAttribute("data-state"));
        Assert.Equal("Out of range", cut.Find(".atom-number-field-subtext").TextContent);
    }

    [Fact]
    public void Committing_a_value_notifies_the_EditContext()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var notified = false;
        editContext.OnFieldChanged += (_, _) => notified = true;

        var cut = Render<AtomNumberField<int>>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Quantity)
            .Add(c => c.ValidationFor, () => model.Quantity));

        cut.Find("input").Input("4");

        Assert.True(notified);
    }
}
