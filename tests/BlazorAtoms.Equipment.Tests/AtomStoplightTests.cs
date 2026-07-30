namespace BlazorAtoms.Equipment.Tests;

public class AtomStoplightTests : BunitContext
{
    // ---- structure -----------------------------------------------------------------------------

    [Fact]
    public void Renders_exactly_three_lamps_in_red_yellow_green_order()
    {
        var cut = Render<AtomStoplight>();
        var lamps = cut.FindAll(".atom-stoplight-lamp");
        Assert.Equal(3, lamps.Count);
        Assert.Equal(["red", "yellow", "green"], lamps.Select(l => l.GetAttribute("data-hue")));
    }

    [Fact]
    public void Default_state_is_red_and_only_red_is_active()
    {
        var cut = Render<AtomStoplight>();
        var lamps = cut.FindAll(".atom-stoplight-lamp");
        Assert.Equal("true", lamps[0].GetAttribute("data-active"));
        Assert.Null(lamps[1].GetAttribute("data-active"));
        Assert.Null(lamps[2].GetAttribute("data-active"));
    }

    [Theory]
    [InlineData(StoplightState.Red, 0)]
    [InlineData(StoplightState.Yellow, 1)]
    [InlineData(StoplightState.Green, 2)]
    public void Exactly_one_lamp_is_active_and_it_matches_State(StoplightState state, int activeIndex)
    {
        var cut = Render<AtomStoplight>(p => p.Add(c => c.State, state));
        var lamps = cut.FindAll(".atom-stoplight-lamp");
        for (var i = 0; i < lamps.Count; i++)
        {
            var active = lamps[i].GetAttribute("data-active");
            if (i == activeIndex) Assert.Equal("true", active);
            else Assert.Null(active);
        }
    }

    // ---- orientation -----------------------------------------------------------------------------

    [Fact]
    public void Vertical_is_the_default_orientation()
    {
        var cut = Render<AtomStoplight>();
        Assert.Equal("vertical", cut.Find(".atom-stoplight").GetAttribute("data-orientation"));
    }

    [Fact]
    public void Horizontal_orientation_swaps_the_viewBox_aspect_ratio()
    {
        var vertical = Render<AtomStoplight>();
        var horizontal = Render<AtomStoplight>(p => p.Add(c => c.Orientation, StoplightOrientation.Horizontal));

        var (vw, vh) = ViewBoxSize(vertical.Find(".atom-stoplight"));
        var (hw, hh) = ViewBoxSize(horizontal.Find(".atom-stoplight"));

        Assert.Equal("horizontal", horizontal.Find(".atom-stoplight").GetAttribute("data-orientation"));
        Assert.Equal(vw, hh);
        Assert.Equal(vh, hw);
        Assert.True(vh > vw); // vertical stack reads taller than it is wide
    }

    private static (double Width, double Height) ViewBoxSize(AngleSharp.Dom.IElement svg)
    {
        var parts = svg.GetAttribute("viewBox")!.Split(' ');
        return (double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
    }

    // ---- styling reaches the root ------------------------------------------------------------

    [Fact]
    public void CssClass_and_Style_land_on_the_root_svg()
    {
        var cut = Render<AtomStoplight>(p => p
            .Add(c => c.CssClass, "my-signal")
            .Add(c => c.Style, "opacity:.5"));
        var root = cut.Find(".atom-stoplight");
        Assert.Contains("my-signal", root.GetAttribute("class"));
        Assert.Contains("opacity:.5", root.GetAttribute("style"));
    }

    [Fact]
    public void Width_maps_to_the_stoplight_width_custom_property()
    {
        var cut = Render<AtomStoplight>(p => p.Add(c => c.Width, 200d));
        Assert.Contains("--stoplight-width:200px", cut.Find(".atom-stoplight").GetAttribute("style"));
    }

    // ---- accessibility ------------------------------------------------------------------------

    [Fact]
    public void AriaLabel_defaults_from_State_and_is_overridable()
    {
        var defaulted = Render<AtomStoplight>(p => p.Add(c => c.State, StoplightState.Green));
        Assert.Equal("Stoplight showing Green", defaulted.Find(".atom-stoplight").GetAttribute("aria-label"));

        var overridden = Render<AtomStoplight>(p => p.Add(c => c.AriaLabel, "Intersection signal"));
        Assert.Equal("Intersection signal", overridden.Find(".atom-stoplight").GetAttribute("aria-label"));
    }

    // ---- geometry sanity --------------------------------------------------------------------

    [Fact]
    public void No_coordinate_anywhere_is_NaN_or_negative()
    {
        foreach (var orientation in new[] { StoplightOrientation.Vertical, StoplightOrientation.Horizontal })
        {
            var cut = Render<AtomStoplight>(p => p.Add(c => c.Orientation, orientation));
            var markup = cut.Markup;
            Assert.DoesNotContain("NaN", markup);
            Assert.DoesNotContain("-Infinity", markup);

            foreach (var circle in cut.FindAll("circle"))
                Assert.True(double.Parse(circle.GetAttribute("r")!, System.Globalization.CultureInfo.InvariantCulture) > 0);
        }
    }

    [Fact]
    public void Every_lamp_has_a_visor_path()
    {
        var cut = Render<AtomStoplight>();
        Assert.Equal(3, cut.FindAll(".atom-stoplight-lamp-visor").Count);
        foreach (var visor in cut.FindAll(".atom-stoplight-lamp-visor"))
            Assert.False(string.IsNullOrWhiteSpace(visor.GetAttribute("d")));
    }
}
