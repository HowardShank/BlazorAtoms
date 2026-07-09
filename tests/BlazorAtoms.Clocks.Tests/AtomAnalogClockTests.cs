using System;
using System.Linq;

namespace BlazorAtoms.Clocks.Tests;

public class AtomAnalogClockTests : TestContext
{
    // Live=false so no PeriodicTimer spins up during the assertion.

    [Fact]
    public void Renders_svg_face_with_dial_and_cap()
    {
        var cut = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.Live, false));

        Assert.Single(cut.FindAll("svg.atom-analog-clock-face"));
        Assert.Single(cut.FindAll("circle.aclk-dial"));
        Assert.Single(cut.FindAll("circle.aclk-cap"));
        Assert.Equal("utc", cut.Find(".atom-analog-clock").GetAttribute("data-kind"));
    }

    [Fact]
    public void Twelve_hour_ticks_always_present()
    {
        var cut = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.ShowMinuteTicks, false)
            .Add(c => c.Live, false));

        Assert.Equal(12, cut.FindAll("line.aclk-tick-hour").Count);
        Assert.Empty(cut.FindAll("line.aclk-tick-min"));
    }

    [Fact]
    public void Minute_ticks_add_up_to_sixty()
    {
        var cut = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.ShowMinuteTicks, true)
            .Add(c => c.Live, false));

        // 60 marks total: 12 hour + 48 minute.
        Assert.Equal(12, cut.FindAll("line.aclk-tick-hour").Count);
        Assert.Equal(48, cut.FindAll("line.aclk-tick-min").Count);
    }

    [Fact]
    public void Second_hand_toggles_with_ShowSeconds()
    {
        var on = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc).Add(c => c.Live, false));
        Assert.Single(on.FindAll("line.aclk-hand-sec"));

        var off = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc).Add(c => c.ShowSeconds, false).Add(c => c.Live, false));
        Assert.Empty(off.FindAll("line.aclk-hand-sec"));
    }

    [Fact]
    public void Hands_rotate_to_the_current_time()
    {
        // Custom +00 zone so the rendered instant equals UtcNow; assert hand angles match.
        var now = DateTimeOffset.UtcNow;
        var cut = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.Live, false));

        var sec = cut.Find("line.aclk-hand-sec").GetAttribute("transform") ?? "";
        var min = cut.Find("line.aclk-hand-min").GetAttribute("transform") ?? "";

        // Second-hand angle = second * 6. Allow the render to straddle a 1s boundary.
        var expected = new[] { now.Second, (now.Second + 1) % 60 }.Select(s => $"rotate({s * 6} ");
        Assert.Contains(expected, e => sec.StartsWith(e));
        Assert.StartsWith("rotate(", min);
    }

    [Fact]
    public void Numerals_render_only_when_enabled()
    {
        var without = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc).Add(c => c.Live, false));
        Assert.Empty(without.FindAll("svg text"));

        var with = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc).Add(c => c.ShowNumerals, true).Add(c => c.Live, false));
        var numerals = with.FindAll("svg text");
        Assert.Equal(12, numerals.Count);
        Assert.Equal("12", numerals[11].TextContent);
    }

    [Fact]
    public void Size_and_color_tokens_emitted()
    {
        var cut = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.Size, 200)
            .Add(c => c.FaceColor, "#111")
            .Add(c => c.HandColor, "#0f0")
            .Add(c => c.AccentColor, "#f00")
            .Add(c => c.Live, false));

        var style = cut.Find(".atom-analog-clock").GetAttribute("style") ?? "";
        Assert.Contains("--aclk-size:200px", style);
        Assert.Contains("--aclk-face:#111", style);
        Assert.Contains("--aclk-hand:#0f0", style);
        Assert.Contains("--aclk-accent:#f00", style);
    }

    [Fact]
    public void Label_renders_as_figcaption()
    {
        var cut = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.Label, "UTC")
            .Add(c => c.Live, false));

        Assert.Equal("UTC", cut.Find("figcaption.atom-analog-clock-label").TextContent);
    }

    [Fact]
    public void Explicit_timezone_overrides_kind()
    {
        var plus5 = TimeZoneInfo.CreateCustomTimeZone("t+5", TimeSpan.FromHours(5), "t+5", "t+5");
        var cut = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.TimeZone, plus5)
            .Add(c => c.Live, false));

        Assert.Equal("custom", cut.Find(".atom-analog-clock").GetAttribute("data-kind"));
    }

    [Fact]
    public void Browser_kind_falls_back_gracefully_when_js_yields_nothing()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = RenderComponent<AtomAnalogClock>(p => p
            .Add(c => c.Kind, ClockKind.Browser)
            .Add(c => c.Live, false));

        Assert.Equal("browser", cut.Find(".atom-analog-clock").GetAttribute("data-kind"));
        Assert.Single(cut.FindAll("circle.aclk-dial"));
    }
}
