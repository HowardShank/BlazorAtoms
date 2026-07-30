namespace BlazorAtoms.Charts.Tests;

/// <summary>
/// The value axis, the legend and the gauge's labels — everything added after the first browser pass showed
/// that a chart with no readable numbers is only half a chart. Each is now an opt-in element, so every case
/// here fills the matching slot; what the elements themselves do with their own parameters lives in
/// <see cref="ChartElementTests"/>.
/// </summary>
public class AxisAndLabelTests : BunitContext
{
    private static double D(string? s) =>
        double.Parse(s!, System.Globalization.CultureInfo.InvariantCulture);

    private static RenderFragment Axis => Slot.Of<AtomChartValueAxis>();
    private static RenderFragment Rules => Slot.Of<AtomChartGridlines>();

    // ---- nice scale -----------------------------------------------------------------------------

    [Fact]
    public void The_auto_range_rounds_outward_to_a_readable_step()
    {
        // 0..28 divided into the requested 5 would tick at 5.6. A nice step of 10 rounds the top to 30 and
        // gives three intervals instead — the count bends so the numbers stay round.
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 12d, 19, 7, 23, 15, 28 })
            .Add(c => c.ValueAxis, Axis));

        var ticks = cut.FindAll(".atom-chart-value-axis-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["0", "10", "20", "30"], ticks);
    }

    [Fact]
    public void Every_tick_is_a_whole_multiple_of_the_step()
    {
        foreach (var data in new[]
                 {
                     new[] { 3d, 7, 11 },
                     new[] { 0.02, 0.07 },
                     new[] { 1200d, 8400 },
                     new[] { -13d, 46 },
                 })
        {
            var cut = Render<AtomLineChart>(p => p
                .Add(c => c.Values, data)
                .Add(c => c.ValueAxis, Axis));

            var ticks = cut.FindAll(".atom-chart-value-axis-label")
                .Select(e => D(e.TextContent)).ToArray();

            var step = ticks[1] - ticks[0];
            foreach (var t in ticks)
            {
                var multiples = t / step;
                Assert.True(Math.Abs(multiples - Math.Round(multiples)) < 1e-6,
                    $"tick {t} is not a multiple of step {step} for [{string.Join(", ", data)}]");
            }
        }
    }

    [Fact]
    public void NiceScale_can_be_turned_off_to_keep_the_range_exactly_on_the_data()
    {
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 7d, 28 })
            .Add(c => c.NiceScale, false)
            .Add(c => c.ValueAxis, Axis));

        var ticks = cut.FindAll(".atom-chart-value-axis-label").Select(e => e.TextContent).ToArray();
        Assert.Equal("7", ticks[0]);
        Assert.Equal("28", ticks[^1]);
    }

    [Fact]
    public void An_explicit_bound_is_never_rounded()
    {
        // A caller who names a bound has made a decision; rounding it would silently override them.
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 12d, 19, 23 })
            .Add(c => c.Min, 11d)
            .Add(c => c.Max, 24d)
            .Add(c => c.ValueAxis, Axis));

        var ticks = cut.FindAll(".atom-chart-value-axis-label").Select(e => e.TextContent).ToArray();
        Assert.Equal("11", ticks[0]);
        Assert.Equal("24", ticks[^1]);
    }

    [Fact]
    public void Nice_scaling_never_produces_a_bad_coordinate()
    {
        foreach (var data in new[]
                 {
                     Array.Empty<double>(),
                     new[] { 0d },
                     new[] { 0d, 0 },
                     new[] { 5d, 5 },
                     new[] { 1e-9, 2e-9 },
                     new[] { 1e9, 2e9 },
                 })
        {
            var markup = Render<AtomLineChart>(p => p
                .Add(c => c.Values, data)
                .Add(c => c.ValueAxis, Axis)
                .Add(c => c.Gridlines, Rules)).Markup;

            Assert.DoesNotContain("NaN", markup);
            Assert.DoesNotContain("Infinity", markup);
        }
    }

    // ---- value axis rendering -------------------------------------------------------------------

    [Fact]
    public void There_is_no_axis_without_the_element()
    {
        var cut = Render<AtomLineChart>(p => p.Add(c => c.Values, new[] { 1d, 2 }));

        Assert.Empty(cut.FindAll(".atom-chart-value-axis"));
        Assert.Empty(cut.FindAll(".atom-chart-value-axis-label"));
    }

    [Fact]
    public void There_is_one_more_tick_than_gridline_intervals()
    {
        // 0..100 with 4 gridlines: a nice step of 20 divides it into 5, so 6 labels including both bounds.
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 0d, 100 })
            .Add(c => c.GridlineCount, 4)
            .Add(c => c.ValueAxis, Axis));

        Assert.Equal(6, cut.FindAll(".atom-chart-value-axis-label").Count);
    }

    [Fact]
    public void Gridlines_land_under_the_tick_labels()
    {
        // The two must agree: a gridline without a label beside it, or vice versa, reads as a bug.
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 0d, 100 })
            .Add(c => c.GridlineCount, 4)
            .Add(c => c.ValueAxis, Axis)
            .Add(c => c.Gridlines, Rules));

        var lineYs = cut.FindAll(".atom-chart-gridline").Select(e => D(e.GetAttribute("y1"))).ToArray();
        var labelYs = cut.FindAll(".atom-chart-value-axis-label")
            .Select(e => D(e.GetAttribute("y")) - 3).ToArray();

        // Every tick except the low end, which is the baseline's own line.
        Assert.Equal(labelYs.Length - 1, lineYs.Length);
        foreach (var y in lineYs) Assert.Contains(y, labelYs);
    }

    [Fact]
    public void The_top_tick_gets_a_gridline_too()
    {
        // Only the bottom is skipped (the baseline's job) — the top tick used to have no line of its own,
        // which read as a rendering bug rather than a design choice.
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 12d, 19, 7, 23, 15, 28 })
            .Add(c => c.Gridlines, Rules)
            .Add(c => c.ValueAxis, Axis));

        var lineYs = cut.FindAll(".atom-chart-gridline").Select(e => D(e.GetAttribute("y1"))).ToArray();
        var topLabelY = cut.FindAll(".atom-chart-value-axis-label")
            .Select(e => D(e.GetAttribute("y")) - 3).Min();

        Assert.Contains(topLabelY, lineYs);
    }

    [Fact]
    public void GridlineCount_is_exact_when_nice_scaling_is_off()
    {
        // NiceScale trades an exact interval count for round numbers; without it the count is honoured
        // precisely. GridlineCount 7 means 8 intervals (7 requested plus the one the top tick always adds),
        // and a line at every tick except the low end, which is the baseline's own line — 8 gridlines.
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.NiceScale, false)
            .Add(c => c.GridlineCount, 7)
            .Add(c => c.Gridlines, Rules));

        Assert.Equal(8, cut.FindAll(".atom-chart-gridline").Count);
    }

    [Fact]
    public void Axis_labels_are_evenly_spaced_and_ascend_up_the_plot()
    {
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 0d, 100 })
            .Add(c => c.GridlineCount, 3)
            .Add(c => c.ValueAxis, Axis));

        var ys = cut.FindAll(".atom-chart-value-axis-label").Select(e => D(e.GetAttribute("y"))).ToArray();
        // Low value first, and low means further down the SVG.
        Assert.True(ys[0] > ys[^1]);

        var gaps = ys.Zip(ys.Skip(1), (a, b) => a - b).ToArray();
        Assert.All(gaps, g => Assert.Equal(gaps[0], g, 3));
    }

    [Fact]
    public void The_gutter_only_takes_space_when_the_axis_element_is_present()
    {
        // Without an axis the plot must not lose width to an empty gutter. Read from the slot, because the
        // chart cannot see a child component during its own render pass — which is the whole reason these
        // are slots rather than flat children.
        var without = Render<AtomLineChart>(p => p.Add(c => c.Values, new[] { 1d, 2 }));
        var with = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.ValueAxis, Axis));

        Assert.Contains("--chart-pad-left:1.875%", without.Find(".atom-line-chart").GetAttribute("style"));
        Assert.Contains("--chart-pad-left:9.375%", with.Find(".atom-line-chart").GetAttribute("style"));
    }

    [Fact]
    public void ValueAxisWidth_widens_the_gutter_for_long_labels()
    {
        // A formatter emitting currency or separators overflows the default 30 units.
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.ValueAxisWidth, 64d)
            .Add(c => c.ValueAxis, Axis));

        Assert.Contains("--chart-pad-left:20%", cut.Find(".atom-line-chart").GetAttribute("style"));
    }

    [Fact]
    public void ValueAxisWidth_does_nothing_without_the_axis_element()
    {
        // It is the gutter, and there is no gutter without an axis to put in it.
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.ValueAxisWidth, 64d));

        Assert.Contains("--chart-pad-left:1.875%", cut.Find(".atom-line-chart").GetAttribute("style"));
    }

    [Fact]
    public void The_category_label_row_insets_by_the_same_amount_as_the_gutter()
    {
        // The pixel-vs-percentage mismatch here is what put the labels 13px out of line before.
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Labels, new[] { "a", "b" })
            .Add(c => c.ValueAxis, Axis)
            .Add(c => c.CategoryAxis, Slot.Of<AtomChartCategoryAxis>()));

        var style = cut.Find(".atom-bar-chart").GetAttribute("style")!;
        Assert.Contains("--chart-pad-left:9.375%", style);
        Assert.Contains("--chart-pad-right:1.875%", style);
    }

    [Fact]
    public void Horizontal_bars_label_the_value_axis_in_html_along_the_bottom()
    {
        // A vertical axis aligns to fractions of the height, which percentage padding cannot express; a
        // horizontal one aligns to the width, which it can. Hence two markup shapes from one element.
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 0d, 50 })
            .Add(c => c.Orientation, ChartOrientation.Horizontal)
            .Add(c => c.GridlineCount, 4)
            .Add(c => c.ValueAxis, Axis));

        Assert.Equal(6, cut.FindAll(".atom-chart-value-axis-tick").Count);
        Assert.Empty(cut.FindAll(".atom-chart-value-axis-label"));
    }

    [Fact]
    public void Vertical_bars_label_it_in_the_svg_gutter_instead()
    {
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 0d, 50 })
            .Add(c => c.ValueAxis, Axis));

        Assert.NotEmpty(cut.FindAll(".atom-chart-value-axis-label"));
        Assert.Empty(cut.FindAll(".atom-chart-value-axis-tick"));
    }

    [Fact]
    public void Horizontal_bars_take_no_left_gutter()
    {
        // Its value axis is along the bottom, so widening the left pad would just shift the bars.
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Orientation, ChartOrientation.Horizontal)
            .Add(c => c.ValueAxis, Axis));

        Assert.Contains("--chart-pad-left:1.875%", cut.Find(".atom-bar-chart").GetAttribute("style"));
    }

    [Fact]
    public void The_axis_respects_the_Formatter()
    {
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 0d, 1000 })
            .Add(c => c.Formatter, v => $"{v / 1000:0.#}k")
            .Add(c => c.ValueAxis, Axis));

        Assert.Equal("1k", cut.FindAll(".atom-chart-value-axis-label")[^1].TextContent);
    }

    [Fact]
    public void An_empty_series_shows_no_tick_labels_at_all()
    {
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, Array.Empty<double>())
            .Add(c => c.ValueAxis, Axis));

        Assert.Empty(cut.FindAll(".atom-chart-value-axis-label"));
    }

    [Fact]
    public void Sparkline_has_no_axis_surface_at_all()
    {
        Assert.Null(typeof(AtomSparkline).GetProperty("ValueAxis"));
        Assert.Null(typeof(AtomSparkline).GetProperty("ValueAxisWidth"));
        Assert.Null(typeof(AtomSparkline).GetProperty("NiceScale"));
    }

    [Fact]
    public void The_topmost_tick_label_has_room_for_its_ascender()
    {
        // It is centred on the plot's top edge, so with only the base 6 units of padding its glyphs reached
        // y≈2 and the viewBox clipped the top of the number. Baseline must clear a 9-unit font's ascent.
        const double ascent = 9;

        // Two separate renders rather than a shared loop: the generic component types have no common base
        // the compiler can infer for a tuple array.
        //
        // The line chart's labels sit inside a translated <g>, so their y attribute is group-relative — the
        // group's own offset has to be added to get the position the viewBox actually clips against.
        var lineCut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 0d, 30 })
            .Add(c => c.ValueAxis, Axis));

        var groupOffsetY = D(System.Text.RegularExpressions.Regex
            .Match(lineCut.Find("svg g").GetAttribute("transform")!, @"translate\([^,]+,\s*([-\d.]+)\)")
            .Groups[1].Value);

        var lineTop = groupOffsetY + lineCut
            .FindAll(".atom-chart-value-axis-label").Select(e => D(e.GetAttribute("y"))).Min();

        var barTop = Render<AtomBarChart>(p => p
                .Add(c => c.Values, new[] { 0d, 30 })
                .Add(c => c.ValueAxis, Axis))
            .FindAll(".atom-chart-value-axis-label").Select(e => D(e.GetAttribute("y"))).Min();

        Assert.True(lineTop >= ascent, $"line chart top baseline at {lineTop}, needs >= {ascent}");
        Assert.True(barTop >= ascent, $"bar chart top baseline at {barTop}, needs >= {ascent}");
    }

    [Fact]
    public void The_leftmost_tick_label_stays_inside_the_gutter()
    {
        // text-anchor="end" means the label grows leftward from its x, so a wide number must still start
        // after 0. Four digits at ~5 units each is the realistic worst case.
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 0d, 4000 })
            .Add(c => c.ValueAxis, Axis));

        var labels = cut.FindAll(".atom-chart-value-axis-label");
        var widest = labels.Max(e => e.TextContent.Length);
        // The group is translated by PadLeft, and x is negative within it, so the absolute right edge is
        // PadLeft - 4. Check the widest label still fits in what remains.
        var rightEdge = 30 - 4;
        Assert.True(widest * 5.5 <= rightEdge,
            $"widest label '{widest} chars' needs {widest * 5.5} units, gutter gives {rightEdge}");
    }

    // ---- viewBox vs CSS aspect-ratio ------------------------------------------------------------

    [Theory]
    [InlineData(typeof(AtomSparkline), "0 0 300 40")]   // CSS: aspect-ratio: 300 / 40
    [InlineData(typeof(AtomLineChart), "0 0 320 160")]  // CSS: aspect-ratio: 2 / 1
    [InlineData(typeof(AtomBarChart), "0 0 320 160")]   // CSS: aspect-ratio: 2 / 1
    [InlineData(typeof(AtomDonut), "0 0 100 100")]      // CSS: aspect-ratio: 1 / 1
    [InlineData(typeof(AtomGauge), "0 0 100 100")]      // CSS: aspect-ratio: 1 / 1
    public void The_viewBox_matches_the_aspect_ratio_its_stylesheet_locks(Type component, string expected)
    {
        // The stylesheet pins each box to its viewBox's ratio, because SVG's default uniform scaling
        // otherwise fits the artwork to whichever axis runs out first — drawing it narrow and centred while
        // the HTML label rows still span the full width, so points sit under nothing. Changing a viewBox
        // without changing the matching aspect-ratio silently reintroduces that, and no layout-free test
        // can see it. This pins the numbers so the pair has to be changed together.
        var svg = Render(component).Find("svg");

        Assert.Equal(expected, svg.GetAttribute("viewBox"));
    }

    private IRenderedComponent<IComponent> Render(Type component) => component switch
    {
        _ when component == typeof(AtomSparkline) => Render<AtomSparkline>(p => p.Add(c => c.Values, Data)),
        _ when component == typeof(AtomLineChart) => Render<AtomLineChart>(p => p.Add(c => c.Values, Data)),
        _ when component == typeof(AtomBarChart) => Render<AtomBarChart>(p => p.Add(c => c.Values, Data)),
        _ when component == typeof(AtomDonut) => Render<AtomDonut>(p => p.Add(c => c.Values, Data)),
        _ => Render<AtomGauge>(p => p.Add(c => c.Value, 5d)),
    };

    private static readonly double[] Data = [1, 5, 3];

    // ---- the legend -----------------------------------------------------------------------------

    [Fact]
    public void There_is_no_legend_without_the_element()
    {
        var cut = Render<AtomDonut>(p => p.Add(c => c.Values, new[] { 1d, 2 }));

        Assert.Empty(cut.FindAll(".atom-chart-legend"));
    }

    [Fact]
    public void The_legend_lists_every_drawn_slice_with_swatch_value_and_percent()
    {
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 45d, 30, 25 })
            .Add(c => c.Labels, new[] { "Direct", "Search", "Social" })
            .Add(c => c.Palette, new[] { "red", "green", "blue" })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>()));

        var items = cut.FindAll(".atom-chart-legend-item");
        Assert.Equal(3, items.Count);

        Assert.Contains("background:red",
            items[0].QuerySelector(".atom-chart-legend-swatch")!.GetAttribute("style"));
        Assert.Equal("Direct", items[0].QuerySelector(".atom-chart-legend-label")!.TextContent);
        Assert.Equal("45", items[0].QuerySelector(".atom-chart-legend-value")!.TextContent);
        Assert.Equal("45%", items[0].QuerySelector(".atom-chart-legend-percent")!.TextContent);
    }

    [Fact]
    public void The_legend_skips_the_values_the_ring_skips()
    {
        // Negatives are not drawn, so listing them would make the legend disagree with the graphic.
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 50d, -10, 50 })
            .Add(c => c.Labels, new[] { "a", "b", "c" })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>()));

        var labels = cut.FindAll(".atom-chart-legend-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["a", "c"], labels);
    }

    [Fact]
    public void Legend_colours_match_the_slices_they_name()
    {
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 1d, 1, 1 })
            .Add(c => c.Palette, new[] { "red", "blue" })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>()));

        var sliceColors = cut.FindAll(".atom-donut-slice").Select(s => s.GetAttribute("stroke")).ToArray();
        var swatchColors = cut.FindAll(".atom-chart-legend-swatch")
            .Select(s => s.GetAttribute("style")!.Replace("background:", "").Trim().TrimEnd(';')).ToArray();

        Assert.Equal(sliceColors, swatchColors);
    }

    [Fact]
    public void An_empty_donut_renders_no_legend_rows()
    {
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 0d, 0 })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>()));

        Assert.Empty(cut.FindAll(".atom-chart-legend-item"));
    }

    // ---- donut slice labels ---------------------------------------------------------------------

    [Fact]
    public void There_are_no_slice_percentages_without_the_element()
    {
        var cut = Render<AtomDonut>(p => p.Add(c => c.Values, new[] { 1d, 1 }));

        Assert.Empty(cut.FindAll(".atom-chart-slice-label"));
    }

    [Fact]
    public void Thin_slices_get_no_on_ring_label()
    {
        // Nothing here can measure text, so a threshold is the honest way to avoid collisions. The value
        // is still reachable through the tooltip and the legend.
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 96d, 2, 2 })
            .Add(c => c.SliceLabels, Slot.Of<AtomChartSliceLabels>()));

        var labels = cut.FindAll(".atom-chart-slice-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["96%"], labels);
        // …but all three slices are drawn, and all three keep their tooltips.
        Assert.Equal(3, cut.FindAll(".atom-donut-slice").Count);
        Assert.Equal(3, cut.FindAll(".atom-donut-slice title").Count);
    }

    [Fact]
    public void The_threshold_is_the_elements_to_set()
    {
        // Dropping a label changes no geometry, which is the line between an element parameter and a chart
        // one — so MinPercent lives on the element and the chart hands over every mark with its share.
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 96d, 2, 2 })
            .Add(c => c.SliceLabels, Slot.Of<AtomChartSliceLabels>(("MinPercent", 0d))));

        Assert.Equal(3, cut.FindAll(".atom-chart-slice-label").Count);
    }

    [Fact]
    public void Slice_labels_sit_outside_the_rotated_group_so_they_stay_upright()
    {
        // Inside it they would inherit the rotation and tilt by StartAngle.
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 50d, 50 })
            .Add(c => c.StartAngle, 90d)
            .Add(c => c.SliceLabels, Slot.Of<AtomChartSliceLabels>()));

        foreach (var label in cut.FindAll(".atom-chart-slice-label"))
        {
            Assert.False(label.HasAttribute("transform"));
            // Its own group, and the group's parent — neither may carry the slices' rotation.
            Assert.Null(label.ParentElement!.GetAttribute("transform"));
            Assert.Null(label.ParentElement!.ParentElement!.GetAttribute("transform"));
        }
    }

    [Fact]
    public void StartAngle_moves_the_slice_labels_with_the_slices()
    {
        var at0 = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 100d })
            .Add(c => c.SliceLabels, Slot.Of<AtomChartSliceLabels>()));
        var at90 = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 100d })
            .Add(c => c.StartAngle, 90d)
            .Add(c => c.SliceLabels, Slot.Of<AtomChartSliceLabels>()));

        Assert.NotEqual(
            at0.Find(".atom-chart-slice-label").GetAttribute("x"),
            at90.Find(".atom-chart-slice-label").GetAttribute("x"));
    }

    // ---- gauge range labels ----------------------------------------------------------------------

    [Fact]
    public void Range_labels_need_the_element_and_then_show_both_bounds()
    {
        var without = Render<AtomGauge>(p => p.Add(c => c.Value, 5d));
        var with = Render<AtomGauge>(p => p
            .Add(c => c.Value, 5d)
            .Add(c => c.Min, 10d)
            .Add(c => c.Max, 90d)
            .Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        Assert.Empty(without.FindAll(".atom-chart-range-label"));
        var labels = with.FindAll(".atom-chart-range-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["10", "90"], labels);
    }

    [Fact]
    public void Range_labels_sit_at_opposite_ends_of_the_arc()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.SweepAngle, 240d)
            .Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        var labels = cut.FindAll(".atom-chart-range-label");
        // Symmetric about the vertical centre line, for a sweep centred on 12 o'clock.
        var x0 = D(labels[0].GetAttribute("x"));
        var x1 = D(labels[1].GetAttribute("x"));
        Assert.Equal(50 - x0, x1 - 50, 3);
        Assert.Equal(D(labels[0].GetAttribute("y")), D(labels[1].GetAttribute("y")), 3);
    }

    [Fact]
    public void A_closed_dial_prints_only_one_range_label()
    {
        // 0° and 360° are the same point, so a Max label would land on top of the Min one.
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.SweepAngle, 360d)
            .Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        Assert.Single(cut.FindAll(".atom-chart-range-label"));
    }

    [Fact]
    public void Range_labels_stay_upright_outside_the_rotated_group()
    {
        var cut = Render<AtomGauge>(p => p.Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        foreach (var label in cut.FindAll(".atom-chart-range-label"))
        {
            Assert.False(label.HasAttribute("transform"));
            Assert.Null(label.ParentElement!.GetAttribute("transform"));
            Assert.Null(label.ParentElement!.ParentElement!.GetAttribute("transform"));
        }
    }

    [Fact]
    public void Range_labels_honour_the_Formatter()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.Max, 1_000_000d)
            .Add(c => c.Formatter, v => $"{v / 1000:0}k")
            .Add(c => c.RangeLabels, Slot.Of<AtomChartRangeLabels>()));

        Assert.Equal("1000k", cut.FindAll(".atom-chart-range-label")[^1].TextContent);
    }
}
