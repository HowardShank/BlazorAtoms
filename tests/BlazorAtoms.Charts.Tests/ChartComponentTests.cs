namespace BlazorAtoms.Charts.Tests;

public class ChartComponentTests : BunitContext
{
    // ---- AtomSparkline: no chrome, on purpose -----------------------------------------------------
    // The "which slots does it refuse" half of this lives in ChartElementTests, alongside the rest of the
    // element surface.

    [Fact]
    public void Sparkline_marks_the_latest_value_by_default()
    {
        var cut = Render<AtomSparkline>(p => p.Add(c => c.Values, new[] { 1d, 9, 4 }));

        var point = cut.Find(".atom-sparkline-point");
        Assert.Equal("4", point.QuerySelector("title")!.TextContent);
    }

    [Fact]
    public void Sparkline_area_is_only_drawn_when_asked()
    {
        var without = Render<AtomSparkline>(p => p.Add(c => c.Values, new[] { 1d, 2 }));
        var with = Render<AtomSparkline>(p => p.Add(c => c.Values, new[] { 1d, 2 }).Add(c => c.Fill, true));

        Assert.Empty(without.FindAll(".atom-sparkline-area"));
        Assert.Single(with.FindAll(".atom-sparkline-area"));
    }

    [Fact]
    public void Smooth_produces_curves_and_straight_produces_lines()
    {
        var straight = Render<AtomSparkline>(p => p.Add(c => c.Values, new[] { 1d, 5, 2, 8 }));
        var smooth = Render<AtomSparkline>(p => p.Add(c => c.Values, new[] { 1d, 5, 2, 8 }).Add(c => c.Smooth, true));

        Assert.Contains(" L ", straight.Find(".atom-sparkline-line").GetAttribute("d"));
        Assert.Contains(" C ", smooth.Find(".atom-sparkline-line").GetAttribute("d"));
    }

    [Fact]
    public void The_area_path_closes_back_to_the_baseline()
    {
        var cut = Render<AtomSparkline>(p => p.Add(c => c.Values, new[] { 1d, 2 }).Add(c => c.Fill, true));

        Assert.EndsWith("Z", cut.Find(".atom-sparkline-area").GetAttribute("d"));
    }

    [Fact]
    public void The_line_declares_pathLength_so_the_draw_in_css_needs_no_geometry()
    {
        var cut = Render<AtomSparkline>(p => p.Add(c => c.Values, new[] { 1d, 2 }));

        Assert.Equal("100", cut.Find(".atom-sparkline-line").GetAttribute("pathLength"));
    }

    // ---- AtomLineChart --------------------------------------------------------------------------

