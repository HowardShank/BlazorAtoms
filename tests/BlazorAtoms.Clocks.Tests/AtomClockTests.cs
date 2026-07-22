using System;
using System.Text.RegularExpressions;

namespace BlazorAtoms.Clocks.Tests;

public class AtomClockTests : BunitContext
{
    // Live=false in most tests so no PeriodicTimer spins up during the assertion.

    [Fact]
    public void Renders_time_element_with_iso_datetime()
    {
        var cut = Render<AtomClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.Live, false));

        var t = cut.Find(".atom-clock-time");
        var iso = t.GetAttribute("datetime") ?? "";
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+\-]\d{2}:\d{2}$", iso);
    }

    [Fact]
    public void Utc_kind_has_zero_offset_and_data_kind()
    {
        var cut = Render<AtomClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.Live, false));

        Assert.Equal("utc", cut.Find(".atom-clock").GetAttribute("data-kind"));
        Assert.EndsWith("+00:00", cut.Find(".atom-clock-time").GetAttribute("datetime"));
    }

    [Fact]
    public void Explicit_timezone_overrides_kind_and_offset()
    {
        var plus5 = TimeZoneInfo.CreateCustomTimeZone("t+5", TimeSpan.FromHours(5), "t+5", "t+5");
        var cut = Render<AtomClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)   // overridden by TimeZone
            .Add(c => c.TimeZone, plus5)
            .Add(c => c.Live, false));

        Assert.Equal("custom", cut.Find(".atom-clock").GetAttribute("data-kind"));
        Assert.EndsWith("+05:00", cut.Find(".atom-clock-time").GetAttribute("datetime"));
    }

    [Fact]
    public void Format_and_culture_control_display_text()
    {
        var cut = Render<AtomClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.Format, "yyyy")
            .Add(c => c.Live, false));

        Assert.Equal(DateTime.UtcNow.Year.ToString(), cut.Find(".atom-clock-time").TextContent);
    }

    [Fact]
    public void Label_renders_when_set()
    {
        var cut = Render<AtomClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.Label, "Server")
            .Add(c => c.Live, false));

        Assert.Equal("Server", cut.Find(".atom-clock-label").TextContent);
    }

    [Fact]
    public void No_label_element_when_unset()
    {
        var cut = Render<AtomClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.Live, false));

        Assert.Empty(cut.FindAll(".atom-clock-label"));
    }

    [Fact]
    public void Size_and_color_tokens_emitted()
    {
        var cut = Render<AtomClock>(p => p
            .Add(c => c.Kind, ClockKind.Utc)
            .Add(c => c.Size, 24)
            .Add(c => c.Background, "#111")
            .Add(c => c.TextColor, "#0f0")
            .Add(c => c.Live, false));

        var style = cut.Find(".atom-clock").GetAttribute("style") ?? "";
        Assert.Contains("--clk-size:24px", style);
        Assert.Contains("--clk-bg:#111", style);
        Assert.Contains("--clk-color:#0f0", style);
    }

    [Fact]
    public void Switching_kind_to_browser_at_runtime_detects_zone()
    {
        // Regression: Kind=Server first, then flipped to Browser after the first interactive render
        // (a bound dropdown). Detection must fire on that switch, not only on the first render —
        // otherwise the zone stays null and ResolvedZone silently falls back to UTC.
        var module = JSInterop.SetupModule("./_content/BlazorAtoms.Clocks/atom-clocks.js");
        module.Setup<string?>("timezoneId").SetResult("");   // no IANA id → use the offset
        module.Setup<int>("timezoneOffset").SetResult(300);  // UTC+05:00

        var cut = Render<AtomClock>(p => p
            .Add(c => c.Kind, ClockKind.Server)
            .Add(c => c.Live, false));

        cut.Render(p => p.Add(c => c.Kind, ClockKind.Browser));

        Assert.Equal("browser", cut.Find(".atom-clock").GetAttribute("data-kind"));
        Assert.EndsWith("+05:00", cut.Find(".atom-clock-time").GetAttribute("datetime"));
    }

    [Fact]
    public void Browser_kind_falls_back_gracefully_when_js_yields_nothing()
    {
        // Loose interop = JS is "present" but the module returns nothing (like prerender/SSR where
        // detection hasn't resolved). The clock must still render — falling back to UTC.
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Render<AtomClock>(p => p
            .Add(c => c.Kind, ClockKind.Browser)
            .Add(c => c.Live, false));

        Assert.Equal("browser", cut.Find(".atom-clock").GetAttribute("data-kind"));
        Assert.EndsWith("+00:00", cut.Find(".atom-clock-time").GetAttribute("datetime"));
    }
}
