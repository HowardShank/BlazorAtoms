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

    [Fact]
    public void Zero_config_uses_default_min_max_step_and_does_not_throw()
    {
        // ex1 shape: only @bind-Value, no Min/Max/Step — must not throw (regression guard for
        // Min/Max both defaulting to 0 and tripping the Min<Max guard).
        var cut = RenderComponent<AtomRangeInput<int>>(p => p.Add(c => c.Value, 5));

        var input = cut.Find("input");
        Assert.Equal("0", input.GetAttribute("min"));
        Assert.Equal("100", input.GetAttribute("max"));
        Assert.Equal("1", input.GetAttribute("step"));
    }

    [Fact]
    public void Supports_negative_min_and_value()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Value, -5)
            .Add(c => c.Min, -10)
            .Add(c => c.Max, 10));

        var input = cut.Find("input");
        Assert.Equal("-10", input.GetAttribute("min"));
        Assert.Equal("-5", input.GetAttribute("value"));
    }

    [Fact]
    public void Supports_fractional_step_and_value_with_double()
    {
        double changedTo = double.NaN;
        var cut = RenderComponent<AtomRangeInput<double>>(p => p
            .Add(c => c.Value, 1.5)
            .Add(c => c.Min, 0.0)
            .Add(c => c.Max, 5.0)
            .Add(c => c.Step, 0.5)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<double>(this, v => changedTo = v)));

        Assert.Equal("0.5", cut.Find("input").GetAttribute("step"));
        Assert.Equal("1.5", cut.Find("input").GetAttribute("value"));

        cut.Find("input").Input("2.5");
        Assert.Equal(2.5, changedTo);
    }

    [Fact]
    public void Supports_decimal_tvalue()
    {
        var cut = RenderComponent<AtomRangeInput<decimal>>(p => p
            .Add(c => c.Value, 2.5m)
            .Add(c => c.Min, 0m)
            .Add(c => c.Max, 10m)
            .Add(c => c.Step, 0.5m));

        Assert.Equal("2.5", cut.Find("input").GetAttribute("value"));
    }

    // ---- Disabled vs ReadOnly --------------------------------------------------------------

    [Fact]
    public void Disabled_greys_out_and_blocks_input()
    {
        int? changedTo = null;
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.Value, 5)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<int>(this, v => changedTo = v)));

        Assert.Equal("disabled", cut.Find(".atom-range-input").GetAttribute("data-state"));
        Assert.NotNull(cut.Find("input").GetAttribute("disabled"));

        cut.Find("input").Input("8");
        Assert.Null(changedTo);
        Assert.Equal(5, cut.Instance.Value);
    }

    [Fact]
    public void ReadOnly_equates_to_disabled()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.ReadOnly, true)
            .Add(c => c.Value, 5)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Equal("disabled", cut.Find(".atom-range-input").GetAttribute("data-state"));
        Assert.NotNull(cut.Find("input").GetAttribute("disabled"));
    }

    [Fact]
    public void Visible_false_hides_via_display_none_but_stays_in_dom()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Visible, false)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        var root = cut.Find(".atom-range-input");
        Assert.Contains("display:none", root.GetAttribute("style"));
        Assert.NotNull(cut.Find("input")); // still rendered
    }

    [Fact]
    public void Visible_true_by_default_has_no_display_none()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        var style = cut.Find(".atom-range-input").GetAttribute("style");
        Assert.DoesNotContain("display:none", style ?? "");
    }

    [Theory]
    [InlineData(HandleShape.Round, "round")]
    [InlineData(HandleShape.Square, "square")]
    [InlineData(HandleShape.Heart, "heart")]
    [InlineData(HandleShape.Star, "star")]
    [InlineData(HandleShape.Bolt, "bolt")]
    public void HandleShape_maps_to_data_attribute(HandleShape shape, string expected)
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.HandleShape, shape)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Equal(expected, cut.Find("input").GetAttribute("data-handle-shape"));
    }

    [Fact]
    public void Glyph_handle_bakes_svg_background_with_fill_and_stroke()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.HandleShape, HandleShape.Star)
            .Add(c => c.HandleColor, "#ff8800")
            .Add(c => c.OutlineColor, "#003366")
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        var input = cut.Find("input");
        Assert.Equal("true", input.GetAttribute("data-handle-glyph"));
        var style = input.GetAttribute("style")!;
        Assert.Contains("--range-handle-glyph", style);
        // '#' is URL-encoded to %23 so it doesn't truncate the data: URL.
        Assert.Contains("fill=\"%23ff8800\"", style);
        Assert.Contains("stroke=\"%23003366\"", style);
    }

    [Fact]
    public void Glyph_handle_with_zero_outline_width_omits_stroke()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.HandleShape, HandleShape.Star)
            .Add(c => c.OutlineWidth, 0d)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.DoesNotContain("stroke=", cut.Find("input").GetAttribute("style")!);
    }

    [Fact]
    public void Glyph_handle_in_error_state_strokes_error_color()
    {
        var model = new TestModel { Count = 20 };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Count)), "Out of range");
        editContext.NotifyValidationStateChanged();

        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.HandleShape, HandleShape.Star)
            .Add(c => c.Value, model.Count)
            .Add(c => c.Min, 1)
            .Add(c => c.Max, 20)
            .Add(c => c.ValidationFor, () => model.Count));

        Assert.Contains("stroke=\"%23dc2626\"", cut.Find("input").GetAttribute("style")!);
    }

    [Fact]
    public void Non_glyph_handle_has_no_glyph_var_or_flag()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.HandleShape, HandleShape.Round)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        var input = cut.Find("input");
        Assert.Null(input.GetAttribute("data-handle-glyph"));
        Assert.DoesNotContain("--range-handle-glyph", input.GetAttribute("style") ?? "");
    }

    [Fact]
    public void Box_handle_emits_handle_and_outline_custom_properties()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.HandleShape, HandleShape.Round)
            .Add(c => c.HandleColor, "#ff8800")
            .Add(c => c.OutlineColor, "#003366")
            .Add(c => c.OutlineWidth, 3d)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        var style = cut.Find("input").GetAttribute("style")!;
        Assert.Contains("--range-handle-color:#ff8800", style);
        Assert.Contains("--range-handle-outline-color:#003366", style);
        Assert.Contains("--range-handle-outline-width:3px", style);
    }

    [Theory]
    [InlineData(HandlePosition.Above, "above")]
    [InlineData(HandlePosition.Below, "below")]
    public void HandlePosition_emits_data_attribute(HandlePosition position, string expected)
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.HandlePosition, position)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Equal(expected, cut.Find("input").GetAttribute("data-handle-position"));
    }

    [Fact]
    public void HandlePosition_center_emits_no_data_attribute()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Null(cut.Find("input").GetAttribute("data-handle-position"));
    }

    [Fact]
    public void HandleOffset_emits_inline_offset_var_and_overrides_position()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.HandlePosition, HandlePosition.Above)
            .Add(c => c.HandleOffset, -20d)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        var input = cut.Find("input");
        Assert.Contains("--range-handle-offset:-20px", input.GetAttribute("style"));
        // Numeric offset wins: the enum's data attribute is suppressed.
        Assert.Null(input.GetAttribute("data-handle-position"));
    }

    [Fact]
    public void HandleRotation_emits_inline_rotate_var()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.HandleRotation, 45d)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Contains("--range-handle-rotate:45deg", cut.Find("input").GetAttribute("style"));
    }

    [Fact]
    public void Start_and_end_icons_render_in_their_own_slots()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10)
            .Add(c => c.StartIcon, b => b.AddMarkupContent(0, "<i class=\"start-marker\"></i>"))
            .Add(c => c.EndIcon, b => b.AddMarkupContent(0, "<i class=\"end-marker\"></i>")));

        var start = cut.Find(".atom-range-input-icon-start");
        var end = cut.Find(".atom-range-input-icon-end");
        Assert.NotNull(start.QuerySelector(".start-marker"));
        Assert.NotNull(end.QuerySelector(".end-marker"));
    }

    [Fact]
    public void No_icon_slots_when_not_provided()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Empty(cut.FindAll(".atom-range-input-icon"));
    }

    // ---- icon presets --------------------------------------------------------------------

    private static string IconAt(IRenderedComponent<AtomRangeInput<int>> cut, string slot) =>
        cut.Find($".atom-range-input-icon-{slot} svg")!.GetAttribute("data-icon")!;

    [Theory]
    [InlineData(RangeIconPreset.Brightness, "brightness-low", "brightness-high")]
    [InlineData(RangeIconPreset.PlaybackSpeed, "speed-slow", "speed-fast")]
    [InlineData(RangeIconPreset.Price, "price-low", "price-high")]
    [InlineData(RangeIconPreset.Opacity, "opacity-low", "opacity-high")]
    public void Preset_horizontal_puts_min_icon_at_start_and_max_icon_at_end(
        RangeIconPreset preset, string minIcon, string maxIcon)
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.IconPreset, preset)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Equal(minIcon, IconAt(cut, "start"));
        Assert.Equal(maxIcon, IconAt(cut, "end"));
    }

    [Fact]
    public void Volume_preset_horizontal_puts_mute_at_start_and_loud_at_end()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.IconPreset, RangeIconPreset.Volume)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Equal("volume-mute", IconAt(cut, "start"));
        Assert.Equal("volume-loud", IconAt(cut, "end"));
    }

    [Fact]
    public void IconPresetReversed_swaps_min_and_max_icons()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.IconPreset, RangeIconPreset.Volume)
            .Add(c => c.IconPresetReversed, true)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Equal("volume-loud", IconAt(cut, "start"));
        Assert.Equal("volume-mute", IconAt(cut, "end"));
    }

    [Fact]
    public void Thermostat_preset_vertical_default_direction_puts_hot_at_start()
    {
        // BottomToTop (default): max at top = Start slot -> hot (max end).
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.IconPreset, RangeIconPreset.Thermostat)
            .Add(c => c.Orientation, Orientation.Vertical)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Equal("thermostat-hot", IconAt(cut, "start"));
        Assert.Equal("thermostat-cold", IconAt(cut, "end"));
    }

    [Fact]
    public void Thermostat_preset_vertical_topToBottom_puts_cold_at_start()
    {
        // TopToBottom: max at bottom = Start slot -> cold (min end).
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.IconPreset, RangeIconPreset.Thermostat)
            .Add(c => c.Orientation, Orientation.Vertical)
            .Add(c => c.VerticalDirection, VerticalDirection.TopToBottom)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Equal("thermostat-cold", IconAt(cut, "start"));
        Assert.Equal("thermostat-hot", IconAt(cut, "end"));
    }

    [Fact]
    public void Explicit_StartIcon_overrides_preset_for_that_slot_only()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.IconPreset, RangeIconPreset.Volume)
            .Add(c => c.StartIcon, b => b.AddMarkupContent(0, "<i class=\"custom-marker\"></i>"))
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.NotNull(cut.Find(".atom-range-input-icon-start").QuerySelector(".custom-marker"));
        Assert.Equal("volume-loud", IconAt(cut, "end"));
    }

    [Fact]
    public void Vertical_orientation_emits_data_and_aria_attributes()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Orientation, Orientation.Vertical)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        var input = cut.Find("input");
        Assert.Equal("vertical", input.GetAttribute("data-orientation"));
        Assert.Equal("vertical", input.GetAttribute("aria-orientation"));
        Assert.Equal("vertical", cut.Find(".atom-range-input-track-box").GetAttribute("data-orientation"));
        Assert.Equal("vertical", cut.Find(".atom-range-input-track-wrap").GetAttribute("data-orientation"));
    }

    [Fact]
    public void Horizontal_orientation_default_emits_no_orientation_attributes()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        var input = cut.Find("input");
        Assert.Null(input.GetAttribute("data-orientation"));
        Assert.Null(input.GetAttribute("aria-orientation"));
        Assert.Null(cut.Find(".atom-range-input-track-box").GetAttribute("data-orientation"));
        Assert.Null(cut.Find(".atom-range-input-track-wrap").GetAttribute("data-orientation"));
    }

    [Fact]
    public void VerticalDirection_topToBottom_emits_data_attribute_when_vertical()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Orientation, Orientation.Vertical)
            .Add(c => c.VerticalDirection, VerticalDirection.TopToBottom)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Equal("top-to-bottom", cut.Find("input").GetAttribute("data-vertical-direction"));
    }

    [Fact]
    public void VerticalDirection_default_emits_no_data_attribute()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Orientation, Orientation.Vertical)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Null(cut.Find("input").GetAttribute("data-vertical-direction"));
    }

    [Fact]
    public void VerticalDirection_ignored_when_horizontal()
    {
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.VerticalDirection, VerticalDirection.TopToBottom)
            .Add(c => c.Min, 0)
            .Add(c => c.Max, 10));

        Assert.Null(cut.Find("input").GetAttribute("data-vertical-direction"));
    }

    // ---- inverted min/max --------------------------------------------------------------------

    [Fact]
    public void Inverted_min_max_renders_without_throwing()
    {
        // A transient Min > Max (e.g. mid-drag on two separate slider controls) must NOT throw —
        // throwing in OnParametersSet faults the component and freezes the whole render tree.
        var cut = RenderComponent<AtomRangeInput<int>>(p => p
            .Add(c => c.Min, 10)
            .Add(c => c.Max, 5)
            .Add(c => c.Value, 7));

        var input = cut.Find("input");
        Assert.Equal("10", input.GetAttribute("min"));
        Assert.Equal("5", input.GetAttribute("max"));
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