    [Fact]
    public void Gridlines_exclude_the_baseline()
    {
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.NiceScale, false)
            .Add(c => c.GridlineCount, 3)
            .Add(c => c.Gridlines, Slot.Of<AtomChartGridlines>())
            .Add(c => c.Baseline, Slot.Of<AtomChartBaseline>()));

        // GridlineCount 3 means 4 intervals (3 interior ticks plus the top), so 4 gridlines — one per tick
        // except the low end, which is the baseline's own line. NiceScale off, because with it on the count
        // follows the nice step instead — see AxisAndLabelTests.
        Assert.Equal(4, cut.FindAll(".atom-chart-gridline").Count);
        var baselineY = cut.Find(".atom-chart-baseline-rule").GetAttribute("y1");
        Assert.DoesNotContain(baselineY, cut.FindAll(".atom-chart-gridline").Select(g => g.GetAttribute("y1")));
    }

    [Fact]
    public void GridlineCount_is_clamped_rather_than_trusted()
    {
        var many = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.NiceScale, false)
            .Add(c => c.GridlineCount, 5000)
            .Add(c => c.Gridlines, Slot.Of<AtomChartGridlines>()));

        // Clamped to 20 intervals, so 21 gridlines — every tick above the baseline.
        Assert.Equal(21, many.FindAll(".atom-chart-gridline").Count);

        var negative = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.GridlineCount, -3)
            .Add(c => c.Gridlines, Slot.Of<AtomChartGridlines>()));

        Assert.Empty(negative.FindAll(".atom-chart-gridline"));
    }

    [Fact]
    public void Dashed_is_the_gridline_default_and_can_be_turned_off()
    {
        // A dashed rule reads as a reading aid; a solid one competes with the data.
        var dashed = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Gridlines, Slot.Of<AtomChartGridlines>()));

        var solid = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Gridlines, Slot.Of<AtomChartGridlines>(("Dashed", false))));

        Assert.Equal("true", dashed.Find(".atom-chart-gridlines").GetAttribute("data-dashed"));
        Assert.False(solid.Find(".atom-chart-gridlines").HasAttribute("data-dashed"));
    }

    [Fact]
    public void Category_labels_render_as_html_beneath_rather_than_svg_text()
    {
        // HTML labels inherit the page font and can ellipsis; SVG text in a scaled viewBox can do neither.
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Labels, new[] { "Jan", "Feb" })
            .Add(c => c.CategoryAxis, Slot.Of<AtomChartCategoryAxis>()));

        var labels = cut.FindAll(".atom-chart-category-axis-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["Jan", "Feb"], labels);
        Assert.Empty(cut.FindAll("svg text.atom-chart-category-axis-label"));
    }

    [Fact]
    public void A_missing_label_keeps_its_slot_so_the_others_stay_under_their_marks()
    {
        // Skipping the empty one would slide every later label off the mark it names.
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2, 3 })
            .Add(c => c.Labels, new[] { "Jan" })
            .Add(c => c.CategoryAxis, Slot.Of<AtomChartCategoryAxis>()));

        var labels = cut.FindAll(".atom-chart-category-axis-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["Jan", "", ""], labels);
    }

    [Fact]
    public void An_empty_series_renders_no_category_labels()
    {
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, Array.Empty<double>())
            .Add(c => c.Labels, new[] { "Jan" })
            .Add(c => c.CategoryAxis, Slot.Of<AtomChartCategoryAxis>()));

        Assert.Empty(cut.FindAll(".atom-chart-category-axis-label"));
    }

    [Fact]
    public void Only_the_line_chart_anchors_its_end_labels_to_the_plot_edges()
    {
        // A line's points span exactly 0..width, so the end labels line up with the edges. A bar is always
        // inset within its own slot by BarGap — first and last included — so its end labels must centre
        // like every other one, not drift outward to the plot edge.
        var line = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2, 3 })
            .Add(c => c.CategoryAxis, Slot.Of<AtomChartCategoryAxis>()));

        var bar = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2, 3 })
            .Add(c => c.CategoryAxis, Slot.Of<AtomChartCategoryAxis>()));

        Assert.Equal("true", line.Find(".atom-chart-category-axis").GetAttribute("data-align-ends"));
        Assert.False(bar.Find(".atom-chart-category-axis").HasAttribute("data-align-ends"));
    }

    // ---- AtomBarChart ---------------------------------------------------------------------------

    [Fact]
    public void Bars_are_zero_based_so_the_smallest_is_not_flattened()
    {
        // Scaled from the data minimum, the 10 bar would have zero height and the others would overstate.
        var cut = Render<AtomBarChart>(p => p.Add(c => c.Values, new[] { 10d, 20 }));

        var heights = cut.FindAll(".atom-bar-chart-bar")
            .Select(b => double.Parse(b.GetAttribute("height")!, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();

        Assert.All(heights, h => Assert.True(h > 0));
        // 10 is half of 20, so its bar is half the height.
        Assert.Equal(heights[1] / 2, heights[0], 1);
    }

    [Fact]
    public void An_explicit_Min_overrides_the_zero_baseline()
    {
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 10d, 20 })
            .Add(c => c.Min, 10d));

        var first = double.Parse(cut.FindAll(".atom-bar-chart-bar")[0].GetAttribute("height")!,
            System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(0, first, 1);
    }

    [Fact]
    public void Negative_bars_hang_below_the_zero_line_and_are_flagged_for_the_animation()
    {
        var cut = Render<AtomBarChart>(p => p.Add(c => c.Values, new[] { 5d, -5 }));

        var bars = cut.FindAll(".atom-bar-chart-bar");
        Assert.False(bars[0].HasAttribute("data-negative"));
        Assert.Equal("true", bars[1].GetAttribute("data-negative"));

        // The negative bar starts where the positive one ends: at the shared zero line.
        var positiveBottom = Y(bars[0]) + H(bars[0]);
        Assert.Equal(positiveBottom, Y(bars[1]), 1);
    }

    [Fact]
    public void Orientation_swaps_the_axis_and_is_exposed_to_css()
    {
        var vertical = Render<AtomBarChart>(p => p.Add(c => c.Values, new[] { 1d, 2 }));
        var horizontal = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Orientation, ChartOrientation.Horizontal));

        Assert.Equal("vertical", vertical.Find(".atom-bar-chart").GetAttribute("data-orientation"));
        Assert.Equal("horizontal", horizontal.Find(".atom-bar-chart").GetAttribute("data-orientation"));

        // Vertical bars share a y and differ in x; horizontal bars do the opposite.
        var v = vertical.FindAll(".atom-bar-chart-bar");
        Assert.NotEqual(v[0].GetAttribute("x"), v[1].GetAttribute("x"));
        var h = horizontal.FindAll(".atom-bar-chart-bar");
        Assert.NotEqual(h[0].GetAttribute("y"), h[1].GetAttribute("y"));
        Assert.Equal(h[0].GetAttribute("x"), h[1].GetAttribute("x"));
    }

    [Fact]
    public void Gridlines_run_across_the_value_axis_in_both_orientations()
    {
        // A gridline parallel to the bars would convey nothing.
        var horizontal = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Orientation, ChartOrientation.Horizontal)
            .Add(c => c.GridlineCount, 2)
            .Add(c => c.Gridlines, Slot.Of<AtomChartGridlines>()));

        foreach (var line in horizontal.FindAll(".atom-chart-gridline"))
        {
            // Vertical lines: same x at both ends.
            Assert.Equal(line.GetAttribute("x1"), line.GetAttribute("x2"));
        }
    }

    [Fact]
    public void BarGap_is_clamped_so_bars_cannot_vanish()
    {
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.BarGap, 50d));

        Assert.All(cut.FindAll(".atom-bar-chart-bar"), b => Assert.True(W(b) > 0));
    }

    // ---- AtomDonut ------------------------------------------------------------------------------

    [Fact]
    public void Slice_lengths_are_shares_of_the_total()
    {
        // pathLength=100 means a slice's dash length is its percentage outright.
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 25d, 25, 50 })
            .Add(c => c.PadAngle, 0d));

        var lengths = cut.FindAll(".atom-donut-slice")
            .Select(s => double.Parse(s.GetAttribute("stroke-dasharray")!.Split(' ')[0],
                System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();

        Assert.Equal([25d, 25, 50], lengths);
    }

    [Fact]
    public void Slices_are_laid_end_to_end()
    {
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 30d, 70 })
            .Add(c => c.PadAngle, 0d));

        var offsets = cut.FindAll(".atom-donut-slice")
            .Select(s => s.GetAttribute("stroke-dashoffset"))
            .ToArray();

        Assert.Equal("0", offsets[0]);
        Assert.Equal("-30", offsets[1]);
    }

    [Fact]
    public void Negative_values_are_dropped_rather_than_drawn_backwards()
    {
        // A negative share of a whole has no meaning; the label indices of the survivors still line up.
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 50d, -10, 50 })
            .Add(c => c.Labels, new[] { "a", "b", "c" }));

        var titles = cut.FindAll(".atom-donut-slice title").Select(t => t.TextContent).ToArray();
        Assert.Equal(2, titles.Length);
        Assert.StartsWith("a:", titles[0]);
        Assert.StartsWith("c:", titles[1]);
    }

    [Fact]
    public void A_zero_total_draws_only_the_track()
    {
        var cut = Render<AtomDonut>(p => p.Add(c => c.Values, new[] { 0d, 0 }));

        Assert.NotNull(cut.Find(".atom-donut-track"));
        Assert.Empty(cut.FindAll(".atom-donut-slice"));
        Assert.Equal("empty donut chart", cut.Find("svg").GetAttribute("aria-label"));
    }

    [Fact]
    public void The_palette_cycles_when_there_are_more_slices_than_colours()
    {
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 1d, 1, 1 })
            .Add(c => c.Palette, new[] { "red", "blue" }));

        var strokes = cut.FindAll(".atom-donut-slice").Select(s => s.GetAttribute("stroke")!).ToArray();
        Assert.Equal(["red", "blue", "red"], strokes);
    }

    [Fact]
    public void An_empty_or_blank_palette_falls_back_instead_of_drawing_colourless_slices()
    {
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 1d })
            .Add(c => c.Palette, new[] { "  ", "" }));

        Assert.False(string.IsNullOrWhiteSpace(cut.Find(".atom-donut-slice").GetAttribute("stroke")));
    }

    [Fact]
    public void Thickness_is_clamped_so_the_ring_cannot_invert_its_own_hole()
    {
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 1d })
            .Add(c => c.Thickness, 500d));

        var r = double.Parse(cut.Find(".atom-donut-track").GetAttribute("r")!,
            System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(r > 0);
    }

    [Fact]
    public void Slice_titles_carry_the_percentage_as_well_as_the_value()
    {
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 1d, 3 })
            .Add(c => c.Labels, new[] { "one", "three" }));

        var titles = cut.FindAll(".atom-donut-slice title").Select(t => t.TextContent).ToArray();
        Assert.Equal("one: 1 (25%)", titles[0]);
        Assert.Equal("three: 3 (75%)", titles[1]);
    }

    [Fact]
    public void The_centre_element_renders_in_the_hole_and_lets_hovers_through_to_the_slices()
    {
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 1d })
            .Add(c => c.Center, Slot.Of<AtomChartCenter>(("ChildContent", Slot.Text("Total")))));

        Assert.Contains("Total", cut.Find(".atom-chart-center").TextContent);
    }

    // ---- AtomGauge ------------------------------------------------------------------------------

    [Fact]
    public void Gauge_takes_a_single_Value_and_no_series()
    {
        // It plots one number, so Values/Labels would be dead weight — the reason it skips the series base.
        Assert.Null(typeof(AtomGauge).GetProperty("Values"));
        Assert.Null(typeof(AtomGauge).GetProperty("Labels"));
        Assert.NotNull(typeof(AtomGauge).GetProperty("Value"));
    }

    [Theory]
    [InlineData(-50, 0)]
    [InlineData(150, 100)]
    [InlineData(42, 42)]
    public void An_out_of_range_Value_pins_to_the_end_of_the_dial(double input, double expected)
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.Value, input)
            .Add(c => c.Readout, Slot.Of<AtomChartReadout>()));

        Assert.Contains(expected.ToString(System.Globalization.CultureInfo.InvariantCulture),
            cut.Find(".atom-chart-readout-value").TextContent);
    }

    [Fact]
    public void A_zero_width_range_reads_as_empty_rather_than_dividing_by_zero()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.Min, 5d)
            .Add(c => c.Max, 5d)
            .Add(c => c.Value, 5d));

        Assert.DoesNotContain("NaN", cut.Markup);
    }

    [Fact]
    public void The_track_covers_only_the_sweep_angle()
    {
        // 240 of 360 degrees is two thirds of the circle, and pathLength=100 makes that 66.667.
        var cut = Render<AtomGauge>(p => p.Add(c => c.SweepAngle, 240d));

        var dash = cut.Find(".atom-gauge-track").GetAttribute("stroke-dasharray")!;
        Assert.StartsWith("66.667", dash);
    }

    [Fact]
    public void SweepAngle_is_clamped()
    {
        var tiny = Render<AtomGauge>(p => p.Add(c => c.SweepAngle, 0d));
        var huge = Render<AtomGauge>(p => p.Add(c => c.SweepAngle, 9000d));

        Assert.DoesNotContain("NaN", tiny.Markup);
        var dash = huge.Find(".atom-gauge-track").GetAttribute("stroke-dasharray")!;
        Assert.StartsWith("100", dash);
    }

    [Fact]
    public void Bands_tile_the_dial_from_the_minimum_upward()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.Bands, new[]
            {
                new GaugeBand(50, "green"),
                new GaugeBand(80, "orange"),
                new GaugeBand(100, "red"),
            })
            .Add(c => c.SweepAngle, 360d));

        var bands = cut.FindAll(".atom-gauge-band");
        Assert.Equal(3, bands.Count);
        // 0-50, 50-80, 80-100 over a full circle => 50, 30, 20 units, starting at 0, 50, 80.
        Assert.StartsWith("50 ", bands[0].GetAttribute("stroke-dasharray"));
        Assert.Equal("-50", bands[1].GetAttribute("stroke-dashoffset"));
        Assert.StartsWith("30 ", bands[1].GetAttribute("stroke-dasharray"));
        Assert.Equal("-80", bands[2].GetAttribute("stroke-dashoffset"));
    }

    [Fact]
    public void Out_of_order_or_out_of_range_bands_do_not_overlap()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.Bands, new[]
            {
                new GaugeBand(80, "green"),
                new GaugeBand(30, "orange"),   // backwards — skipped, not drawn inverted
                new GaugeBand(500, "red"),     // beyond Max — clamped
            }));

        var bands = cut.FindAll(".atom-gauge-band");
        Assert.Equal(2, bands.Count);
        Assert.All(bands, b => Assert.DoesNotContain("-", b.GetAttribute("stroke-dasharray")));
    }

    [Fact]
    public void The_needle_is_on_by_default_and_the_value_arc_is_not()
    {
        var cut = Render<AtomGauge>(p => p.Add(c => c.Value, 50d));

        Assert.Single(cut.FindAll(".atom-gauge-needle"));
        Assert.Empty(cut.FindAll(".atom-gauge-value-arc"));
    }

    [Fact]
    public void The_needle_moves_with_the_value()
    {
        var low = Render<AtomGauge>(p => p.Add(c => c.Value, 0d));
        var high = Render<AtomGauge>(p => p.Add(c => c.Value, 100d));

        var lowTip = (low.Find(".atom-gauge-needle").GetAttribute("x2"),
                      low.Find(".atom-gauge-needle").GetAttribute("y2"));
        var highTip = (high.Find(".atom-gauge-needle").GetAttribute("x2"),
                       high.Find(".atom-gauge-needle").GetAttribute("y2"));

        Assert.NotEqual(lowTip, highTip);
    }

    [Fact]
    public void The_gauge_names_itself_with_its_value_and_range()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.Value, 30d)
            .Add(c => c.Min, 10d)
            .Add(c => c.Max, 50d));

        Assert.Equal("gauge showing 30 of 10 to 50", cut.Find("svg").GetAttribute("aria-label"));
    }

    [Fact]
    public void The_gauge_is_role_img_not_role_meter()
    {
        // role="meter" belongs to BlazorAtoms.Progress' AtomMeter, which implements the ARIA measurement
        // semantics. This is a dial: a graphic.
        var cut = Render<AtomGauge>(p => p.Add(c => c.Value, 1d));

        Assert.Equal("img", cut.Find("svg").GetAttribute("role"));
    }

    // ---- helpers --------------------------------------------------------------------------------

    private static double Y(AngleSharp.Dom.IElement e) =>
        double.Parse(e.GetAttribute("y")!, System.Globalization.CultureInfo.InvariantCulture);

    private static double H(AngleSharp.Dom.IElement e) =>
        double.Parse(e.GetAttribute("height")!, System.Globalization.CultureInfo.InvariantCulture);

    private static double W(AngleSharp.Dom.IElement e) =>
        double.Parse(e.GetAttribute("width")!, System.Globalization.CultureInfo.InvariantCulture);
}
