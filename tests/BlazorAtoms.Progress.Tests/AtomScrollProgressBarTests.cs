using Bunit;
using Xunit;

namespace BlazorAtoms.Progress.Tests;

/// <summary>bUnit coverage for <see cref="AtomScrollProgressBar"/>. The native (scroll-driven
/// animation) vs. JS-fallback split, and the track-geometry sync, happen in OnAfterRenderAsync
/// via a module import — not exercised here (would need JSInterop module setup); these
/// assertions cover markup/style wiring, which is identical regardless of which path ends up
/// active. Position/CssClass/Style/Color/Height apply to the outer track (sized to the real
/// scroll container by JS); the inner .atom-scroll-progress-bar is the 0%→100% fill.</summary>
public class AtomScrollProgressBarTests
{
    [Fact]
    public void Renders_track_and_fill_bar()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<AtomScrollProgressBar>();

        var track = cut.Find(".atom-scroll-progress-track");
        Assert.NotNull(track);
        var bar = cut.Find(".atom-scroll-progress-bar");
        Assert.NotNull(bar);
    }

    [Fact]
    public void Default_position_is_top()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<AtomScrollProgressBar>();

        var track = cut.Find(".atom-scroll-progress-track");
        Assert.Contains("atom-scroll-progress-track-top", track.ClassList);
    }

    [Fact]
    public void Position_Bottom_selects_bottom_class()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<AtomScrollProgressBar>(p => p.Add(x => x.Position, ScrollProgressPosition.Bottom));

        var track = cut.Find(".atom-scroll-progress-track");
        Assert.Contains("atom-scroll-progress-track-bottom", track.ClassList);
        Assert.DoesNotContain("atom-scroll-progress-track-top", track.ClassList);
    }

    [Fact]
    public void Color_and_Height_flow_into_track_style()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<AtomScrollProgressBar>(p => p
            .Add(x => x.Color, "#00ffee")
            .Add(x => x.Height, "6px"));

        var track = cut.Find(".atom-scroll-progress-track");
        var style = track.GetAttribute("style");
        Assert.Contains("--atom-scroll-progress-color:#00ffee;", style);
        Assert.Contains("--atom-scroll-progress-height:6px;", style);
    }

    [Fact]
    public void CssClass_and_Style_append_to_track()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<AtomScrollProgressBar>(p => p
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "opacity:.9;"));

        var track = cut.Find(".atom-scroll-progress-track");
        Assert.Contains("extra", track.ClassList);
        Assert.EndsWith("opacity:.9;", track.GetAttribute("style"));
    }
}
