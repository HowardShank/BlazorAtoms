using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Navigation.Tests;

/// <summary>
/// bUnit coverage for <see cref="AtomScrollTo"/>: default/custom rendering, tooltip + ARIA, styling
/// custom properties, self-positioning tokens, auto-hide (VisibleAfter), overlap avoidance
/// (HideNear), and the JS interop contract (scrollToTarget / watchVisibility / watchCollision arg
/// order, including the trailing ScrollContainer selector). JS is mocked via bUnit's module interop.
/// </summary>
public class AtomScrollToTests
{
    private const string ModulePath = "./_content/BlazorAtoms.Navigation/atom-navigation.js";

    // ---- rendering ----------------------------------------------------------------------------

    [Fact]
    public void Renders_button_with_default_up_arrow()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>();
        var btn = cut.Find("button.atom-scroll-to");
        Assert.Equal("button", btn.GetAttribute("type"));
        // Up chevron path.
        Assert.Contains("M6 15l6-6 6 6", cut.Markup);
    }

    [Fact]
    public void Down_direction_uses_down_arrow_and_aria()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>(p => p.Add(x => x.Direction, ScrollDirection.Down));
        Assert.Contains("M6 9l6 6 6-6", cut.Markup);
        Assert.Equal("Scroll to bottom", cut.Find("button").GetAttribute("aria-label"));
    }

    [Fact]
    public void Tooltip_sets_title_and_aria_label()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>(p => p.Add(x => x.Tooltip, "Back to top"));
        var btn = cut.Find("button");
        Assert.Equal("Back to top", btn.GetAttribute("title"));
        Assert.Equal("Back to top", btn.GetAttribute("aria-label"));
    }

    [Fact]
    public void ChildContent_overrides_default_arrow()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>(p => p
            .AddChildContent("<span class=\"rocket\">🚀</span>"));
        Assert.Contains("class=\"rocket\"", cut.Markup);
        Assert.DoesNotContain("atom-scroll-to-arrow", cut.Markup);
    }

    [Fact]
    public void Styling_params_emit_custom_properties()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.Color, "#111")
            .Add(x => x.Background, "#eee")
            .Add(x => x.Size, "60px")
            .Add(x => x.Radius, "12px"));
        var style = cut.Find("button").GetAttribute("style") ?? "";
        Assert.Contains("--scrollto-color:#111", style);
        Assert.Contains("--scrollto-bg:#eee", style);
        Assert.Contains("--scrollto-size:60px", style);
        Assert.Contains("--scrollto-radius:12px", style);
    }

    [Fact]
    public void ArrowStrokeWidth_applied_to_default_arrow()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>(p => p.Add(x => x.ArrowStrokeWidth, 3.5));
        Assert.Contains("stroke-width=\"3.5\"", cut.Markup);
    }

    // ---- positioning --------------------------------------------------------------------------

    [Fact]
    public void Default_position_inline_emits_no_data_position()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>();
        Assert.False(cut.Find("button").HasAttribute("data-position"));
    }

    [Fact]
    public void Fixed_bottom_right_emits_data_position()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.Position, ScrollPosition.FixedBottomRight));
        Assert.Equal("fixed-bottom-right", cut.Find("button").GetAttribute("data-position"));
    }

    [Fact]
    public void Absolute_top_left_emits_data_position()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.Position, ScrollPosition.AbsoluteTopLeft));
        Assert.Equal("absolute-top-left", cut.Find("button").GetAttribute("data-position"));
    }

    [Fact]
    public void Center_position_emits_center_token()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.Position, ScrollPosition.FixedBottomCenter));
        Assert.Equal("fixed-bottom-center", cut.Find("button").GetAttribute("data-position"));
    }

    [Fact]
    public void OffsetV_and_OffsetH_emit_custom_properties()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.Position, ScrollPosition.FixedBottomRight)
            .Add(x => x.OffsetV, "2rem")
            .Add(x => x.OffsetH, "0.75rem"));
        var style = cut.Find("button").GetAttribute("style") ?? "";
        Assert.Contains("--scrollto-offset-v:2rem", style);
        Assert.Contains("--scrollto-offset-h:0.75rem", style);
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.CssClass, "my-extra")
            .Add(x => x.Style, "opacity:.5"));
        var btn = cut.Find("button");
        Assert.Contains("atom-scroll-to", btn.GetAttribute("class"));
        Assert.Contains("my-extra", btn.GetAttribute("class"));
        Assert.Contains("opacity:.5", btn.GetAttribute("style"));
    }

    [Fact]
    public void Multiple_instances_are_independent()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var up = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.Direction, ScrollDirection.Up)
            .Add(x => x.Position, ScrollPosition.FixedTopRight));
        var down = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.Direction, ScrollDirection.Down)
            .Add(x => x.Position, ScrollPosition.FixedBottomRight));
        Assert.Equal("fixed-top-right", up.Find("button").GetAttribute("data-position"));
        Assert.Equal("fixed-bottom-right", down.Find("button").GetAttribute("data-position"));
        Assert.Contains("M6 15l6-6 6 6", up.Markup);   // up chevron
        Assert.Contains("M6 9l6 6 6-6", down.Markup);   // down chevron
    }

    // ---- auto-hide ----------------------------------------------------------------------------

    [Fact]
    public void VisibleAfter_null_renders_visible_true()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomScrollTo>();
        Assert.Equal("true", cut.Find("button").GetAttribute("data-visible"));
    }

    [Fact]
    public void VisibleAfter_set_starts_hidden_and_wires_watcher()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        var cut = ctx.Render<AtomScrollTo>(p => p.Add(x => x.VisibleAfter, 300));
        Assert.Equal("false", cut.Find("button").GetAttribute("data-visible"));
        var inv = module.VerifyInvoke("watchVisibility");
        // args: [0]=id, [1]=el, [2]=dotNetRef, [3]=threshold, [4]=scope, [5]=container
        Assert.Equal(300, inv.Arguments[3]);
    }

    [Fact]
    public void VisibleAfter_without_callback_passes_null_dotNet_ref()
    {
        // No OnVisibilityChanged delegate → no DotNetObjectReference is marshaled, so JS can never
        // call back into a reference that could go stale. arg[2] is the dotNet ref slot.
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        ctx.Render<AtomScrollTo>(p => p.Add(x => x.VisibleAfter, 300));
        var inv = module.VerifyInvoke("watchVisibility");
        Assert.Null(inv.Arguments[2]);
    }

    [Fact]
    public void VisibleAfter_with_callback_passes_dotNet_ref()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.VisibleAfter, 300)
            .Add(x => x.OnVisibilityChanged, EventCallback.Factory.Create<bool>(this, _ => { })));
        var inv = module.VerifyInvoke("watchVisibility");
        Assert.NotNull(inv.Arguments[2]);
    }

    // ---- interop: scroll ----------------------------------------------------------------------

    [Fact]
    public void Click_invokes_scrollToTarget_top_for_up_page()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        var cut = ctx.Render<AtomScrollTo>();
        cut.Find("button").Click();
        var inv = module.VerifyInvoke("scrollToTarget");
        Assert.Equal("top", inv.Arguments[1]);
        Assert.Equal("page", inv.Arguments[3]);
        Assert.Equal("smooth", inv.Arguments[4]);
    }

    [Fact]
    public void Click_with_target_uses_selector_mode()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        var cut = ctx.Render<AtomScrollTo>(p => p.Add(x => x.Target, "#section-3"));
        cut.Find("button").Click();
        var inv = module.VerifyInvoke("scrollToTarget");
        Assert.Equal("selector", inv.Arguments[1]);
        Assert.Equal("#section-3", inv.Arguments[2]);
    }

    [Fact]
    public void Container_scope_and_auto_motion_flow_to_JS()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        var cut = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.Direction, ScrollDirection.Down)
            .Add(x => x.Scope, ScrollScope.Container)
            .Add(x => x.Motion, ScrollMotion.Auto));
        cut.Find("button").Click();
        var inv = module.VerifyInvoke("scrollToTarget");
        Assert.Equal("bottom", inv.Arguments[1]);
        Assert.Equal("container", inv.Arguments[3]);
        Assert.Equal("auto", inv.Arguments[4]);
    }

    [Fact]
    public void ScrollContainer_flows_to_scrollToTarget_as_trailing_arg()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        var cut = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.Scope, ScrollScope.Container)
            .Add(x => x.ScrollContainer, "#log-panel"));
        cut.Find("button").Click();
        var inv = module.VerifyInvoke("scrollToTarget");
        Assert.Equal("#log-panel", inv.Arguments[5]);
    }

    [Fact]
    public void ScrollContainer_flows_to_watchVisibility_as_trailing_arg()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        var cut = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.VisibleAfter, 100)
            .Add(x => x.ScrollContainer, "#log-panel"));
        var inv = module.VerifyInvoke("watchVisibility");
        Assert.Equal("#log-panel", inv.Arguments[5]);
    }

    // ---- interop: collision (HideNear) --------------------------------------------------------

    [Fact]
    public void HideNear_wires_watchCollision_with_selector()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        var cut = ctx.Render<AtomScrollTo>(p => p.Add(x => x.HideNear, "footer"));
        var inv = module.VerifyInvoke("watchCollision");
        // args: [0]=id, [1]=el, [2]=hideNearSelector, [3]=scope, [4]=container
        Assert.Equal("footer", inv.Arguments[2]);
    }

    [Fact]
    public void No_HideNear_never_calls_watchCollision()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        ctx.Render<AtomScrollTo>(p => p.Add(x => x.VisibleAfter, 100));
        module.VerifyNotInvoke("watchCollision");
    }

    [Fact]
    public void HideNear_and_VisibleAfter_wire_both_watchers()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.VisibleAfter, 200)
            .Add(x => x.HideNear, "#end"));
        module.VerifyInvoke("watchVisibility");
        module.VerifyInvoke("watchCollision");
    }

    // ---- exceptions + callbacks ---------------------------------------------------------------

    [Fact]
    public void Click_swallows_JSException()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("scrollToTarget", _ => true).SetException(new JSException("boom"));
        var cut = ctx.Render<AtomScrollTo>();
        var ex = Record.Exception(() => cut.Find("button").Click());
        Assert.Null(ex); // swallowed
    }

    [Fact]
    public void OnScrolled_fires_after_successful_scroll()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.SetupModule(ModulePath);
        var fired = false;
        var cut = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.OnScrolled, EventCallback.Factory.Create(this, () => fired = true)));
        cut.Find("button").Click();
        Assert.True(fired);
    }

    [Fact]
    public async Task OnVisibilityChangedInternal_forwards_to_callback()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.SetupModule(ModulePath);
        bool? seen = null;
        var cut = ctx.Render<AtomScrollTo>(p => p
            .Add(x => x.VisibleAfter, 100)
            .Add(x => x.OnVisibilityChanged, EventCallback.Factory.Create<bool>(this, v => seen = v)));
        await cut.InvokeAsync(() => cut.Instance.OnVisibilityChangedInternal(true));
        Assert.True(seen);
    }
}
