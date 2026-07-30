namespace BlazorAtoms.Charts.Tests;

/// <summary>
/// The element components themselves: that they are genuinely opt-in, that a caller's
/// <c>CssClass</c>/<c>Style</c> reach them, and that using one outside a chart is harmless.
/// </summary>
public class ChartElementTests : BunitContext
{
    private static readonly double[] Data = [12d, 19, 7, 23, 15, 28];
    private static readonly string[] Months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"];

    // ---- opt-in ---------------------------------------------------------------------------------

    [Fact]
    public void A_chart_with_no_element_slots_draws_only_its_marks()
    {
        // The whole point of replacing the Show* booleans: nothing appears unless it was asked for.
        var cut = Render<AtomLineChart>(p => p.Add(c => c.Values, Data).Add(c => c.Labels, Months));

        Assert.Single(cut.FindAll(".atom-line-chart-line"));
        Assert.Empty(cut.FindAll(".atom-chart-value-axis"));
        Assert.Empty(cut.FindAll(".atom-chart-category-axis"));
        Assert.Empty(cut.FindAll(".atom-chart-gridlines"));
        Assert.Empty(cut.FindAll(".atom-chart-baseline"));
        Assert.Empty(cut.FindAll(".atom-chart-value-labels"));
        Assert.Empty(cut.FindAll(".atom-chart-heading"));
        Assert.Empty(cut.FindAll(".atom-chart-legend"));
        Assert.Empty(cut.FindAll(".atom-chart-caption"));
    }

    [Fact]
    public void Labels_still_name_the_tooltips_without_a_category_axis()
    {
        // Labels are data, not chrome: they belong to the chart either way. The element is what puts them
        // on the axis.
        var cut = Render<AtomLineChart>(p => p.Add(c => c.Values, Data).Add(c => c.Labels, Months));

        Assert.Equal("Jan: 12", cut.FindAll("circle title")[0].TextContent);
        Assert.Empty(cut.FindAll(".atom-chart-category-axis"));
    }

