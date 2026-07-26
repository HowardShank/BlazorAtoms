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
    private const string ModulePath = "./_content/BlazorAtoms.Progress/atom-progress.js";

    [Fact]
    public void Default_Width_and_Align_flow_into_the_attach_call_as_null_and_start()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("attachScrollProgress", _ => true).SetVoidResult();

        ctx.Render<AtomScrollProgressBar>();

        var call = module.VerifyInvoke("attachScrollProgress");
        // track ref, bar ref, position, width, align
        Assert.Equal("top", call.Arguments[2]);
        Assert.Null(call.Arguments[3]);
        Assert.Equal("start", call.Arguments[4]);
    }

    [Fact]
    public void Width_and_Align_flow_into_the_attach_call()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("attachScrollProgress", _ => true).SetVoidResult();

        ctx.Render<AtomScrollProgressBar>(p => p
            .Add(x => x.Width, "60%")
            .Add(x => x.Align, ScrollProgressAlign.Center));

        var call = module.VerifyInvoke("attachScrollProgress");
        Assert.Equal("60%", call.Arguments[3]);
        Assert.Equal("center", call.Arguments[4]);
    }

    [Fact]
    public void Changing_Width_after_first_render_calls_updateLayout_not_a_second_attach()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("attachScrollProgress", _ => true).SetVoidResult();
        module.SetupVoid("updateLayout", _ => true).SetVoidResult();

        var cut = ctx.Render<AtomScrollProgressBar>(p => p.Add(x => x.Width, "50%"));
        Assert.Single(module.Invocations, i => i.Identifier == "attachScrollProgress");

        cut.Render(p => p.Add(x => x.Width, "80%").Add(x => x.Align, ScrollProgressAlign.End));

        Assert.Single(module.Invocations, i => i.Identifier == "attachScrollProgress");
        var call = module.VerifyInvoke("updateLayout");
        // track ref, position, width, align
        Assert.Equal("80%", call.Arguments[2]);
        Assert.Equal("end", call.Arguments[3]);
    }

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
