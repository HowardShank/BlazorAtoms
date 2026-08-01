namespace BlazorAtoms.Charts.Tests;

public class GaugeColorScaleTests : BunitContext
{
    [Fact]
    public void Lerp_at_the_endpoints_returns_the_endpoints_exactly()
    {
        Assert.Equal("#ff0000", GaugeColorScale.Lerp("#ff0000", "#00ff00", 0));
        Assert.Equal("#00ff00", GaugeColorScale.Lerp("#ff0000", "#00ff00", 1));
    }

    [Fact]
    public void Midpoint_sweeps_hue_through_yellow_not_a_muddy_RGB_average()
    {
        // A naive RGB lerp of pure red and pure green would land on (128,128,0) — an olive/brown, not
        // yellow. Sweeping hue in HSL space instead lands exactly on pure yellow at the midpoint.
        Assert.Equal("#ffff00", GaugeColorScale.Lerp("#ff0000", "#00ff00", 0.5));
    }

    [Fact]
    public void T_is_clamped_to_0_1()
    {
        Assert.Equal(GaugeColorScale.Lerp("#ff0000", "#00ff00", 0), GaugeColorScale.Lerp("#ff0000", "#00ff00", -5));
        Assert.Equal(GaugeColorScale.Lerp("#ff0000", "#00ff00", 1), GaugeColorScale.Lerp("#ff0000", "#00ff00", 5));
    }

    [Fact]
    public void Bands_slices_the_range_into_equal_width_segments_ending_at_Max()
    {
        var bands = GaugeColorScale.Bands(4, 0, 100, "#ff0000", "#00ff00");

        Assert.Equal(4, bands.Count);
        Assert.Equal([25d, 50d, 75d, 100d], bands.Select(b => b.UpTo));
        Assert.Equal("#ff0000", bands[0].Color);
        Assert.Equal("#00ff00", bands[3].Color);
    }

    [Fact]
    public void Bands_returns_empty_for_a_non_positive_count()
    {
        Assert.Empty(GaugeColorScale.Bands(0, 0, 100, "#ff0000", "#00ff00"));
        Assert.Empty(GaugeColorScale.Bands(-1, 0, 100, "#ff0000", "#00ff00"));
    }
}
