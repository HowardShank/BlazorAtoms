using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorAtoms.Progress.Tests;

/// <summary>bUnit coverage for <see cref="AtomScrollProgressBar"/>. The native (scroll-driven
/// animation) vs. JS-fallback split, the track-geometry sync, and the ResizeObserver-driven
/// container re-resolution all happen inside atom-progress.js — not exercised here (bUnit can't
/// execute the module); these assertions cover the C#/markup contract, which is identical
/// regardless of which path ends up active. Position/CssClass/Style/Color/Height apply to the
/// outer track (sized to the real scroll container by JS); the inner .atom-scroll-progress-bar is
/// the 0%→100% fill.
///
/// attachScrollProgress returns bool (true = measured), which is what clears the -pending class,
/// so tests asserting on the revealed track must Setup it with a result rather than SetupVoid.</summary>
public class AtomScrollProgressBarTests
{
    private const string ModulePath = "./_content/BlazorAtoms.Progress/atom-progress.js";

    private static BunitJSModuleInterop SetupAttachableModule(BunitContext ctx, bool measured = true)
    {
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("attachScrollProgress", _ => true).SetResult(measured);
        return module;
    }

    [Fact]
    public void Default_Width_and_Align_flow_into_the_attach_call_as_null_and_start()
    {
        using var ctx = new BunitContext();
        var module = SetupAttachableModule(ctx);

        ctx.Render<AtomScrollProgressBar>();

        var call = module.VerifyInvoke("attachScrollProgress");
        // track ref, bar ref, position, width, align, scrollContainer — pinned so a future
        // parameter added on one side of the C#/JS boundary can't silently shift the others.
        Assert.Equal(6, call.Arguments.Count);
        Assert.Equal("top", call.Arguments[2]);
        Assert.Null(call.Arguments[3]);
        Assert.Equal("start", call.Arguments[4]);
    }

    [Fact]
    public void Width_and_Align_flow_into_the_attach_call()
    {
        using var ctx = new BunitContext();
        var module = SetupAttachableModule(ctx);

        ctx.Render<AtomScrollProgressBar>(p => p
            .Add(x => x.Width, "60%")
            .Add(x => x.Align, ScrollProgressAlign.Center));

        var call = module.VerifyInvoke("attachScrollProgress");
        Assert.Equal("60%", call.Arguments[3]);
        Assert.Equal("center", call.Arguments[4]);
    }

    [Fact]
    public void ScrollContainer_defaults_to_null_in_the_attach_call()
    {
        using var ctx = new BunitContext();
        var module = SetupAttachableModule(ctx);

        ctx.Render<AtomScrollProgressBar>();

        var call = module.VerifyInvoke("attachScrollProgress");
        Assert.Null(call.Arguments[5]);
    }

    [Fact]
    public void ScrollContainer_flows_into_the_attach_call()
    {
        using var ctx = new BunitContext();
        var module = SetupAttachableModule(ctx);

        ctx.Render<AtomScrollProgressBar>(p => p.Add(x => x.ScrollContainer, "#content"));

        var call = module.VerifyInvoke("attachScrollProgress");
        Assert.Equal("#content", call.Arguments[5]);
    }

    [Fact]
    public void Changing_Width_after_first_render_calls_updateLayout_not_a_second_attach()
    {
        using var ctx = new BunitContext();
        var module = SetupAttachableModule(ctx);
        module.Setup<bool>("updateLayout", _ => true).SetResult(true);

        var cut = ctx.Render<AtomScrollProgressBar>(p => p.Add(x => x.Width, "50%"));
        Assert.Single(module.Invocations, i => i.Identifier == "attachScrollProgress");

        cut.Render(p => p.Add(x => x.Width, "80%").Add(x => x.Align, ScrollProgressAlign.End));

        Assert.Single(module.Invocations, i => i.Identifier == "attachScrollProgress");
        var call = module.VerifyInvoke("updateLayout");
        // track ref, position, width, align, scrollContainer
        Assert.Equal("80%", call.Arguments[2]);
        Assert.Equal("end", call.Arguments[3]);
    }

