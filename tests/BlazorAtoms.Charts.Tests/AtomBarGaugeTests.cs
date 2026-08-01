namespace BlazorAtoms.Charts.Tests;

public class AtomBarGaugeTests : BunitContext
{
    [Fact]
    public void Horizontal_is_the_default_orientation()
    {
        var cut = Render<AtomBarGauge>();
        Assert.Equal("horizontal", cut.Find(".atom-bar-gauge-wrap").GetAttribute("data-orientation"));
    }

    [Fact]
    public void Vertical_orientation_swaps_the_viewBox_aspect_ratio()
    {
        var horizontal = Render<AtomBarGauge>();
        var vertical = Render<AtomBarGauge>(p => p.Add(c => c.Orientation, ChartOrientation.Vertical));

        var (hw, hh) = ViewBoxSize(horizontal.Find(".atom-bar-gauge-svg"));
        var (vw, vh) = ViewBoxSize(vertical.Find(".atom-bar-gauge-svg"));

        Assert.Equal("vertical", vertical.Find(".atom-bar-gauge-wrap").GetAttribute("data-orientation"));
        Assert.Equal(hw, vh);
        Assert.Equal(hh, vw);
        Assert.True(hw > hh); // horizontal track reads wider than it is tall
    }

    [Fact]
    public void Segmented_style_draws_one_rect_per_band()
    {
        var cut = Render<AtomBarGauge>(p => p.Add(c => c.SegmentCount, 4));
        Assert.Equal(4, cut.FindAll(".atom-bar-gauge-band").Count);
    }

    [Fact]
    public void Segmented_style_is_colored_with_4_bands_by_default()
    {
        var cut = Render<AtomBarGauge>();
        Assert.Equal(4, cut.FindAll(".atom-bar-gauge-band").Count);
        Assert.NotNull(cut.Find(".atom-bar-gauge-track"));
    }

    [Fact]
    public void An_explicit_empty_Bands_list_opts_out_of_bands_entirely()
    {
        var cut = Render<AtomBarGauge>(p => p.Add(c => c.Bands, Array.Empty<GaugeBand>()));
        Assert.Empty(cut.FindAll(".atom-bar-gauge-band"));
        Assert.NotNull(cut.Find(".atom-bar-gauge-track"));
    }

    [Fact]
    public void Gradient_style_draws_one_gradient_rect_and_a_unique_defs_id()
    {
        var first = Render<AtomBarGauge>(p => p.Add(c => c.BarStyle, BarGaugeStyle.Gradient));
        var second = Render<AtomBarGauge>(p => p.Add(c => c.BarStyle, BarGaugeStyle.Gradient));

        var firstId = first.Find("linearGradient").GetAttribute("id");
        var secondId = second.Find("linearGradient").GetAttribute("id");

        Assert.NotNull(firstId);
        Assert.NotEqual(firstId, secondId);
        Assert.Contains($"url(#{firstId})", first.Find(".atom-bar-gauge-band").GetAttribute("fill"));
    }

    [Fact]
    public void Ticks_style_draws_many_ticks_with_exactly_one_active()
    {
        var cut = Render<AtomBarGauge>(p => p
            .Add(c => c.BarStyle, BarGaugeStyle.Ticks)
            .Add(c => c.TickCount, 10)
            .Add(c => c.Value, 50d));

        var ticks = cut.FindAll(".atom-bar-gauge-tick");
        Assert.Equal(10, ticks.Count);
        Assert.Single(ticks, t => t.GetAttribute("data-active") == "true");
    }

    [Fact]
    public void The_pointer_moves_with_the_value()
    {
        var low = Render<AtomBarGauge>(p => p.Add(c => c.Value, 0d));
        var high = Render<AtomBarGauge>(p => p.Add(c => c.Value, 100d));

        Assert.NotEqual(low.Find(".atom-bar-gauge-pointer").GetAttribute("d"),
            high.Find(".atom-bar-gauge-pointer").GetAttribute("d"));
    }

    [Fact]
    public void ShowPointer_false_omits_the_pointer()
    {
        var cut = Render<AtomBarGauge>(p => p.Add(c => c.ShowPointer, false));
        Assert.Empty(cut.FindAll(".atom-bar-gauge-pointer"));
    }

    [Fact]
    public void Elevation_defaults_to_Floating_and_is_overridable()
    {
        var defaulted = Render<AtomBarGauge>();
        Assert.Equal("floating", defaulted.Find(".atom-bar-gauge").GetAttribute("data-elevation"));

        var flat = Render<AtomBarGauge>(p => p.Add(c => c.Elevation, GaugeElevation.Flat));
        Assert.Equal("flat", flat.Find(".atom-bar-gauge").GetAttribute("data-elevation"));
    }

    [Fact]
    public void CssClass_and_Style_land_on_the_root()
    {
        var cut = Render<AtomBarGauge>(p => p
            .Add(c => c.CssClass, "my-bar")
            .Add(c => c.Style, "opacity:.5"));
        var root = cut.Find(".atom-bar-gauge");
        Assert.Contains("my-bar", root.GetAttribute("class"));
        Assert.Contains("opacity:.5", root.GetAttribute("style"));
    }

    [Fact]
    public void SegmentLabels_renders_one_label_per_entry_instead_of_just_Min_Max()
    {
        var cut = Render<AtomBarGauge>(p => p
            .Add(c => c.SegmentLabels, new[] { "Poor", "Good" })
            .Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        var texts = cut.FindAll(".atom-chart-range-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["Poor", "Good"], texts);
    }

    [Fact]
    public void No_coordinate_anywhere_is_NaN_or_Infinity()
    {
        foreach (var orientation in new[] { ChartOrientation.Horizontal, ChartOrientation.Vertical })
        for (var segments = 1; segments <= 10; segments++)
        {
            var cut = Render<AtomBarGauge>(p => p
                .Add(c => c.Orientation, orientation)
                .Add(c => c.SegmentCount, segments)
                .Add(c => c.Value, 37d));

            Assert.DoesNotContain("NaN", cut.Markup);
            Assert.DoesNotContain("Infinity", cut.Markup);
        }
    }

    private static (double Width, double Height) ViewBoxSize(AngleSharp.Dom.IElement svg)
    {
        var parts = svg.GetAttribute("viewBox")!.Split(' ');
        return (double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
    }
}
