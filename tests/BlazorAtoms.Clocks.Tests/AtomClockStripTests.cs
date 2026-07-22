using System;
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Clocks.Tests;

public class AtomClockStripTests : BunitContext
{
    // Live=false so the strip doesn't tick; Loose JS so viewer detection is a no-op → UTC.
    private IRenderedComponent<AtomClockStrip> Render(Action<ComponentParameterCollectionBuilder<AtomClockStrip>>? extra = null)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        return Render<AtomClockStrip>(p =>
        {
            p.Add(c => c.Live, false);
            extra?.Invoke(p);
        });
    }

    [Fact]
    public void Default_zones_render_one_cell_each()
    {
        var cut = Render();
        Assert.Equal(ClockZone.Default.Count, cut.FindAll(".cstrip-cell").Count);
        Assert.Equal("group", cut.Find(".atom-clock-strip").GetAttribute("role"));
    }

    [Fact]
    public void Custom_zones_replace_defaults()
    {
        var zones = new[] { new ClockZone("A", "UTC"), new ClockZone("B", "Asia/Tokyo") };
        var cut = Render(p => p.Add(c => c.Zones, zones));
        Assert.Equal(2, cut.FindAll(".cstrip-cell").Count);
    }

    [Fact]
    public void Digital_face_renders_digital_clocks()
    {
        var cut = Render(p => p.Add(c => c.Face, ClockFace.Digital));
        Assert.Equal("digital", cut.Find(".atom-clock-strip").GetAttribute("data-face"));
        Assert.NotEmpty(cut.FindAll(".cstrip-cell .atom-clock"));
        Assert.Empty(cut.FindAll(".cstrip-cell .atom-analog-clock"));
    }

    [Fact]
    public void Analog_face_renders_analog_clocks()
    {
        var cut = Render(p => p.Add(c => c.Face, ClockFace.Analog));
        Assert.Equal("analog", cut.Find(".atom-clock-strip").GetAttribute("data-face"));
        Assert.NotEmpty(cut.FindAll(".cstrip-cell .atom-analog-clock"));
        Assert.Empty(cut.FindAll(".cstrip-cell .atom-clock"));
    }

    [Theory]
    [InlineData(ClockStripLayout.Row, "row")]
    [InlineData(ClockStripLayout.Grid, "grid")]
    [InlineData(ClockStripLayout.Stacked, "stacked")]
    public void Layout_sets_data_attribute(ClockStripLayout layout, string expected)
    {
        var cut = Render(p => p.Add(c => c.Layout, layout));
        Assert.Equal(expected, cut.Find(".atom-clock-strip").GetAttribute("data-layout"));
    }

    [Fact]
    public void Sort_by_offset_orders_west_to_east()
    {
        // Honolulu (UTC-10) < Tokyo (UTC+9) < Sydney (UTC+10/+11). Labels are the clock captions.
        var zones = new[]
        {
            new ClockZone("Tokyo", "Asia/Tokyo"),
            new ClockZone("Honolulu", "Pacific/Honolulu"),
            new ClockZone("Sydney", "Australia/Sydney"),
        };
        var cut = Render(p => p.Add(c => c.Zones, zones).Add(c => c.SortByOffset, true));
        var labels = cut.FindAll(".cstrip-cell .atom-clock-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(new[] { "Honolulu", "Tokyo", "Sydney" }, labels);
    }

    [Fact]
    public void Unsorted_keeps_input_order()
    {
        var zones = new[]
        {
            new ClockZone("Tokyo", "Asia/Tokyo"),
            new ClockZone("Honolulu", "Pacific/Honolulu"),
        };
        var cut = Render(p => p.Add(c => c.Zones, zones));
        var labels = cut.FindAll(".cstrip-cell .atom-clock-label").Select(e => e.TextContent).ToArray();
        Assert.Equal(new[] { "Tokyo", "Honolulu" }, labels);
    }

    [Fact]
    public void Relative_offset_renders_signed_badge()
    {
        var zones = new[]
        {
            new ClockZone("UTC", "UTC"),
            new ClockZone("Tokyo", "Asia/Tokyo"),
        };
        var cut = Render(p => p
            .Add(c => c.Zones, zones)
            .Add(c => c.ShowRelativeOffset, true)
            .Add(c => c.ReferenceTimeZoneId, "UTC"));

        var badges = cut.FindAll(".cstrip-offset").Select(e => e.TextContent).ToArray();
        Assert.Equal(2, badges.Length);
        Assert.Contains("±0", badges);                       // UTC vs UTC
        Assert.Contains(badges, b => b == "+9h");            // Tokyo is UTC+9 year-round
    }

    [Fact]
    public void No_offset_badge_by_default()
    {
        Assert.Empty(Render().FindAll(".cstrip-offset"));
    }

    [Fact]
    public void Selectable_click_raises_and_marks_selected()
    {
        ClockZone? got = null;
        var zones = new[] { new ClockZone("Tokyo", "Asia/Tokyo"), new ClockZone("UTC", "UTC") };
        var cut = Render(p => p
            .Add(c => c.Zones, zones)
            .Add(c => c.Selectable, true)
            .Add(c => c.OnSelect, EventCallback.Factory.Create<ClockZone>(this, z => got = z)));

        cut.FindAll(".cstrip-cell")[0].Click();
        Assert.Equal("Asia/Tokyo", got?.TimeZoneId);
        Assert.Contains("is-selected", cut.FindAll(".cstrip-cell")[0].GetAttribute("class"));
    }

    [Fact]
    public void Not_selectable_click_is_noop()
    {
        ClockZone? got = null;
        var cut = Render(p => p
            .Add(c => c.OnSelect, EventCallback.Factory.Create<ClockZone>(this, z => got = z)));
        cut.FindAll(".cstrip-cell")[0].Click();
        Assert.Null(got);
    }

    [Fact]
    public void Viewer_highlight_renders_under_loose_js()
    {
        var cut = Render(p => p.Add(c => c.HighlightViewerZone, true));
        Assert.Equal(ClockZone.Default.Count, cut.FindAll(".cstrip-cell").Count);
    }

    [Fact]
    public void Analog_options_pass_through_to_cells()
    {
        var zones = new[] { new ClockZone("A", "UTC"), new ClockZone("B", "Asia/Tokyo") };

        var plain = Render(p => p.Add(c => c.Zones, zones).Add(c => c.Face, ClockFace.Analog));
        Assert.Empty(plain.FindAll(".cstrip-cell svg text"));           // numerals off by default
        Assert.NotEmpty(plain.FindAll(".cstrip-cell line.aclk-hand-sec")); // second hand on by default

        var opts = Render(p => p
            .Add(c => c.Zones, zones)
            .Add(c => c.Face, ClockFace.Analog)
            .Add(c => c.ShowNumerals, true)
            .Add(c => c.ShowSeconds, false));
        Assert.Equal(24, opts.FindAll(".cstrip-cell svg text").Count);  // 12 numerals × 2 cells
        Assert.Empty(opts.FindAll(".cstrip-cell line.aclk-hand-sec"));  // second hand off
    }

    [Fact]
    public void Gap_and_highlight_tokens_emitted()
    {
        var cut = Render(p => p.Add(c => c.Gap, 24).Add(c => c.HighlightColor, "#f0f"));
        var style = cut.Find(".atom-clock-strip").GetAttribute("style") ?? "";
        Assert.Contains("--cstrip-gap:24px", style);
        Assert.Contains("--cstrip-highlight:#f0f", style);
    }
}