    [Fact]
    public void Changing_ScrollContainer_after_first_render_calls_updateLayout_not_a_second_attach()
    {
        using var ctx = new BunitContext();
        var module = SetupAttachableModule(ctx);
        module.Setup<bool>("updateLayout", _ => true).SetResult(true);

        var cut = ctx.Render<AtomScrollProgressBar>(p => p.Add(x => x.ScrollContainer, "#first"));
        Assert.Single(module.Invocations, i => i.Identifier == "attachScrollProgress");

        cut.Render(p => p.Add(x => x.ScrollContainer, "#second"));

        Assert.Single(module.Invocations, i => i.Identifier == "attachScrollProgress");
        var call = module.VerifyInvoke("updateLayout");
        Assert.Equal("#second", call.Arguments[4]);
    }

    [Fact]
    public void Track_is_pending_until_attach_reports_a_measure()
    {
        using var ctx = new BunitContext();
        // measured:false stands in for "the module never got far enough to measure" — prerender,
        // a failed import, JS disabled. The track must stay hidden rather than paint full-width.
        SetupAttachableModule(ctx, measured: false);

        var cut = ctx.Render<AtomScrollProgressBar>();

        var track = cut.Find(".atom-scroll-progress-track");
        Assert.Contains("atom-scroll-progress-track-pending", track.ClassList);
    }

    [Fact]
    public void Track_drops_pending_once_attach_reports_a_measure()
    {
        using var ctx = new BunitContext();
        SetupAttachableModule(ctx, measured: true);

        var cut = ctx.Render<AtomScrollProgressBar>();

        var track = cut.Find(".atom-scroll-progress-track");
        Assert.DoesNotContain("atom-scroll-progress-track-pending", track.ClassList);
    }

    [Fact]
    public async Task Disposing_detaches_the_JS_listeners()
    {
        using var ctx = new BunitContext();
        var module = SetupAttachableModule(ctx);
        module.SetupVoid("detachScrollProgress", _ => true).SetVoidResult();

        var cut = ctx.Render<AtomScrollProgressBar>();
        await cut.Instance.DisposeAsync();

        var call = module.VerifyInvoke("detachScrollProgress");
        // The track element is how the module finds everything it registered for this instance —
        // listeners, observer, and its claim on the container's shared scroll-timeline.
        Assert.Single(call.Arguments);
        Assert.IsType<ElementReference>(call.Arguments[0]!);
    }

    [Fact]
    public void Two_instances_attach_independently_with_their_own_containers()
    {
        using var ctx = new BunitContext();
        var module = SetupAttachableModule(ctx);

        ctx.Render<AtomScrollProgressBar>(p => p.Add(x => x.ScrollContainer, "#first"));
        ctx.Render<AtomScrollProgressBar>(p => p.Add(x => x.ScrollContainer, "#second"));

        // Two bars on one page must each resolve their own container. The JS-side half of this —
        // one reference-counted scroll-timeline per container, so instances don't overwrite each
        // other's timeline name — can't be reached from bUnit; this pins the C# half.
        var attaches = module.Invocations
            .Where(i => i.Identifier == "attachScrollProgress")
            .ToList();
        Assert.Equal(2, attaches.Count);
        Assert.Equal("#first", attaches[0].Arguments[5]);
        Assert.Equal("#second", attaches[1].Arguments[5]);
    }

    [Fact]
    public void Re_rendering_with_unchanged_parameters_does_not_call_updateLayout()
    {
        using var ctx = new BunitContext();
        var module = SetupAttachableModule(ctx);
        module.Setup<bool>("updateLayout", _ => true).SetResult(true);

        var cut = ctx.Render<AtomScrollProgressBar>(p => p.Add(x => x.Width, "50%"));
        cut.Render(p => p.Add(x => x.Width, "50%"));

        // Guards the _lastLayout comparison, which gained a fourth member (ScrollContainer) — and
        // note revealing the track is itself a StateHasChanged, so OnAfterRenderAsync runs again
        // with nothing changed. That must not round-trip to JS.
        Assert.DoesNotContain(module.Invocations, i => i.Identifier == "updateLayout");
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
