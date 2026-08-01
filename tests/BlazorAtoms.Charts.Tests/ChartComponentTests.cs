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
    public void AtomChartReadout_FontSize_and_Padding_emit_style_vars_the_pill_reads()
    {
        var cut = Render<AtomGauge>(p => p.Add(c => c.Readout, Slot.Of<AtomChartReadout>(
            ("FontSize", 0.9), ("Padding", ".2em .5em"))));

        var style = cut.Find(".atom-chart-readout").GetAttribute("style")!;
        Assert.Contains("--chart-readout-font-size:0.9em", style);
        Assert.Contains("--chart-readout-padding:.2em .5em", style);
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
        // The needle shape itself is drawn at a fixed reference angle now (see NeedleRotationDeg's
        // remarks) — only the wrapping group's own rotate() changes with Value, which is what makes the
        // motion CSS-transitionable instead of the path jumping between values.
        var low = Render<AtomGauge>(p => p.Add(c => c.Value, 0d));
        var high = Render<AtomGauge>(p => p.Add(c => c.Value, 100d));

        Assert.NotEqual(low.Find(".atom-gauge-needle-group").GetAttribute("transform"),
            high.Find(".atom-gauge-needle-group").GetAttribute("transform"));
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

    [Fact]
    public void SegmentCount_auto_generates_bands_and_an_explicit_Bands_list_wins_over_it()
    {
        var auto = Render<AtomGauge>(p => p.Add(c => c.SegmentCount, 4).Add(c => c.SweepAngle, 360d));
        Assert.Equal(4, auto.FindAll(".atom-gauge-band").Count);

        var explicitWins = Render<AtomGauge>(p => p
            .Add(c => c.SegmentCount, 4)
            .Add(c => c.Bands, new[] { new GaugeBand(100, "purple") })
            .Add(c => c.SweepAngle, 360d));
        var bands = explicitWins.FindAll(".atom-gauge-band");
        Assert.Single(bands);
        Assert.Equal("purple", bands[0].GetAttribute("stroke"));
    }

    [Fact]
    public void Untouched_gauge_is_colored_with_4_bands_by_default()
    {
        // Every reference "score gauge" ships colored out of the box — an untouched AtomGauge is not a
        // plain gray track, it defaults to 4 red→green bands.
        var cut = Render<AtomGauge>(p => p.Add(c => c.Value, 50d));
        Assert.Equal(4, cut.FindAll(".atom-gauge-band").Count);
    }

    [Fact]
    public void An_explicit_empty_Bands_list_opts_out_of_bands_entirely()
    {
        var cut = Render<AtomGauge>(p => p.Add(c => c.Bands, Array.Empty<GaugeBand>()));
        Assert.Empty(cut.FindAll(".atom-gauge-band"));
    }

    [Fact]
    public void SegmentCount_bands_sweep_from_StartColor_to_EndColor()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.SegmentCount, 3)
            .Add(c => c.StartColor, "#ff0000")
            .Add(c => c.EndColor, "#00ff00")
            .Add(c => c.SweepAngle, 360d));

        var bands = cut.FindAll(".atom-gauge-band");
        Assert.Equal(3, bands.Count);
        Assert.Equal("#ff0000", bands[0].GetAttribute("stroke"));
        Assert.Equal("#00ff00", bands[2].GetAttribute("stroke"));
        // The middle band is neither endpoint — proof the sweep actually moved, not a flat 2-color split.
        Assert.NotEqual("#ff0000", bands[1].GetAttribute("stroke"));
        Assert.NotEqual("#00ff00", bands[1].GetAttribute("stroke"));
    }

    [Fact]
    public void ReverseColors_swaps_which_end_StartColor_and_EndColor_apply_to()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.SegmentCount, 3)
            .Add(c => c.StartColor, "#ff0000")
            .Add(c => c.EndColor, "#00ff00")
            .Add(c => c.ReverseColors, true)
            .Add(c => c.SweepAngle, 360d));

        var bands = cut.FindAll(".atom-gauge-band");
        Assert.Equal("#00ff00", bands[0].GetAttribute("stroke"));
        Assert.Equal("#ff0000", bands[2].GetAttribute("stroke"));
    }

    [Theory]
    [InlineData("R")]
    [InlineData("Re")]
    public void A_partial_color_typed_into_StartColor_does_not_crash_the_render(string partialInput)
    {
        // A live-bound color text field re-renders on every keystroke — "R" then "Re" on the way to
        // typing "Red" must render *something* rather than throwing mid-render.
        var exception = Record.Exception(() => Render<AtomGauge>(p => p.Add(c => c.StartColor, partialInput)));
        Assert.Null(exception);
    }

    [Fact]
    public void StartColor_accepts_a_named_CSS_color_not_just_hex()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.SegmentCount, 2)
            .Add(c => c.StartColor, "purple")
            .Add(c => c.SweepAngle, 360d));

        var bands = cut.FindAll(".atom-gauge-band");
        Assert.Equal("#800080", bands[0].GetAttribute("stroke"));
    }

    [Fact]
    public void ReverseColors_has_no_effect_on_an_explicit_Bands_list()
    {
        var forward = Render<AtomGauge>(p => p.Add(c => c.Bands, new[] { new GaugeBand(50, "#123456"), new GaugeBand(100, "#abcdef") }));
        var reversed = Render<AtomGauge>(p => p
            .Add(c => c.Bands, new[] { new GaugeBand(50, "#123456"), new GaugeBand(100, "#abcdef") })
            .Add(c => c.ReverseColors, true));

        Assert.Equal(
            forward.FindAll(".atom-gauge-band").Select(b => b.GetAttribute("stroke")),
            reversed.FindAll(".atom-gauge-band").Select(b => b.GetAttribute("stroke")));
    }

    [Fact]
    public void ReverseColors_also_swaps_AtomBarGauge_and_AtomDotGauge_gradient_ends()
    {
        var barForward = Render<AtomBarGauge>(p => p
            .Add(c => c.BarStyle, BarGaugeStyle.Gradient).Add(c => c.StartColor, "#ff0000").Add(c => c.EndColor, "#00ff00"));
        var barReversed = Render<AtomBarGauge>(p => p
            .Add(c => c.BarStyle, BarGaugeStyle.Gradient).Add(c => c.StartColor, "#ff0000").Add(c => c.EndColor, "#00ff00")
            .Add(c => c.ReverseColors, true));

        var forwardStops = barForward.Find(".atom-bar-gauge-svg").QuerySelectorAll("stop");
        var reversedStops = barReversed.Find(".atom-bar-gauge-svg").QuerySelectorAll("stop");
        Assert.Equal("#ff0000", forwardStops[0].GetAttribute("stop-color"));
        Assert.Equal("#00ff00", reversedStops[0].GetAttribute("stop-color"));

        var dotForward = Render<AtomDotGauge>(p => p.Add(c => c.StartColor, "#ff0000").Add(c => c.EndColor, "#00ff00"));
        var dotReversed = Render<AtomDotGauge>(p => p
            .Add(c => c.StartColor, "#ff0000").Add(c => c.EndColor, "#00ff00").Add(c => c.ReverseColors, true));

        var forwardDots = dotForward.FindAll(".atom-dot-gauge-dot");
        var reversedDots = dotReversed.FindAll(".atom-dot-gauge-dot");
        Assert.Equal("#ff0000", forwardDots[0].GetAttribute("style")!.Split(':')[1].TrimEnd(';'));
        Assert.Equal("#00ff00", reversedDots[0].GetAttribute("style")!.Split(':')[1].TrimEnd(';'));
    }

    [Fact]
    public void Elevation_defaults_to_Floating_and_is_overridable()
    {
        var defaulted = Render<AtomGauge>();
        Assert.Equal("floating", defaulted.Find(".atom-gauge").GetAttribute("data-elevation"));

        var flat = Render<AtomGauge>(p => p.Add(c => c.Elevation, GaugeElevation.Flat));
        Assert.Equal("flat", flat.Find(".atom-gauge").GetAttribute("data-elevation"));
    }

    [Fact]
    public void The_dial_always_has_a_face_plate_and_a_bezel_ring()
    {
        var cut = Render<AtomGauge>();
        var face = cut.Find(".atom-gauge-face");
        Assert.True(double.Parse(face.GetAttribute("r")!, System.Globalization.CultureInfo.InvariantCulture) > 0);
        Assert.NotNull(cut.Find(".atom-gauge-bezel"));
    }

    [Theory]
    [InlineData(1d)] // default
    [InlineData(6d)] // max in the playground slider
    [InlineData(0.5d)]
    public void The_bezel_ring_never_exceeds_the_100x100_viewBox_regardless_of_BezelWidth(double bezelWidth)
    {
        // Regression: ArcRadius used to put the band's own outer edge exactly at radius 50 (the viewBox
        // boundary) with zero room left outside it, so BezelRadius (+thickness/2 +1) landed at 51 —
        // always 1 unit past the edge — and the SVG clipped the bezel ring on all four sides. The fix
        // reserves room for the bezel dynamically, so its outer edge must stay inside 50 for any width.
        var cut = Render<AtomGauge>(p => p.Add(c => c.BezelWidth, bezelWidth));

        var bezel = cut.Find(".atom-gauge-bezel");
        var r = double.Parse(bezel.GetAttribute("r")!, System.Globalization.CultureInfo.InvariantCulture);
        var strokeWidth = double.Parse(bezel.GetAttribute("stroke-width")!, System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(r + strokeWidth / 2 < 50, $"bezel outer edge {r + strokeWidth / 2} reaches/exceeds the viewBox boundary (50)");
    }

    [Fact]
    public void FaceColor_maps_to_the_face_color_custom_property()
    {
        var cut = Render<AtomGauge>(p => p.Add(c => c.FaceColor, "#112233"));
        Assert.Contains("--chart-face-color:#112233", cut.Find(".atom-gauge").GetAttribute("style"));
    }

    [Fact]
    public void Triangle_needle_style_draws_a_short_bold_path_with_no_tail_or_extra_hub_layer()
    {
        var cut = Render<AtomGauge>(p => p.Add(c => c.NeedleStyle, GaugeNeedleStyle.Triangle));

        Assert.Single(cut.FindAll(".atom-gauge-needle-triangle"));
        Assert.Empty(cut.FindAll(".atom-gauge-needle"));
        Assert.Empty(cut.FindAll(".atom-gauge-needle-tapered"));
        Assert.Empty(cut.FindAll(".atom-gauge-hub-outer"));
        Assert.False(string.IsNullOrWhiteSpace(cut.Find(".atom-gauge-needle-triangle").GetAttribute("d")));
    }

    [Fact]
    public void RimTab_needle_style_draws_a_tab_with_no_centre_pivot_hub()
    {
        var cut = Render<AtomGauge>(p => p.Add(c => c.NeedleStyle, GaugeNeedleStyle.RimTab));

        Assert.Single(cut.FindAll(".atom-gauge-rim-tab"));
        Assert.Empty(cut.FindAll(".atom-gauge-hub"));
        Assert.Empty(cut.FindAll(".atom-gauge-hub-outer"));
        Assert.False(string.IsNullOrWhiteSpace(cut.Find(".atom-gauge-rim-tab").GetAttribute("d")));
    }

    [Theory]
    [InlineData(GaugeNeedleStyle.Triangle)]
    [InlineData(GaugeNeedleStyle.RimTab)]
    public void New_needle_styles_move_with_the_value(GaugeNeedleStyle style)
    {
        var low = Render<AtomGauge>(p => p.Add(c => c.NeedleStyle, style).Add(c => c.Value, 0d));
        var high = Render<AtomGauge>(p => p.Add(c => c.NeedleStyle, style).Add(c => c.Value, 100d));

        // Every needle style's own path is fixed now — the wrapping group's rotate() is what moves.
        Assert.NotEqual(low.Find(".atom-gauge-needle-group").GetAttribute("transform"),
            high.Find(".atom-gauge-needle-group").GetAttribute("transform"));
    }

    [Fact]
    public void Banner_slot_renders_when_filled_and_stays_empty_otherwise()
    {
        var empty = Render<AtomGauge>();
        Assert.Empty(empty.FindAll(".atom-chart-banner"));

        var filled = Render<AtomGauge>(p => p.Add(c => c.Banner,
            Slot.Of<AtomChartBanner>(("ChildContent", Slot.Text("Score")))));
        Assert.Equal("Score", filled.Find(".atom-chart-banner").TextContent);
    }

    [Fact]
    public void ShowPedestal_defaults_to_false_and_draws_a_platform_when_enabled()
    {
        var defaulted = Render<AtomGauge>();
        Assert.Empty(defaulted.FindAll(".atom-gauge-pedestal"));

        var withPedestal = Render<AtomGauge>(p => p.Add(c => c.ShowPedestal, true));
        var pedestal = withPedestal.Find(".atom-gauge-pedestal");
        Assert.True(double.Parse(pedestal.GetAttribute("width")!, System.Globalization.CultureInfo.InvariantCulture) > 0);
    }

    [Fact]
    public void BezelColor_and_BezelWidth_reach_the_bezel_ring()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.BezelColor, "#445566")
            .Add(c => c.BezelWidth, 4d));

        Assert.Contains("--chart-bezel-color:#445566", cut.Find(".atom-gauge").GetAttribute("style"));
        Assert.Equal("4", cut.Find(".atom-gauge-bezel").GetAttribute("stroke-width"));
    }

    [Fact]
    public void Line_is_the_default_needle_style()
    {
        var cut = Render<AtomGauge>();
        Assert.Single(cut.FindAll(".atom-gauge-needle"));
        Assert.Empty(cut.FindAll(".atom-gauge-needle-tapered"));
    }

    [Fact]
    public void Tapered_needle_style_draws_a_dart_a_tail_and_a_two_layer_hub_instead_of_the_plain_line()
    {
        var cut = Render<AtomGauge>(p => p.Add(c => c.NeedleStyle, GaugeNeedleStyle.Tapered));

        Assert.Empty(cut.FindAll(".atom-gauge-needle"));
        Assert.Empty(cut.FindAll(".atom-gauge-hub"));
        Assert.Single(cut.FindAll(".atom-gauge-needle-tapered"));
        Assert.Single(cut.FindAll(".atom-gauge-needle-tail"));
        Assert.Single(cut.FindAll(".atom-gauge-hub-outer"));
        Assert.Single(cut.FindAll(".atom-gauge-hub-inner"));
        Assert.False(string.IsNullOrWhiteSpace(cut.Find(".atom-gauge-needle-tapered").GetAttribute("d")));
    }

    [Fact]
    public void Tapered_needle_moves_with_the_value()
    {
        var low = Render<AtomGauge>(p => p.Add(c => c.NeedleStyle, GaugeNeedleStyle.Tapered).Add(c => c.Value, 0d));
        var high = Render<AtomGauge>(p => p.Add(c => c.NeedleStyle, GaugeNeedleStyle.Tapered).Add(c => c.Value, 100d));

        Assert.NotEqual(low.Find(".atom-gauge-needle-group").GetAttribute("transform"),
            high.Find(".atom-gauge-needle-group").GetAttribute("transform"));
    }

    [Fact]
    public void Needle_own_shape_stays_constant_across_values_only_the_wrapping_group_rotates()
    {
        // The point of the refactor: a needle's own path/endpoint is a fixed reference shape now, so CSS
        // can transition the wrapping group's transform instead of the shape jumping between values.
        var low = Render<AtomGauge>(p => p.Add(c => c.Value, 0d));
        var high = Render<AtomGauge>(p => p.Add(c => c.Value, 100d));

        Assert.Equal(low.Find(".atom-gauge-needle").GetAttribute("x2"), high.Find(".atom-gauge-needle").GetAttribute("x2"));
        Assert.Equal(low.Find(".atom-gauge-needle").GetAttribute("y2"), high.Find(".atom-gauge-needle").GetAttribute("y2"));
    }

    [Fact]
    public void RedlineFrom_null_draws_no_redline_and_a_value_draws_one_ending_at_Max()
    {
        var none = Render<AtomGauge>();
        Assert.Empty(none.FindAll(".atom-gauge-redline"));

        var cut = Render<AtomGauge>(p => p.Add(c => c.Min, 0d).Add(c => c.Max, 100d).Add(c => c.RedlineFrom, 80d));
        var redline = cut.Find(".atom-gauge-redline");
        var length = double.Parse(redline.GetAttribute("stroke-dasharray")!.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
        // 80..100 is a fifth of the 0..100 range, so the redline's own dash length should be ~1/5 of TrackLength.
        var trackLength = double.Parse(cut.Find(".atom-gauge-track").GetAttribute("stroke-dasharray")!.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(length, trackLength * 0.15, trackLength * 0.25);
    }

    [Fact]
    public void RedlineFrom_at_or_past_Max_draws_nothing()
    {
        var cut = Render<AtomGauge>(p => p.Add(c => c.Min, 0d).Add(c => c.Max, 100d).Add(c => c.RedlineFrom, 150d));
        Assert.Empty(cut.FindAll(".atom-gauge-redline"));
    }

    [Fact]
    public void ShowTickRuler_off_by_default_draws_no_ruler_marks()
    {
        var cut = Render<AtomGauge>();
        Assert.Empty(cut.FindAll(".atom-gauge-tick-ruler-mark"));
    }

    [Fact]
    public void ShowTickRuler_draws_major_and_minor_marks_and_major_numbers_via_RangeLabels()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.ShowTickRuler, true)
            .Add(c => c.MajorTickCount, 6)
            .Add(c => c.MinorTicksPerMajor, 4)
            .Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        var marks = cut.FindAll(".atom-gauge-tick-ruler-mark");
        // (6 - 1) major intervals * (4 minor + 1 major) each = 25 endpoints total, +1 for the very last one.
        Assert.Equal(26, marks.Count);
        Assert.Equal(6, marks.Count(m => m.GetAttribute("data-major") == "true"));

        var labels = cut.FindAll(".atom-chart-range-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["0", "20", "40", "60", "80", "100"], labels);
    }

    [Fact]
    public void SegmentLabels_takes_precedence_over_ShowTickRuler_when_both_are_set()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.ShowTickRuler, true)
            .Add(c => c.SegmentLabels, new[] { "Low", "High" })
            .Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        var labels = cut.FindAll(".atom-chart-range-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["Low", "High"], labels);
    }

    [Fact]
    public void ArcStyle_Segmented_is_the_default_and_draws_bands_not_gradient_slices_or_ticks()
    {
        var cut = Render<AtomGauge>();

        Assert.NotEmpty(cut.FindAll(".atom-gauge-band"));
        Assert.Empty(cut.FindAll(".atom-gauge-arc-tick"));
    }

    [Fact]
    public void ArcStyle_Gradient_draws_many_slices_and_ignores_EffectiveBands_shape()
    {
        var cut = Render<AtomGauge>(p => p.Add(c => c.ArcStyle, GaugeArcStyle.Gradient));

        // Same element class as Segmented bands (a colored arc slice), just far more of them, and not
        // clamped to the small band count SegmentCount/EffectiveBands would produce.
        Assert.True(cut.FindAll(".atom-gauge-band").Count > 10);
    }

    [Fact]
    public void ArcStyle_Ticks_draws_radial_marks_with_one_active_near_the_value()
    {
        var cut = Render<AtomGauge>(p => p.Add(c => c.ArcStyle, GaugeArcStyle.Ticks).Add(c => c.Value, 100d));

        var ticks = cut.FindAll(".atom-gauge-arc-tick");
        Assert.NotEmpty(ticks);
        Assert.Single(ticks, t => t.GetAttribute("data-active") == "true");
        // At Value == Max, the active tick is the last one — not some mid-scale tick.
        Assert.Equal("true", ticks[^1].GetAttribute("data-active"));
    }

    [Fact]
    public void SegmentLabels_renders_one_label_per_entry_instead_of_just_Min_Max()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.SegmentLabels, new[] { "Poor", "Fair", "Good", "Excellent" })
            .Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        var texts = cut.FindAll(".atom-chart-range-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["Poor", "Fair", "Good", "Excellent"], texts);
    }

    [Fact]
    public void No_SegmentLabels_keeps_the_original_Min_Max_range_labels()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.Min, 10d).Add(c => c.Max, 50d)
            .Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        var texts = cut.FindAll(".atom-chart-range-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["10", "50"], texts);
    }

    [Theory]
    [InlineData(true, 20d)]
    [InlineData(false, 20d)]
    [InlineData(true, 60d)]
    [InlineData(false, 8d)]
    public void AtomDotGauge_range_labels_never_exceed_the_viewBox_regardless_of_pointer_or_dot_size(
        bool showPointer, double dotSize)
    {
        var cut = Render<AtomDotGauge>(p => p
            .Add(c => c.ShowPointer, showPointer)
            .Add(c => c.DotSize, dotSize)
            .Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        var viewBox = cut.Find(".atom-dot-gauge-svg").GetAttribute("viewBox")!
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => double.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var viewBoxHeight = viewBox[3];

        foreach (var label in cut.FindAll(".atom-chart-range-label"))
        {
            var y = double.Parse(label.GetAttribute("y")!, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(y < viewBoxHeight, $"label y {y} reaches/exceeds the viewBox height ({viewBoxHeight})");
        }
    }

    [Fact]
    public void AtomBarGauge_range_labels_never_exceed_the_viewBox()
    {
        var cut = Render<AtomBarGauge>(p => p.Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        var viewBox = cut.Find(".atom-bar-gauge-svg").GetAttribute("viewBox")!
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => double.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var viewBoxHeight = viewBox[3];

        foreach (var label in cut.FindAll(".atom-chart-range-label"))
        {
            var y = double.Parse(label.GetAttribute("y")!, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(y < viewBoxHeight, $"label y {y} reaches/exceeds the viewBox height ({viewBoxHeight})");
        }
    }

    [Theory]
    [InlineData(BarGaugeStyle.Segmented, ChartOrientation.Horizontal)]
    [InlineData(BarGaugeStyle.Gradient, ChartOrientation.Horizontal)]
    [InlineData(BarGaugeStyle.Ticks, ChartOrientation.Horizontal)]
    [InlineData(BarGaugeStyle.Segmented, ChartOrientation.Vertical)]
    [InlineData(BarGaugeStyle.Gradient, ChartOrientation.Vertical)]
    [InlineData(BarGaugeStyle.Ticks, ChartOrientation.Vertical)]
    public void AtomBarGauge_band_fill_is_clipped_inside_the_track_not_drawn_over_its_full_bounds(
        BarGaugeStyle style, ChartOrientation orientation)
    {
        var cut = Render<AtomBarGauge>(p => p.Add(c => c.BarStyle, style).Add(c => c.Orientation, orientation));

        var track = cut.Find(".atom-bar-gauge-track");
        var trackX = double.Parse(track.GetAttribute("x")!, System.Globalization.CultureInfo.InvariantCulture);
        var trackY = double.Parse(track.GetAttribute("y")!, System.Globalization.CultureInfo.InvariantCulture);
        var trackW = double.Parse(track.GetAttribute("width")!, System.Globalization.CultureInfo.InvariantCulture);
        var trackH = double.Parse(track.GetAttribute("height")!, System.Globalization.CultureInfo.InvariantCulture);

        var group = track.NextElementSibling;
        Assert.NotNull(group);
        var clipAttr = group!.GetAttribute("clip-path");
        Assert.False(string.IsNullOrEmpty(clipAttr));

        var clipId = clipAttr!.Replace("url(#", "").TrimEnd(')');
        var clipRect = cut.Find($"clipPath#{clipId} > rect");
        var clipX = double.Parse(clipRect.GetAttribute("x")!, System.Globalization.CultureInfo.InvariantCulture);
        var clipY = double.Parse(clipRect.GetAttribute("y")!, System.Globalization.CultureInfo.InvariantCulture);
        var clipW = double.Parse(clipRect.GetAttribute("width")!, System.Globalization.CultureInfo.InvariantCulture);
        var clipH = double.Parse(clipRect.GetAttribute("height")!, System.Globalization.CultureInfo.InvariantCulture);

        // The inset applies on both axes regardless of orientation — assert both, not just the one that
        // happens to be the track's "long" axis, so a Vertical-only regression can't slip through.
        Assert.True(clipX > trackX, $"clip x {clipX} is not inset from the track's own x {trackX}");
        Assert.True(clipX + clipW < trackX + trackW,
            $"clip's far edge (x) {clipX + clipW} is not inset from the track's own far edge {trackX + trackW}");
        Assert.True(clipY > trackY, $"clip y {clipY} is not inset from the track's own y {trackY}");
        Assert.True(clipY + clipH < trackY + trackH,
            $"clip's far edge (y) {clipY + clipH} is not inset from the track's own far edge {trackY + trackH}");
    }

    // ---- helpers --------------------------------------------------------------------------------

    private static double Y(AngleSharp.Dom.IElement e) =>
        double.Parse(e.GetAttribute("y")!, System.Globalization.CultureInfo.InvariantCulture);

    private static double H(AngleSharp.Dom.IElement e) =>
        double.Parse(e.GetAttribute("height")!, System.Globalization.CultureInfo.InvariantCulture);

    private static double W(AngleSharp.Dom.IElement e) =>
        double.Parse(e.GetAttribute("width")!, System.Globalization.CultureInfo.InvariantCulture);
}