    [Fact]
    public void Each_slot_renders_the_element_it_is_given()
    {
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, Data)
            .Add(c => c.Labels, Months)
            .Add(c => c.Heading, Slot.Of<AtomChartHeading>(
                ("ChildContent", Slot.Text("Revenue")), ("Subtitle", "FY25")))
            .Add(c => c.ValueAxis, Slot.Of<AtomChartValueAxis>())
            .Add(c => c.CategoryAxis, Slot.Of<AtomChartCategoryAxis>())
            .Add(c => c.Gridlines, Slot.Of<AtomChartGridlines>())
            .Add(c => c.Baseline, Slot.Of<AtomChartBaseline>())
            .Add(c => c.ValueLabels, Slot.Of<AtomChartValueLabels>())
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>())
            .Add(c => c.Caption, Slot.Of<AtomChartCaption>(("ChildContent", Slot.Text("Source: ledger")))));

        Assert.Equal("Revenue", cut.Find(".atom-chart-heading-title").TextContent);
        Assert.Equal("FY25", cut.Find(".atom-chart-heading-subtitle").TextContent);
        Assert.NotEmpty(cut.FindAll(".atom-chart-value-axis-label"));
        Assert.Equal(6, cut.FindAll(".atom-chart-category-axis-label").Count);
        Assert.NotEmpty(cut.FindAll(".atom-chart-gridline"));
        Assert.Single(cut.FindAll(".atom-chart-baseline-rule"));
        Assert.Equal(6, cut.FindAll(".atom-chart-value-label").Count);
        Assert.Equal(6, cut.FindAll(".atom-chart-legend-item").Count);
        Assert.Equal("Source: ledger", cut.Find(".atom-chart-caption").TextContent);
    }

    // ---- the styling hooks that motivated all of this --------------------------------------------

    [Theory]
    [InlineData(typeof(AtomChartHeading), ".atom-chart-heading")]
    [InlineData(typeof(AtomChartCaption), ".atom-chart-caption")]
    [InlineData(typeof(AtomChartEmptyState), ".atom-chart-empty-state")]
    [InlineData(typeof(AtomChartCategoryAxis), ".atom-chart-category-axis")]
    [InlineData(typeof(AtomChartAxisTitle), ".atom-chart-axis-title")]
    [InlineData(typeof(AtomChartCenter), ".atom-chart-center")]
    [InlineData(typeof(AtomChartReadout), ".atom-chart-readout")]
    [InlineData(typeof(AtomChartValueAxis), ".atom-chart-value-axis")]
    [InlineData(typeof(AtomChartValueLabels), ".atom-chart-value-labels")]
    [InlineData(typeof(AtomChartGridlines), ".atom-chart-gridlines")]
    [InlineData(typeof(AtomChartBaseline), ".atom-chart-baseline")]
    [InlineData(typeof(AtomChartSliceLabels), ".atom-chart-slice-labels")]
    [InlineData(typeof(AtomChartRangeLabels), ".atom-chart-range-labels")]
    public void CssClass_and_Style_reach_every_element_root(Type element, string selector)
    {
        // This is the request the whole design exists to answer: per-element styling hooks, not just one
        // pair on the chart root.
        var root = RenderStandalone(element).Find(selector);

        Assert.Contains("mine", root.GetAttribute("class")!.Split(' '));
        Assert.Contains("opacity:.5", root.GetAttribute("style"));
    }

    [Fact]
    public void The_legend_takes_CssClass_and_Style_too()
    {
        // Rendered through a chart rather than standalone: with no rows there is nothing to list, so the
        // legend deliberately emits no empty <ul>.
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>(
                ("CssClass", "mine"), ("Style", "opacity:.5"))));

        var root = cut.Find(".atom-chart-legend");
        Assert.Contains("mine", root.GetAttribute("class")!.Split(' '));
        Assert.Contains("opacity:.5", root.GetAttribute("style"));
    }

    // ---- standalone ------------------------------------------------------------------------------

    [Theory]
    [InlineData(typeof(AtomChartValueAxis))]
    [InlineData(typeof(AtomChartCategoryAxis))]
    [InlineData(typeof(AtomChartValueLabels))]
    [InlineData(typeof(AtomChartGridlines))]
    [InlineData(typeof(AtomChartBaseline))]
    [InlineData(typeof(AtomChartSliceLabels))]
    [InlineData(typeof(AtomChartRangeLabels))]
    [InlineData(typeof(AtomChartReadout))]
    [InlineData(typeof(AtomChartLegend))]
    public void An_element_outside_a_chart_draws_nothing_rather_than_throwing(Type element)
    {
        // A markup mistake should not break the page. Same convention as AtomCardSectionBase outside an
        // AtomCard: no context means no data, and no data means no content.
        var cut = RenderStandalone(element);

        Assert.Empty(cut.FindAll("text"));
        Assert.Empty(cut.FindAll("line"));
        Assert.Empty(cut.FindAll("li"));
    }

    // ---- the empty state -------------------------------------------------------------------------

    [Fact]
    public void The_empty_state_shows_only_when_there_is_nothing_to_draw()
    {
        var empty = Render<AtomLineChart>(p => p
            .Add(c => c.Values, Array.Empty<double>())
            .Add(c => c.EmptyState, Slot.Of<AtomChartEmptyState>()));

        var full = Render<AtomLineChart>(p => p
            .Add(c => c.Values, Data)
            .Add(c => c.EmptyState, Slot.Of<AtomChartEmptyState>()));

        Assert.Equal("No data", empty.Find(".atom-chart-empty-state-text").TextContent);
        Assert.Empty(full.FindAll(".atom-chart-empty-state"));
    }

    [Fact]
    public void A_donut_whose_values_cannot_make_a_ring_counts_as_empty()
    {
        // It has data and still cannot draw: an all-zero or all-negative series is the donut's own
        // degenerate case, and the one an empty state should cover.
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 0d, -5 })
            .Add(c => c.EmptyState, Slot.Of<AtomChartEmptyState>()));

        Assert.Single(cut.FindAll(".atom-chart-empty-state"));
    }

    [Fact]
    public void A_gauge_with_no_range_counts_as_empty()
    {
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.Min, 5d)
            .Add(c => c.Max, 5d)
            .Add(c => c.EmptyState, Slot.Of<AtomChartEmptyState>()));

        Assert.Single(cut.FindAll(".atom-chart-empty-state"));
    }

    // ---- the legend, generalised past the donut --------------------------------------------------

    [Fact]
    public void The_legend_works_on_the_series_charts_and_leaves_percentages_off_there()
    {
        // A line chart's values do not sum to anything, so every row would read 0%. The chart reports a
        // share of zero and the legend's auto mode drops the column.
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 10d, 30 })
            .Add(c => c.Labels, new[] { "a", "b" })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>()));

        Assert.Equal(2, cut.FindAll(".atom-chart-legend-item").Count);
        Assert.Equal("10", cut.FindAll(".atom-chart-legend-value")[0].TextContent);
        Assert.Empty(cut.FindAll(".atom-chart-legend-percent"));
    }

    [Fact]
    public void A_donut_legend_shows_percentages_without_being_asked()
    {
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 25d, 75 })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>()));

        var percents = cut.FindAll(".atom-chart-legend-percent").Select(e => e.TextContent).ToArray();
        Assert.Equal(["25%", "75%"], percents);
    }

    [Fact]
    public void ShowPercent_overrides_the_automatic_choice_in_both_directions()
    {
        var forcedOff = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 25d, 75 })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>(("ShowPercent", false))));

        var forcedOn = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 10d, 30 })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>(("ShowPercent", true))));

        Assert.Empty(forcedOff.FindAll(".atom-chart-legend-percent"));
        Assert.Equal(2, forcedOn.FindAll(".atom-chart-legend-percent").Count);
    }

    [Fact]
    public void A_legend_row_with_no_colour_of_its_own_falls_back_to_the_series_colour()
    {
        // A transparent swatch beside a coloured chart looks like a rendering fault.
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 1d })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>()));

        Assert.Contains("--chart-series-color", cut.Find(".atom-chart-legend-swatch").GetAttribute("style"));
    }

    [Fact]
    public void Columns_is_clamped_and_only_emitted_when_it_is_more_than_one()
    {
        var single = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 1d })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>()));

        var many = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 1d })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>(("Columns", 99))));

        Assert.DoesNotContain("--chart-legend-columns", single.Find(".atom-chart-legend").GetAttribute("style") ?? "");
        Assert.Contains("--chart-legend-columns:6", many.Find(".atom-chart-legend").GetAttribute("style"));
    }

    [Fact]
    public void The_legend_sits_beside_a_donut_and_beneath_everything_else()
    {
        // A ring leaves the space beside it free; a 2:1 plot does not.
        var donut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 1d })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>()));

        var line = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>()));

        Assert.Equal("end", donut.Find(".atom-chart-legend-area").GetAttribute("data-placement"));
        Assert.Equal("below", line.Find(".atom-chart-legend-area").GetAttribute("data-placement"));
    }

    [Fact]
    public void LegendPlacement_overrides_the_per_chart_default()
    {
        var cut = Render<AtomDonut>(p => p
            .Add(c => c.Values, new[] { 1d })
            .Add(c => c.Legend, Slot.Of<AtomChartLegend>())
            .Add(c => c.LegendPlacement, ChartLegendPlacement.Below));

        Assert.Equal("below", cut.Find(".atom-chart-legend-area").GetAttribute("data-placement"));
    }

    // ---- gauge readout and centre, now separable -------------------------------------------------

    [Fact]
    public void The_readout_and_the_centre_content_can_coexist()
    {
        // They were one parameter before, where supplying content silently suppressed the readout.
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.Value, 7d)
            .Add(c => c.Readout, Slot.Of<AtomChartReadout>())
            .Add(c => c.Center, Slot.Of<AtomChartCenter>(("ChildContent", Slot.Text("of 100")))));

        Assert.Equal("7", cut.Find(".atom-chart-readout-value").TextContent);
        Assert.Contains("of 100", cut.Find(".atom-chart-center").TextContent);
    }

    [Fact]
    public void The_readout_carries_its_own_offset_so_the_element_can_override_it()
    {
        // The var moved off the chart root onto the readout, which is what lets Offset live on the element
        // while the sweep-aware default still comes from the chart.
        var auto = Render<AtomGauge>(p => p
            .Add(c => c.SweepAngle, 240d)
            .Add(c => c.Readout, Slot.Of<AtomChartReadout>()));

        var overridden = Render<AtomGauge>(p => p
            .Add(c => c.SweepAngle, 240d)
            .Add(c => c.Readout, Slot.Of<AtomChartReadout>(("Offset", 0.4))));

        Assert.Contains("--chart-readout-offset:16%", auto.Find(".atom-chart-readout").GetAttribute("style"));
        Assert.Contains("--chart-readout-offset:40%", overridden.Find(".atom-chart-readout").GetAttribute("style"));
    }

    [Fact]
    public void A_full_circle_keeps_the_readout_centred()
    {
        // There is no gap at the bottom of a closed dial to move into.
        var cut = Render<AtomGauge>(p => p
            .Add(c => c.SweepAngle, 360d)
            .Add(c => c.Readout, Slot.Of<AtomChartReadout>()));

        Assert.Contains("--chart-readout-offset:0", cut.Find(".atom-chart-readout").GetAttribute("style"));
    }

    // ---- axis titles ----------------------------------------------------------------------------

    [Fact]
    public void The_axis_titles_swap_cells_when_the_bars_turn_sideways()
    {
        // For horizontal bars the value axis is the one along the bottom, so the two titles trade places.
        // An element cannot know which slot it is in, so the chart decides.
        var vertical = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.ValueAxisTitle, Slot.Of<AtomChartAxisTitle>(("ChildContent", Slot.Text("Revenue"))))
            .Add(c => c.CategoryAxisTitle, Slot.Of<AtomChartAxisTitle>(("ChildContent", Slot.Text("Month")))));

        var horizontal = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Orientation, ChartOrientation.Horizontal)
            .Add(c => c.ValueAxisTitle, Slot.Of<AtomChartAxisTitle>(("ChildContent", Slot.Text("Revenue"))))
            .Add(c => c.CategoryAxisTitle, Slot.Of<AtomChartAxisTitle>(("ChildContent", Slot.Text("Month")))));

        Assert.Equal("Revenue", vertical.Find(".atom-bar-chart-axis-title-inline").TextContent);
        Assert.Equal("Month", vertical.Find(".atom-bar-chart-axis-title-block").TextContent);

        Assert.Equal("Month", horizontal.Find(".atom-bar-chart-axis-title-inline").TextContent);
        Assert.Equal("Revenue", horizontal.Find(".atom-bar-chart-axis-title-block").TextContent);
    }

    // ---- the hierarchy still refuses meaningless parameters ---------------------------------------

    [Fact]
    public void Sparkline_offers_no_plot_chrome_slots()
    {
        // A sparkline with gridlines is not a sparkline. It keeps the page-furniture slots — a heading or a
        // caption is not a mark on the plot — but the cartesian chrome lives one level down.
        Assert.Null(typeof(AtomSparkline).GetProperty("Gridlines"));
        Assert.Null(typeof(AtomSparkline).GetProperty("Baseline"));
        Assert.Null(typeof(AtomSparkline).GetProperty("ValueAxis"));
        Assert.Null(typeof(AtomSparkline).GetProperty("CategoryAxis"));
        Assert.Null(typeof(AtomSparkline).GetProperty("ValueLabels"));

        Assert.NotNull(typeof(AtomSparkline).GetProperty("Heading"));
        Assert.NotNull(typeof(AtomSparkline).GetProperty("EmptyState"));
    }

    [Fact]
    public void Donut_and_gauge_offer_no_cartesian_slots()
    {
        Assert.Null(typeof(AtomDonut).GetProperty("Gridlines"));
        Assert.Null(typeof(AtomDonut).GetProperty("ValueAxis"));
        Assert.Null(typeof(AtomGauge).GetProperty("Gridlines"));
        Assert.Null(typeof(AtomGauge).GetProperty("CategoryAxis"));
    }

    [Fact]
    public void The_slice_and_range_label_slots_stay_on_the_chart_that_has_them()
    {
        Assert.NotNull(typeof(AtomDonut).GetProperty("SliceLabels"));
        Assert.Null(typeof(AtomGauge).GetProperty("SliceLabels"));

        Assert.NotNull(typeof(AtomGauge).GetProperty("RangeLabels"));
        Assert.Null(typeof(AtomDonut).GetProperty("RangeLabels"));
    }

    // ---- helpers --------------------------------------------------------------------------------

    /// <summary>
    /// Renders one element on its own, with the styling hooks set.
    /// </summary>
    /// <remarks>
    /// A switch rather than reflection: <c>Render&lt;T&gt;</c> is generic over the component type, and the
    /// theory data is a <see cref="Type"/>. Attempting it through a shared generic would need the element
    /// types to have a common base the compiler can infer, which they do have — but
    /// <c>IRenderedComponent&lt;AtomChartElementBase&gt;</c> is not what <c>Render</c> returns.
    /// </remarks>
    private IRenderedComponent<IComponent> RenderStandalone(Type element)
    {
        const string css = "mine";
        const string style = "opacity:.5";

        return element switch
        {
            _ when element == typeof(AtomChartHeading) => Render<AtomChartHeading>(Styled),
            _ when element == typeof(AtomChartCaption) => Render<AtomChartCaption>(Styled),
            _ when element == typeof(AtomChartEmptyState) => Render<AtomChartEmptyState>(Styled),
            _ when element == typeof(AtomChartLegend) => Render<AtomChartLegend>(Styled),
            _ when element == typeof(AtomChartCategoryAxis) => Render<AtomChartCategoryAxis>(Styled),
            _ when element == typeof(AtomChartAxisTitle) => Render<AtomChartAxisTitle>(Styled),
            _ when element == typeof(AtomChartCenter) => Render<AtomChartCenter>(Styled),
            _ when element == typeof(AtomChartReadout) => Render<AtomChartReadout>(Styled),
            _ when element == typeof(AtomChartValueAxis) => Render<AtomChartValueAxis>(Styled),
            _ when element == typeof(AtomChartValueLabels) => Render<AtomChartValueLabels>(Styled),
            _ when element == typeof(AtomChartGridlines) => Render<AtomChartGridlines>(Styled),
            _ when element == typeof(AtomChartBaseline) => Render<AtomChartBaseline>(Styled),
            _ when element == typeof(AtomChartSliceLabels) => Render<AtomChartSliceLabels>(Styled),
            _ => Render<AtomChartRangeLabels>(Styled),
        };

        static void Styled<T>(ComponentParameterCollectionBuilder<T> p) where T : AtomChartElementBase =>
            p.Add(c => c.CssClass, css).Add(c => c.Style, style);
    }
}
