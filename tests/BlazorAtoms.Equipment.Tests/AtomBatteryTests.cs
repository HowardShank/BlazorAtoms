using System.Globalization;

namespace BlazorAtoms.Equipment.Tests;

public class AtomBatteryTests : BunitContext
{
    // ---- level / fill ---------------------------------------------------------------------------

    [Fact]
    public void Full_is_the_default_level_and_renders_a_fill_rect()
    {
        var cut = Render<AtomBattery>();
        var root = cut.Find(".atom-battery");
        Assert.Equal("full", root.GetAttribute("data-level"));
        Assert.NotNull(cut.Find(".atom-battery-fill"));
    }

    [Fact]
    public void Empty_level_renders_no_fill_rect()
    {
        var cut = Render<AtomBattery>(p => p.Add(c => c.Level, BatteryLevel.Empty));
        Assert.Equal("empty", cut.Find(".atom-battery").GetAttribute("data-level"));
        Assert.Empty(cut.FindAll(".atom-battery-fill"));
    }

    [Theory]
    [InlineData(BatteryLevel.Quarter, 0.25)]
    [InlineData(BatteryLevel.Half, 0.5)]
    [InlineData(BatteryLevel.ThreeQuarter, 0.75)]
    [InlineData(BatteryLevel.Full, 1.0)]
    public void Fill_width_scales_with_level_for_horizontal_orientation(BatteryLevel level, double fraction)
    {
        var full = Render<AtomBattery>(p => p.Add(c => c.Level, BatteryLevel.Full));
        var cut = Render<AtomBattery>(p => p.Add(c => c.Level, level));

        var fullWidth = double.Parse(full.Find(".atom-battery-fill").GetAttribute("width")!, CultureInfo.InvariantCulture);
        var width = double.Parse(cut.Find(".atom-battery-fill").GetAttribute("width")!, CultureInfo.InvariantCulture);

        Assert.Equal(fullWidth * fraction, width, precision: 2);
    }

    // ---- orientation -----------------------------------------------------------------------------

    [Fact]
    public void Horizontal_is_the_default_orientation()
    {
        var cut = Render<AtomBattery>();
        Assert.Equal("horizontal", cut.Find(".atom-battery").GetAttribute("data-orientation"));
    }

    [Fact]
    public void Vertical_orientation_swaps_the_viewBox_aspect_ratio()
    {
        var horizontal = Render<AtomBattery>();
        var vertical = Render<AtomBattery>(p => p.Add(c => c.Orientation, BatteryOrientation.Vertical));

        var (hw, hh) = ViewBoxSize(horizontal.Find(".atom-battery"));
        var (vw, vh) = ViewBoxSize(vertical.Find(".atom-battery"));

        Assert.Equal("vertical", vertical.Find(".atom-battery").GetAttribute("data-orientation"));
        Assert.Equal(hw, vh);
        Assert.Equal(hh, vw);
        Assert.True(hw > hh); // horizontal shell reads wider than it is tall
    }

    [Fact]
    public void Vertical_fill_grows_upward_from_the_bottom()
    {
        var half = Render<AtomBattery>(p => p
            .Add(c => c.Orientation, BatteryOrientation.Vertical)
            .Add(c => c.Level, BatteryLevel.Half));
        var full = Render<AtomBattery>(p => p
            .Add(c => c.Orientation, BatteryOrientation.Vertical)
            .Add(c => c.Level, BatteryLevel.Full));

        var halfFill = half.Find(".atom-battery-fill");
        var fullFill = full.Find(".atom-battery-fill");

        var halfBottom = double.Parse(halfFill.GetAttribute("y")!, CultureInfo.InvariantCulture)
            + double.Parse(halfFill.GetAttribute("height")!, CultureInfo.InvariantCulture);
        var fullBottom = double.Parse(fullFill.GetAttribute("y")!, CultureInfo.InvariantCulture)
            + double.Parse(fullFill.GetAttribute("height")!, CultureInfo.InvariantCulture);

        Assert.Equal(fullBottom, halfBottom, precision: 2); // same anchor
        Assert.True(double.Parse(halfFill.GetAttribute("y")!, CultureInfo.InvariantCulture)
            > double.Parse(fullFill.GetAttribute("y")!, CultureInfo.InvariantCulture)); // half's top sits lower (shorter bar)
    }

    private static (double Width, double Height) ViewBoxSize(AngleSharp.Dom.IElement svg)
    {
        var parts = svg.GetAttribute("viewBox")!.Split(' ');
        return (double.Parse(parts[2], CultureInfo.InvariantCulture),
                double.Parse(parts[3], CultureInfo.InvariantCulture));
    }

    // ---- status badge ----------------------------------------------------------------------------

    [Fact]
    public void None_status_renders_no_badge()
    {
        var cut = Render<AtomBattery>();
        Assert.Equal("none", cut.Find(".atom-battery").GetAttribute("data-status"));
        Assert.Empty(cut.FindAll(".atom-battery-badge-bg"));
    }

    [Theory]
    [InlineData(BatteryStatus.Charging)]
    [InlineData(BatteryStatus.Warning)]
    [InlineData(BatteryStatus.Error)]
    [InlineData(BatteryStatus.Unknown)]
    [InlineData(BatteryStatus.Check)]
    public void Badge_backed_statuses_render_the_badge_circle(BatteryStatus status)
    {
        var cut = Render<AtomBattery>(p => p.Add(c => c.Status, status));
        Assert.NotEmpty(cut.FindAll(".atom-battery-badge-bg"));
    }

