using Bunit;
using Xunit;

namespace BlazorAtoms.Progress.Tests;

/// <summary>bUnit coverage for <see cref="AtomProgressBar"/>. Purely declarative — no JS interop.
/// The look is CSS keyed off <c>data-*</c>, so these assertions cover the attribute/style contract
/// that CSS depends on, plus the value math and the ARIA surface.</summary>
public class AtomProgressBarTests
{
    [Fact]
    public void Value_becomes_fill_width_and_aria_valuenow()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Value, 40d));

        Assert.Contains("width:40%", cut.Find(".atom-progress-bar-fill").GetAttribute("style"));
        Assert.Equal("40", cut.Find(".atom-progress-bar-track").GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void Value_is_scaled_against_custom_min_and_max()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p
            .Add(x => x.Value, 15d)
            .Add(x => x.Min, 10d)
            .Add(x => x.Max, 20d));

        // 15 sits halfway between 10 and 20, so the fill is 50% even though the value is 15.
        Assert.Contains("width:50%", cut.Find(".atom-progress-bar-fill").GetAttribute("style"));
        Assert.Equal("10", cut.Find(".atom-progress-bar-track").GetAttribute("aria-valuemin"));
        Assert.Equal("20", cut.Find(".atom-progress-bar-track").GetAttribute("aria-valuemax"));
    }

    [Theory]
    [InlineData(-50d, "width:0%")]
    [InlineData(250d, "width:100%")]
    public void Out_of_range_value_is_clamped(double value, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Value, value));

        Assert.Contains(expected, cut.Find(".atom-progress-bar-fill").GetAttribute("style"));
    }

    [Fact]
    public void Clamped_value_is_what_gets_announced()
    {
        using var ctx = new BunitContext();

        // The visual and the announced value must not disagree — an over-range value reports the
        // clamped one, not the raw input.
        var cut = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Value, 250d));

        Assert.Equal("100", cut.Find(".atom-progress-bar-track").GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void Collapsed_scale_reports_zero_rather_than_dividing_by_zero()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p
            .Add(x => x.Value, 5d)
            .Add(x => x.Min, 10d)
            .Add(x => x.Max, 10d));

        Assert.Contains("width:0%", cut.Find(".atom-progress-bar-fill").GetAttribute("style"));
    }

    [Fact]
    public void Null_value_is_indeterminate_omits_valuenow_and_sets_no_inline_width()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>();

        Assert.Equal("true", cut.Find(".atom-progress-bar").GetAttribute("data-indeterminate"));
        Assert.Null(cut.Find(".atom-progress-bar-track").GetAttribute("aria-valuenow"));
        // No inline width: the sweep keyframe owns the fill's geometry.
        Assert.Null(cut.Find(".atom-progress-bar-fill").GetAttribute("style"));
    }

    [Fact]
    public void Determinate_emits_no_indeterminate_attribute()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Value, 10d));

        Assert.Null(cut.Find(".atom-progress-bar").GetAttribute("data-indeterminate"));
    }

    [Fact]
    public void Default_readout_is_a_whole_percent_and_Formatter_overrides_it()
    {
        using var ctx = new BunitContext();

        var plain = ctx.Render<AtomProgressBar>(p => p
            .Add(x => x.Value, 42.4d)
            .Add(x => x.ShowValue, true));
        Assert.Equal("42%", plain.Find(".atom-progress-bar-value").TextContent);

        var custom = ctx.Render<AtomProgressBar>(p => p
            .Add(x => x.Value, 42d)
            .Add(x => x.ShowValue, true)
            .Add(x => x.Formatter, v => $"{v} of 100 files"));
        Assert.Equal("42 of 100 files", custom.Find(".atom-progress-bar-value").TextContent);
    }

    [Fact]
    public void Readout_only_renders_when_ShowValue_is_set()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Value, 42d));

        Assert.Empty(cut.FindAll(".atom-progress-bar-value"));
    }

    [Fact]
    public void Indeterminate_shows_no_readout_even_with_ShowValue()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p.Add(x => x.ShowValue, true));

        // There is no number to show, so the element is absent rather than empty.
        Assert.Empty(cut.FindAll(".atom-progress-bar-value"));
    }

    [Theory]
    [InlineData(ProgressValuePosition.Inside, "inside")]
    [InlineData(ProgressValuePosition.Outside, "outside")]
    [InlineData(ProgressValuePosition.Above, "above")]
    public void ValuePosition_sets_the_data_attribute(ProgressValuePosition position, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p
            .Add(x => x.Value, 50d)
            .Add(x => x.ShowValue, true)
            .Add(x => x.ValuePosition, position));

        Assert.Equal(expected, cut.Find(".atom-progress-bar").GetAttribute("data-value-position"));
        Assert.Equal("50%", cut.Find(".atom-progress-bar-value").TextContent);
    }

    [Fact]
    public void Buffer_renders_a_second_band_scaled_to_the_same_span()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p
            .Add(x => x.Value, 30d)
            .Add(x => x.Buffer, 70d));

        Assert.Contains("width:70%", cut.Find(".atom-progress-bar-buffer").GetAttribute("style"));
    }

    [Fact]
    public void Buffer_is_dropped_while_indeterminate()
    {
        using var ctx = new BunitContext();

        // With no value there is no meaningful scale position for the buffer either.
        var cut = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Buffer, 70d));

        Assert.Empty(cut.FindAll(".atom-progress-bar-buffer"));
    }

    [Theory]
    [InlineData(ProgressVariant.Default, "default")]
    [InlineData(ProgressVariant.Primary, "primary")]
    [InlineData(ProgressVariant.Danger, "danger")]
    public void Variant_and_size_axes_are_emitted(ProgressVariant variant, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p
            .Add(x => x.Variant, variant)
            .Add(x => x.Size, ProgressSize.Large));

        var root = cut.Find(".atom-progress-bar");
        Assert.Equal(expected, root.GetAttribute("data-variant"));
        Assert.Equal("large", root.GetAttribute("data-size"));
    }

    [Fact]
    public void Default_effect_emits_no_attribute_and_multiword_effects_are_kebab_cased()
    {
        using var ctx = new BunitContext();

        var none = ctx.Render<AtomProgressBar>();
        Assert.Null(none.Find(".atom-progress-bar").GetAttribute("data-effect"));

        var striped = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Effect, ProgressEffect.StripesAnimated));
        Assert.Equal("stripes-animated", striped.Find(".atom-progress-bar").GetAttribute("data-effect"));
    }

    [Fact]
    public void Theming_parameters_become_custom_properties()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p
            .Add(x => x.Thickness, 14d)
            .Add(x => x.Radius, 0d)
            .Add(x => x.FillColor, "#7c3aed")
            .Add(x => x.Duration, 1.5)
            .Add(x => x.Width, "20rem"));

        var style = cut.Find(".atom-progress-bar").GetAttribute("style") ?? "";
        Assert.Contains("--progress-thickness:14px", style);
        Assert.Contains("--progress-radius:0px", style);
        Assert.Contains("--progress-fill-color:#7c3aed", style);
        // Invariant culture: a locale writing "1,5s" would be an invalid declaration.
        Assert.Contains("--progress-duration:1.5s", style);
        Assert.Contains("width:20rem", style);
    }

    [Fact]
    public void Radius_zero_is_honored_rather_than_treated_as_unset()
    {
        using var ctx = new BunitContext();

        // 0 is a meaningful radius (square corners), so the nullable param must emit it.
        var cut = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Radius, 0d));

        Assert.Contains("--progress-radius:0px", cut.Find(".atom-progress-bar").GetAttribute("style"));
    }

    [Fact]
    public void Unset_theming_parameters_emit_nothing()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Value, 1d));

        var style = cut.Find(".atom-progress-bar").GetAttribute("style") ?? "";
        Assert.DoesNotContain("--progress-thickness", style);
        Assert.DoesNotContain("--progress-fill-color", style);
    }

    [Fact]
    public void Label_renders_and_AriaLabel_falls_back_through_Label_to_a_default()
    {
        using var ctx = new BunitContext();

        var labelled = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Label, "Uploading"));
        Assert.Equal("Uploading", labelled.Find(".atom-progress-bar-label").TextContent);
        Assert.Equal("Uploading", labelled.Find(".atom-progress-bar-track").GetAttribute("aria-label"));

        var explicitName = ctx.Render<AtomProgressBar>(p => p
            .Add(x => x.Label, "Uploading")
            .Add(x => x.AriaLabel, "Upload progress"));
        Assert.Equal("Upload progress", explicitName.Find(".atom-progress-bar-track").GetAttribute("aria-label"));

        var bare = ctx.Render<AtomProgressBar>();
        Assert.Equal("Progress", bare.Find(".atom-progress-bar-track").GetAttribute("aria-label"));
    }

    [Fact]
    public void Role_lives_on_the_track_not_the_root()
    {
        using var ctx = new BunitContext();

        // The root also carries the label and readout; a progressbar must not contain extra text.
        var cut = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Label, "Uploading"));

        Assert.Null(cut.Find(".atom-progress-bar").GetAttribute("role"));
        Assert.Equal("progressbar", cut.Find(".atom-progress-bar-track").GetAttribute("role"));
    }

    [Fact]
    public void Visible_false_hides_without_leaving_the_dom()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p.Add(x => x.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-progress-bar").GetAttribute("style"));
    }

    [Fact]
    public void CssClass_and_Style_layer_onto_the_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressBar>(p => p
            .Add(x => x.CssClass, "mine")
            .Add(x => x.Style, "opacity:.5")
            .Add(x => x.Thickness, 4d));

        var root = cut.Find(".atom-progress-bar");
        Assert.Contains("mine", root.GetAttribute("class"));
        var style = root.GetAttribute("style") ?? "";
        // Caller's Style comes last so it wins over the component's own declarations.
        Assert.True(style.IndexOf("opacity:.5") > style.IndexOf("--progress-thickness"));
    }
}
