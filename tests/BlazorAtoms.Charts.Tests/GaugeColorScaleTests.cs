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

    [Theory]
    [InlineData("#ff0000", true)]
    [InlineData("ff0000", true)]
    [InlineData("#f00", true)]
    [InlineData("f00", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("R", false)]
    [InlineData("Re", false)]
    [InlineData("Red", false)]
    [InlineData("#ff00", false)]
    [InlineData("#gggggg", false)]
    public void IsValidHex_accepts_only_3_or_6_digit_hex_with_or_without_a_hash(string? input, bool expected)
    {
        Assert.Equal(expected, GaugeColorScale.IsValidHex(input));
    }

    [Theory]
    [InlineData("purple", "#800080")]
    [InlineData("Purple", "#800080")]
    [InlineData("PURPLE", "#800080")]
    [InlineData(" purple ", "#800080")]
    [InlineData("red", "#ff0000")]
    [InlineData("rebeccapurple", "#663399")]
    [InlineData("#ff0000", "#ff0000")]
    [InlineData("ff0000", "#ff0000")]
    [InlineData("#f00", "#f00")]
    public void ResolveHex_accepts_named_CSS_colors_the_same_as_hex(string input, string expectedHex)
    {
        Assert.Equal(expectedHex, GaugeColorScale.ResolveHex(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("R")]
    [InlineData("Re")]
    [InlineData("notacolor")]
    public void ResolveHex_returns_null_for_anything_neither_hex_nor_a_named_color(string? input)
    {
        Assert.Null(GaugeColorScale.ResolveHex(input));
    }

    [Theory]
    [InlineData("R")]
    [InlineData("Re")]
    [InlineData("")]
    [InlineData("#12")]
    public void Lerp_never_throws_on_malformed_input_it_was_not_told_to_validate(string malformed)
    {
        // A live-bound color text field passes every keystroke through, including partial input on the
        // way to a real color — Lerp must not crash mid-render on any of it, even though the *right* fix
        // is validating with IsValidHex before it ever reaches here (which every gauge now does).
        var exception = Record.Exception(() => GaugeColorScale.Lerp(malformed, "#00ff00", 0.5));
        Assert.Null(exception);
    }
}
