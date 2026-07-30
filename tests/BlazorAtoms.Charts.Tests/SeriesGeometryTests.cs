namespace BlazorAtoms.Charts.Tests;

/// <summary>
/// The degenerate-input cases, which are ordinary query results rather than caller errors. These are the
/// tests that matter most here: bad geometry does not throw, it silently emits NaN coordinates and the
/// browser draws nothing — a blank chart with a clean console.
/// </summary>
public class SeriesGeometryTests : BunitContext
{
    private static string Markup<T>(BunitContext ctx, Action<ComponentParameterCollectionBuilder<T>> ps)
        where T : class, IComponent => ctx.Render(ps).Markup;

    [Fact]
    public void No_coordinate_anywhere_is_NaN_or_Infinity()
    {
        // One assertion covering every chart and every awkward series, because a single NaN in a path is
        // enough to blank the whole graphic.
        double[][] awkward =
        [
            [],
            [5],
            [3, 3, 3],       // flat: (v - min) / (max - min) divides by zero
            [0, 0, 0],       // flat at zero
            [-4, -9, -1],    // all negative
            [-5, 0, 5],      // straddling zero
            [1e12, 1, 1e-12] // wildly mixed magnitudes
        ];

        foreach (var data in awkward)
        {
            var markups = new[]
            {
                Markup<AtomSparkline>(this, p => p.Add(c => c.Values, data)),
                Markup<AtomLineChart>(this, p => p
                    .Add(c => c.Values, data)
                    .Add(c => c.ValueLabels, Slot.Of<AtomChartValueLabels>())),
                Markup<AtomBarChart>(this, p => p
                    .Add(c => c.Values, data)
                    .Add(c => c.ValueLabels, Slot.Of<AtomChartValueLabels>())),
                Markup<AtomDonut>(this, p => p.Add(c => c.Values, data)),
            };

            foreach (var markup in markups)
            {
                Assert.DoesNotContain("NaN", markup);
                Assert.DoesNotContain("Infinity", markup);
                Assert.DoesNotContain("∞", markup);
            }
        }
    }