    [Fact]
    public void Slash_status_renders_a_full_icon_diagonal_with_no_badge_circle()
    {
        var cut = Render<AtomBattery>(p => p.Add(c => c.Status, BatteryStatus.Slash));
        Assert.Equal("slash", cut.Find(".atom-battery").GetAttribute("data-status"));
        Assert.Empty(cut.FindAll(".atom-battery-badge-bg"));
        Assert.NotEmpty(cut.FindAll(".atom-battery-slash"));
    }

    [Fact]
    public void Unknown_status_renders_a_question_mark_text_element()
    {
        var cut = Render<AtomBattery>(p => p.Add(c => c.Status, BatteryStatus.Unknown));
        var text = cut.Find(".atom-battery-badge-text");
        Assert.Equal("?", text.TextContent);
    }

    [Fact]
    public void Level_and_status_are_independent()
    {
        var cut = Render<AtomBattery>(p => p
            .Add(c => c.Level, BatteryLevel.Quarter)
            .Add(c => c.Status, BatteryStatus.Charging));
        var root = cut.Find(".atom-battery");
        Assert.Equal("quarter", root.GetAttribute("data-level"));
        Assert.Equal("charging", root.GetAttribute("data-status"));
        Assert.NotEmpty(cut.FindAll(".atom-battery-fill"));
        Assert.NotEmpty(cut.FindAll(".atom-battery-badge-bg"));
    }

    // ---- monochrome -------------------------------------------------------------------------------

    [Fact]
    public void Monochrome_sets_data_mono_attribute()
    {
        var cut = Render<AtomBattery>(p => p.Add(c => c.Monochrome, true));
        Assert.Equal("true", cut.Find(".atom-battery").GetAttribute("data-mono"));
    }

    [Fact]
    public void Monochrome_defaults_to_false_and_omits_the_attribute()
    {
        var cut = Render<AtomBattery>();
        Assert.Null(cut.Find(".atom-battery").GetAttribute("data-mono"));
    }

    // ---- styling reaches the root -------------------------------------------------------------

    [Fact]
    public void CssClass_and_Style_land_on_the_root_svg()
    {
        var cut = Render<AtomBattery>(p => p
            .Add(c => c.CssClass, "my-battery")
            .Add(c => c.Style, "opacity:.5"));
        var root = cut.Find(".atom-battery");
        Assert.Contains("my-battery", root.GetAttribute("class"));
        Assert.Contains("opacity:.5", root.GetAttribute("style"));
    }

    [Fact]
    public void Width_maps_to_the_battery_width_custom_property()
    {
        var cut = Render<AtomBattery>(p => p.Add(c => c.Width, 128d));
        Assert.Contains("--battery-width:128px", cut.Find(".atom-battery").GetAttribute("style"));
    }

    [Fact]
    public void OutlineColor_and_FillColor_map_to_custom_properties()
    {
        var cut = Render<AtomBattery>(p => p
            .Add(c => c.OutlineColor, "#112233")
            .Add(c => c.FillColor, "#445566"));
        var style = cut.Find(".atom-battery").GetAttribute("style")!;
        Assert.Contains("--battery-outline:#112233", style);
        Assert.Contains("--battery-fill:#445566", style);
    }

    // ---- accessibility ------------------------------------------------------------------------

    [Fact]
    public void AriaLabel_defaults_from_Level_and_Status_and_is_overridable()
    {
        var defaulted = Render<AtomBattery>(p => p
            .Add(c => c.Level, BatteryLevel.Half)
            .Add(c => c.Status, BatteryStatus.Charging));
        Assert.Equal("Battery, half charge, charging", defaulted.Find(".atom-battery").GetAttribute("aria-label"));

        var noStatus = Render<AtomBattery>(p => p.Add(c => c.Level, BatteryLevel.Empty));
        Assert.Equal("Battery, empty", noStatus.Find(".atom-battery").GetAttribute("aria-label"));

        var overridden = Render<AtomBattery>(p => p.Add(c => c.AriaLabel, "Laptop battery"));
        Assert.Equal("Laptop battery", overridden.Find(".atom-battery").GetAttribute("aria-label"));
    }

    // ---- geometry sanity --------------------------------------------------------------------

    [Fact]
    public void No_coordinate_anywhere_is_NaN_or_negative()
    {
        foreach (var orientation in new[] { BatteryOrientation.Horizontal, BatteryOrientation.Vertical })
        foreach (var level in Enum.GetValues<BatteryLevel>())
        foreach (var status in Enum.GetValues<BatteryStatus>())
        {
            var cut = Render<AtomBattery>(p => p
                .Add(c => c.Orientation, orientation)
                .Add(c => c.Level, level)
                .Add(c => c.Status, status));
            var markup = cut.Markup;
            Assert.DoesNotContain("NaN", markup);
            Assert.DoesNotContain("-Infinity", markup);

            foreach (var rect in cut.FindAll("rect"))
            {
                Assert.True(double.Parse(rect.GetAttribute("width")!, CultureInfo.InvariantCulture) >= 0);
                Assert.True(double.Parse(rect.GetAttribute("height")!, CultureInfo.InvariantCulture) >= 0);
            }
        }
    }
}
