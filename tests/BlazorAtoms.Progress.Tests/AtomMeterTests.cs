using Bunit;
using Xunit;

namespace BlazorAtoms.Progress.Tests;

/// <summary>bUnit coverage for <see cref="AtomMeter"/>. Purely declarative — no JS interop. The
/// substance here is the level classification, which reimplements the HTML <c>&lt;meter&gt;</c>
/// spec's three-way rule, so each of its three branches gets its own case.</summary>
public class AtomMeterTests
{
    [Fact]
    public void Renders_a_meter_role_not_a_progressbar()
    {
        using var ctx = new BunitContext();

        // A meter is a static measurement; a progressbar advances toward completion.
        var cut = ctx.Render<AtomMeter>(p => p.Add(x => x.Value, 50d));

        Assert.Equal("meter", cut.Find(".atom-meter-track").GetAttribute("role"));
    }

    [Fact]
    public void Value_becomes_fill_width_and_aria_valuenow()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p.Add(x => x.Value, 62d));

        Assert.Contains("width:62%", cut.Find(".atom-meter-fill").GetAttribute("style"));
        Assert.Equal("62", cut.Find(".atom-meter-track").GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void Null_value_renders_an_empty_track_and_does_not_animate()
    {
        using var ctx = new BunitContext();

        // A meter has nothing to sweep, so unlike the bar and the ring there is no indeterminate
        // animation — no fill element exists to animate. data-indeterminate is the styling hook for
        // the "no reading" hatching.
        var cut = ctx.Render<AtomMeter>();

        Assert.Empty(cut.FindAll(".atom-meter-fill"));
        Assert.Equal("true", cut.Find(".atom-meter").GetAttribute("data-indeterminate"));
    }

    [Fact]
    public void Null_value_drops_the_meter_role_entirely_rather_than_emitting_an_invalid_one()
    {
        using var ctx = new BunitContext();

        // ARIA requires aria-valuenow on role="meter" — there is no indeterminate meter in the spec.
        // So with no Value the role goes away rather than shipping incomplete.
        var cut = ctx.Render<AtomMeter>(p => p.Add(x => x.Label, "Disk"));

        var track = cut.Find(".atom-meter-track");
        Assert.Null(track.GetAttribute("role"));
        Assert.Null(track.GetAttribute("aria-valuenow"));
        Assert.Null(track.GetAttribute("aria-valuemin"));
        Assert.Null(track.GetAttribute("aria-valuemax"));
        Assert.Null(track.GetAttribute("aria-valuetext"));
        // No invented value either — a valuenow of Min would read as a real 0% measurement.
        Assert.DoesNotContain("aria-valuenow", cut.Markup);
    }

    [Fact]
    public void A_value_restores_the_full_meter_semantics()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 40d)
            .Add(x => x.Label, "Disk"));

        var track = cut.Find(".atom-meter-track");
        Assert.Equal("meter", track.GetAttribute("role"));
        Assert.Equal("Disk", track.GetAttribute("aria-label"));
        Assert.Equal("40", track.GetAttribute("aria-valuenow"));
        Assert.Equal("0", track.GetAttribute("aria-valuemin"));
        Assert.Equal("100", track.GetAttribute("aria-valuemax"));
    }

    [Fact]
    public void No_level_without_an_optimum_to_judge_against()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 50d)
            .Add(x => x.Low, 20d)
            .Add(x => x.High, 80d));

        Assert.Null(cut.Find(".atom-meter").GetAttribute("data-level"));
    }

    [Theory]
    // Optimum below Low — small is good.
    [InlineData(10d, "optimum")]
    [InlineData(20d, "optimum")]      // boundary: at Low is still optimum
    [InlineData(50d, "suboptimum")]
    [InlineData(80d, "suboptimum")]   // boundary: at High is still suboptimum
    [InlineData(95d, "sub-suboptimum")]
    public void Level_when_small_is_good(double value, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.Low, 20d)
            .Add(x => x.High, 80d)
            .Add(x => x.Optimum, 0d));

        Assert.Equal(expected, cut.Find(".atom-meter").GetAttribute("data-level"));
    }

    [Theory]
    // Optimum above High — large is good (the mirror image).
    [InlineData(95d, "optimum")]
    [InlineData(80d, "optimum")]
    [InlineData(50d, "suboptimum")]
    [InlineData(20d, "suboptimum")]
    [InlineData(5d, "sub-suboptimum")]
    public void Level_when_large_is_good(double value, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.Low, 20d)
            .Add(x => x.High, 80d)
            .Add(x => x.Optimum, 100d));

        Assert.Equal(expected, cut.Find(".atom-meter").GetAttribute("data-level"));
    }

    [Theory]
    // Optimum between the bounds — the middle is good, and the spec defines no third band.
    [InlineData(50d, "optimum")]
    [InlineData(20d, "optimum")]
    [InlineData(80d, "optimum")]
    [InlineData(5d, "suboptimum")]
    [InlineData(95d, "suboptimum")]
    public void Level_when_the_middle_is_good_never_reaches_the_third_band(double value, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.Low, 20d)
            .Add(x => x.High, 80d)
            .Add(x => x.Optimum, 50d));

        Assert.Equal(expected, cut.Find(".atom-meter").GetAttribute("data-level"));
    }

    [Fact]
    public void Unset_bounds_collapse_to_the_ends_of_the_scale()
    {
        using var ctx = new BunitContext();

        // With no Low/High the whole scale is one band, so any value is optimum.
        var cut = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 3d)
            .Add(x => x.Optimum, 50d));

        Assert.Equal("optimum", cut.Find(".atom-meter").GetAttribute("data-level"));
    }

    [Fact]
    public void Level_is_judged_on_the_clamped_value()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 500d)
            .Add(x => x.Low, 20d)
            .Add(x => x.High, 80d)
            .Add(x => x.Optimum, 100d));

        // 500 clamps to 100, which is above High → optimum.
        Assert.Equal("optimum", cut.Find(".atom-meter").GetAttribute("data-level"));
    }

    [Fact]
    public void Segments_draw_one_gradient_overlay_not_n_elements()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 60d)
            .Add(x => x.Segments, 5));

        var ticks = cut.FindAll(".atom-meter-ticks");
        Assert.Single(ticks);
        var style = ticks[0].GetAttribute("style") ?? "";
        Assert.Contains("repeating-linear-gradient", style);
        // 100 / 5 = 20% per tick.
        Assert.Contains("20%", style);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public void One_or_no_segments_draws_no_ticks(int? segments)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 60d)
            .Add(x => x.Segments, segments));

        Assert.Empty(cut.FindAll(".atom-meter-ticks"));
    }

    [Fact]
    public void ShowScale_places_the_bounds_at_their_real_positions()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 50d)
            .Add(x => x.Low, 25d)
            .Add(x => x.High, 75d)
            .Add(x => x.ShowScale, true));

        var marks = cut.FindAll(".atom-meter-scale-mark");
        Assert.Equal(2, marks.Count);
        Assert.Contains("inset-inline-start:25%", marks[0].GetAttribute("style"));
        Assert.Contains("inset-inline-start:75%", marks[1].GetAttribute("style"));
    }

    [Fact]
    public void Scale_ruler_shows_raw_numbers_but_honors_a_Formatter()
    {
        using var ctx = new BunitContext();

        var plain = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 50d)
            .Add(x => x.Max, 500d)
            .Add(x => x.ShowScale, true));
        // Raw numbers, not percentages — a percentage of itself would be meaningless on a ruler.
        Assert.Contains("500", plain.Find(".atom-meter-scale").TextContent);

        var formatted = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 50d)
            .Add(x => x.Max, 500d)
            .Add(x => x.ShowScale, true)
            .Add(x => x.Formatter, v => $"{v} GB"));
        Assert.Contains("500 GB", formatted.Find(".atom-meter-scale").TextContent);
    }

    [Fact]
    public void No_scale_element_unless_asked_for()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p.Add(x => x.Value, 50d));

        Assert.Empty(cut.FindAll(".atom-meter-scale"));
    }

    [Fact]
    public void Readout_sits_beside_the_track_when_there_is_no_label()
    {
        using var ctx = new BunitContext();

        var withLabel = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 40d)
            .Add(x => x.Label, "Disk")
            .Add(x => x.ShowValue, true));
        // One readout only — in the head row next to the label.
        Assert.Single(withLabel.FindAll(".atom-meter-value"));
        Assert.NotNull(withLabel.Find(".atom-meter-head .atom-meter-value"));

        var noLabel = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 40d)
            .Add(x => x.ShowValue, true));
        Assert.Single(noLabel.FindAll(".atom-meter-value"));
        Assert.NotNull(noLabel.Find(".atom-meter-row .atom-meter-value"));
    }

    [Fact]
    public void Axes_theming_and_naming_reach_the_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomMeter>(p => p
            .Add(x => x.Value, 40d)
            .Add(x => x.Variant, ProgressVariant.Warning)
            .Add(x => x.Size, ProgressSize.Small)
            .Add(x => x.Effect, ProgressEffect.Gradient)
            .Add(x => x.TrackColor, "#eee")
            .Add(x => x.Width, "12rem"));

        var root = cut.Find(".atom-meter");
        Assert.Equal("warning", root.GetAttribute("data-variant"));
        Assert.Equal("small", root.GetAttribute("data-size"));
        Assert.Equal("gradient", root.GetAttribute("data-effect"));
        var style = root.GetAttribute("style") ?? "";
        Assert.Contains("--progress-track-color:#eee", style);
        Assert.Contains("width:12rem", style);
        Assert.Equal("Meter", cut.Find(".atom-meter-track").GetAttribute("aria-label"));
    }
}