    [Fact]
    public void An_empty_series_draws_no_marks_but_still_renders_the_frame()
    {
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, Array.Empty<double>())
            .Add(c => c.Baseline, Slot.Of<AtomChartBaseline>()));

        Assert.Empty(cut.FindAll("path"));
        Assert.Empty(cut.FindAll("circle"));
        // The baseline is chrome, not data: it survives so the chart holds its space.
        Assert.NotNull(cut.Find(".atom-chart-baseline-rule"));
    }

    [Fact]
    public void A_null_series_behaves_exactly_like_an_empty_one()
    {
        var nullValues = Render<AtomSparkline>();
        var emptyValues = Render<AtomSparkline>(p => p.Add(c => c.Values, Array.Empty<double>()));

        Assert.Equal(emptyValues.Markup, nullValues.Markup);
    }

    [Fact]
    public void A_single_point_sits_at_the_horizontal_middle()
    {
        // Not at x=0, where half the marker would be clipped off the edge. The coordinate is in the
        // translated group's frame — half of the 292-unit plot area, which the translate then offsets to
        // the true centre of the 300-unit box.
        var cut = Render<AtomSparkline>(p => p.Add(c => c.Values, new[] { 7d }));

        var point = cut.Find(".atom-sparkline-point");
        Assert.Equal("146", point.GetAttribute("cx"));
    }

    [Fact]
    public void A_flat_series_plots_at_mid_height_rather_than_dividing_by_zero()
    {
        var cut = Render<AtomLineChart>(p => p
            .Add(c => c.Values, new[] { 4d, 4, 4 })
            .Add(c => c.ShowPoints, true));

        // Range collapses to a unit span, so every point lands at the top of it — consistently, and
        // without NaN.
        var ys = cut.FindAll(".atom-line-chart-point").Select(e => e.GetAttribute("cy")).Distinct();
        Assert.Single(ys);
    }

    // ---- labels are advisory --------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(9)]
    public void Any_Labels_length_is_accepted(int labelCount)
    {
        // A cosmetic mismatch must not become an exception on an otherwise fine page.
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 1d, 2, 3 })
            .Add(c => c.Labels, Enumerable.Range(0, labelCount).Select(i => $"L{i}").ToArray()));

        Assert.Equal(3, cut.FindAll(".atom-bar-chart-bar").Count);
    }

    [Fact]
    public void A_lazily_evaluated_series_is_only_enumerated_once_per_instance()
    {
        // Values is often a LINQ query; re-enumerating per mark would re-run it for every point.
        var enumerations = 0;
        IEnumerable<double> Counting()
        {
            enumerations++;
            yield return 1;
            yield return 2;
            yield return 3;
        }

        var query = Counting();
        Render<AtomLineChart>(p => p
            .Add(c => c.Values, query)
            .Add(c => c.ShowPoints, true)
            .Add(c => c.ValueLabels, Slot.Of<AtomChartValueLabels>()));

        Assert.Equal(1, enumerations);
    }

    // ---- titles and naming ----------------------------------------------------------------------

    [Fact]
    public void Every_mark_carries_a_title_for_the_browsers_own_tooltip()
    {
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 10d, 20 })
            .Add(c => c.Labels, new[] { "Alpha", "Beta" }));

        var titles = cut.FindAll(".atom-bar-chart-bar title").Select(t => t.TextContent).ToArray();
        Assert.Equal(["Alpha: 10", "Beta: 20"], titles);
    }

    [Fact]
    public void A_mark_without_a_label_falls_back_to_its_value()
    {
        var cut = Render<AtomBarChart>(p => p.Add(c => c.Values, new[] { 42.5 }));

        Assert.Equal("42.5", cut.Find(".atom-bar-chart-bar title").TextContent);
    }

    [Fact]
    public void The_Formatter_reaches_the_titles()
    {
        var cut = Render<AtomBarChart>(p => p
            .Add(c => c.Values, new[] { 1500d })
            .Add(c => c.Formatter, v => $"${v / 1000:0.0}k"));

        Assert.Equal("$1.5k", cut.Find(".atom-bar-chart-bar title").TextContent);
    }

    [Fact]
    public void Charts_are_role_img_with_a_generated_name()
    {
        // A bag of rects is not describable on its own, so the graphic states the shape of its data.
        var cut = Render<AtomBarChart>(p => p.Add(c => c.Values, new[] { 1d, 5, 3 }));

        var svg = cut.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Equal("bar chart of 3 values from 1 to 5", svg.GetAttribute("aria-label"));
    }

    [Fact]
    public void An_explicit_AriaLabel_wins()
    {
        var cut = Render<AtomSparkline>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.AriaLabel, "Revenue trend"));

        Assert.Equal("Revenue trend", cut.Find("svg").GetAttribute("aria-label"));
    }

    [Fact]
    public void An_empty_chart_says_so_rather_than_claiming_a_range()
    {
        var cut = Render<AtomSparkline>(p => p.Add(c => c.Values, Array.Empty<double>()));

        Assert.Equal("empty sparkline", cut.Find("svg").GetAttribute("aria-label"));
    }

    // ---- shared axes ----------------------------------------------------------------------------

    [Fact]
    public void Animate_false_removes_the_attribute_the_css_hangs_off()
    {
        var on = Render<AtomSparkline>(p => p.Add(c => c.Values, new[] { 1d, 2 }));
        var off = Render<AtomSparkline>(p => p.Add(c => c.Values, new[] { 1d, 2 }).Add(c => c.Animate, false));

        Assert.Equal("true", on.Find("svg").GetAttribute("data-animate"));
        Assert.False(off.Find("svg").HasAttribute("data-animate"));
    }

    [Fact]
    public void Theming_parameters_reach_the_root_as_custom_properties()
    {
        var cut = Render<AtomSparkline>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Width, "20rem")
            .Add(c => c.Height, "4rem")
            .Add(c => c.SeriesColor, "#f00")
            .Add(c => c.Duration, "250ms")
            .Add(c => c.StrokeWidth, 3)
            .Add(c => c.AreaOpacity, 0.5));

        var style = cut.Find(".atom-sparkline").GetAttribute("style")!;
        Assert.Contains("--chart-width:20rem", style);
        Assert.Contains("--chart-height:4rem", style);
        Assert.Contains("--chart-series-color:#f00", style);
        Assert.Contains("--chart-duration:250ms", style);
        Assert.Contains("--chart-stroke-width:3px", style);
        // Unitless: "0.5px" would be an invalid opacity and the declaration would be dropped.
        Assert.Contains("--chart-area-opacity:0.5;", style);
        Assert.DoesNotContain("--chart-area-opacity:0.5px", style);
    }

    [Fact]
    public void Invisible_charts_stay_in_the_dom_as_display_none()
    {
        var cut = Render<AtomSparkline>(p => p
            .Add(c => c.Values, new[] { 1d, 2 })
            .Add(c => c.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-sparkline").GetAttribute("style"));
    }

    [Fact]
    public void Coordinates_are_written_invariant_regardless_of_culture()
    {
        // A locale that writes "0,5" produces coordinates the browser discards outright.
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var cut = Render<AtomSparkline>(p => p.Add(c => c.Values, new[] { 1d, 2.5, 2 }));

            var d = cut.Find(".atom-sparkline-line").GetAttribute("d")!;
            Assert.DoesNotContain(",", d.Replace(" ", ""));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
