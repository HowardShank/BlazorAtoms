namespace BlazorAtoms.Charts.Tests;

public class AtomDotGaugeTests : BunitContext
{
    [Fact]
    public void Default_dot_count_is_five()
    {
        var cut = Render<AtomDotGauge>();
        Assert.Equal(5, cut.FindAll(".atom-dot-gauge-dot").Count);
    }

    [Fact]
    public void DotCount_overrides_the_default_and_SegmentCount_is_the_fallback()
    {
        var explicitCount = Render<AtomDotGauge>(p => p.Add(c => c.DotCount, 7));
        Assert.Equal(7, explicitCount.FindAll(".atom-dot-gauge-dot").Count);

        var fromSegments = Render<AtomDotGauge>(p => p.Add(c => c.SegmentCount, 8));
        Assert.Equal(8, fromSegments.FindAll(".atom-dot-gauge-dot").Count);
    }

    [Fact]
    public void Dots_grow_from_the_first_to_the_last()
    {
        var cut = Render<AtomDotGauge>(p => p.Add(c => c.DotCount, 5));
        var dots = cut.FindAll(".atom-dot-gauge-dot");

        var radii = dots.Select(d => double.Parse(d.GetAttribute("r")!, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        Assert.Equal(radii, radii.OrderBy(r => r));
        Assert.True(radii[0] < radii[^1]);
    }

    [Fact]
    public void Exactly_one_dot_is_active_and_it_moves_with_the_value()
    {
        var low = Render<AtomDotGauge>(p => p.Add(c => c.DotCount, 5).Add(c => c.Value, 0d));
        var high = Render<AtomDotGauge>(p => p.Add(c => c.DotCount, 5).Add(c => c.Value, 100d));

        Assert.Single(low.FindAll(".atom-dot-gauge-dot"), d => d.GetAttribute("data-active") == "true");
        Assert.Single(high.FindAll(".atom-dot-gauge-dot"), d => d.GetAttribute("data-active") == "true");

        var lowActiveIndex = low.FindAll(".atom-dot-gauge-dot").ToList().FindIndex(d => d.GetAttribute("data-active") == "true");
        var highActiveIndex = high.FindAll(".atom-dot-gauge-dot").ToList().FindIndex(d => d.GetAttribute("data-active") == "true");
        Assert.NotEqual(lowActiveIndex, highActiveIndex);
    }

    [Fact]
    public void Each_dot_carries_its_own_scale_color_as_a_custom_property()
    {
        var cut = Render<AtomDotGauge>(p => p
            .Add(c => c.DotCount, 2)
            .Add(c => c.StartColor, "#ff0000")
            .Add(c => c.EndColor, "#00ff00"));

        var dots = cut.FindAll(".atom-dot-gauge-dot");
        Assert.Contains("--dot-hue:#ff0000", dots[0].GetAttribute("style"));
        Assert.Contains("--dot-hue:#00ff00", dots[1].GetAttribute("style"));
    }

    [Fact]
    public void Horizontal_is_the_default_orientation()
    {
        var cut = Render<AtomDotGauge>();
        Assert.Equal("horizontal", cut.Find(".atom-dot-gauge-wrap").GetAttribute("data-orientation"));
    }

    [Fact]
    public void Vertical_orientation_swaps_the_viewBox_aspect_ratio()
    {
        var horizontal = Render<AtomDotGauge>();
        var vertical = Render<AtomDotGauge>(p => p.Add(c => c.Orientation, ChartOrientation.Vertical));

        var (hw, hh) = ViewBoxSize(horizontal.Find(".atom-dot-gauge-svg"));
        var (vw, vh) = ViewBoxSize(vertical.Find(".atom-dot-gauge-svg"));

        Assert.Equal(hw, vh);
        Assert.Equal(hh, vw);
    }

    [Fact]
    public void ShowPointer_false_omits_the_pointer()
    {
        var cut = Render<AtomDotGauge>(p => p.Add(c => c.ShowPointer, false));
        Assert.Empty(cut.FindAll(".atom-dot-gauge-pointer"));
    }

    [Fact]
    public void Elevation_defaults_to_Floating_and_is_overridable()
    {
        var defaulted = Render<AtomDotGauge>();
        Assert.Equal("floating", defaulted.Find(".atom-dot-gauge").GetAttribute("data-elevation"));

        var flat = Render<AtomDotGauge>(p => p.Add(c => c.Elevation, GaugeElevation.Flat));
        Assert.Equal("flat", flat.Find(".atom-dot-gauge").GetAttribute("data-elevation"));
    }

    [Fact]
    public void SegmentLabels_renders_one_label_per_entry_instead_of_just_Min_Max()
    {
        var cut = Render<AtomDotGauge>(p => p
            .Add(c => c.SegmentLabels, new[] { "Poor", "Fair", "Good" })
            .Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        var texts = cut.FindAll(".atom-chart-range-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["Poor", "Fair", "Good"], texts);
    }

    [Fact]
    public void No_coordinate_anywhere_is_NaN_or_Infinity()
    {
        foreach (var orientation in new[] { ChartOrientation.Horizontal, ChartOrientation.Vertical })
        for (var dots = 2; dots <= 10; dots++)
        {
            var cut = Render<AtomDotGauge>(p => p
                .Add(c => c.Orientation, orientation)
                .Add(c => c.DotCount, dots)
                .Add(c => c.Value, 63d));

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
