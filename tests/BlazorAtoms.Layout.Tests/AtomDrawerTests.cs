namespace BlazorAtoms.Layout.Tests;

/// <summary>
/// bUnit coverage for <see cref="AtomDrawer"/>: mount/visibility, the enter-animation class flip,
/// position/transition classes, style-var emission + length normalization, the close paths
/// (button / backdrop / external Open=false), and transition-gated OnOpen/OnClose events.
/// </summary>
public class AtomDrawerTests
{
    [Fact]
    public void Closed_by_default_renders_nothing()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var cut = ctx.Render<AtomDrawer>();
        Assert.Empty(cut.FindAll("aside"));
        Assert.Empty(cut.FindAll(".atom-drawer-backdrop"));
    }

    [Fact]
    public void Open_renders_dialog_aside()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true));
        var aside = cut.Find("aside");
        Assert.Equal("dialog", aside.GetAttribute("role"));
        Assert.Equal("true", aside.GetAttribute("aria-modal"));
    }

    [Fact]
    public void Open_gets_the_open_class_after_render()
    {
        // The panel mounts hidden, then OnAfterRender adds atom-drawer-open so the CSS transition
        // has a start state to animate from. bUnit runs OnAfterRender, so the class is present.
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true));
        Assert.Contains("atom-drawer-open", cut.Find("aside").ClassList);
    }

    [Theory]
    [InlineData(AtomDrawerPosition.Left, "atom-drawer-left")]
    [InlineData(AtomDrawerPosition.Right, "atom-drawer-right")]
    [InlineData(AtomDrawerPosition.Top, "atom-drawer-top")]
    [InlineData(AtomDrawerPosition.Bottom, "atom-drawer-bottom")]
    public void Position_emits_class(AtomDrawerPosition pos, string cls)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true).Add(x => x.Position, pos));
        Assert.Contains(cls, cut.Find("aside").ClassList);
    }

    [Theory]
    [InlineData(AtomDrawerTransition.Slide, "atom-drawer-slide")]
    [InlineData(AtomDrawerTransition.Fade, "atom-drawer-fade")]
    [InlineData(AtomDrawerTransition.Grow, "atom-drawer-grow")]
    public void Transition_emits_class(AtomDrawerTransition t, string cls)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true).Add(x => x.Transition, t));
        Assert.Contains(cls, cut.Find("aside").ClassList);
    }

    [Fact]
    public void Bare_numeric_width_is_normalized_to_px()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true).Add(x => x.Width, "300"));
        Assert.Contains("--atom-drawer-width:300px", cut.Find("aside").GetAttribute("style"));
    }

    [Fact]
    public void Css_length_width_is_passed_through()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true).Add(x => x.Width, "50%"));
        Assert.Contains("--atom-drawer-width:50%", cut.Find("aside").GetAttribute("style"));
    }

    [Fact]
    public void Vertical_position_defaults_full_width_and_fixed_height()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true).Add(x => x.Position, AtomDrawerPosition.Top));
        var style = cut.Find("aside").GetAttribute("style");
        Assert.Contains("--atom-drawer-width:100vw", style);
        Assert.Contains("--atom-drawer-height:240px", style);
    }

    [Fact]
    public void Viewport_anchor_is_the_default_no_anchor_class()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true));
        Assert.DoesNotContain("atom-drawer-anchor-container", cut.Find("aside").ClassList);
    }

    [Fact]
    public void Container_anchor_adds_class_to_panel_and_backdrop()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Anchor, AtomDrawerAnchor.Container));

        Assert.Contains("atom-drawer-anchor-container", cut.Find("aside").ClassList);
        Assert.Contains("atom-drawer-anchor-container", cut.Find(".atom-drawer-backdrop").ClassList);
    }

    [Fact]
    public void Container_anchor_uses_percent_instead_of_viewport_units()
    {
        // Vertical position (Top) spans the cross axis (width) 100vw under Viewport anchor, but
        // vw/vh would measure the whole page — not the containing ancestor — so Container anchor
        // must use 100% instead.
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Position, AtomDrawerPosition.Top)
            .Add(x => x.Anchor, AtomDrawerAnchor.Container));

        var style = cut.Find("aside").GetAttribute("style");
        Assert.Contains("--atom-drawer-width:100%", style);
        Assert.DoesNotContain("100vw", style);
    }

    [Fact]
    public void Explicit_width_overrides_container_anchor_percent_default()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Anchor, AtomDrawerAnchor.Container)
            .Add(x => x.Width, "320px"));

        var style = cut.Find("aside").GetAttribute("style");
        Assert.Contains("--atom-drawer-width:320px", style);
    }

    [Fact]
    public void ShowCloseButton_false_hides_button()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true).Add(x => x.ShowCloseButton, false));
        Assert.Empty(cut.FindAll(".atom-drawer-close"));
    }

    [Fact]
    public void ShowBackdrop_false_hides_backdrop()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true).Add(x => x.ShowBackdrop, false));
        Assert.Empty(cut.FindAll(".atom-drawer-backdrop"));
    }

    [Fact]
    public void Close_button_notifies_binding_and_fires_OnClose()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        bool? changedTo = null;
        var closeCount = 0;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.OpenChanged, (bool v) => changedTo = v)
            .Add(x => x.OnClose, () => closeCount++));

        cut.Find(".atom-drawer-close").Click();

        Assert.Equal(false, changedTo);
        Assert.Equal(1, closeCount);
    }

    [Fact]
    public void Backdrop_click_closes_when_enabled()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        bool? changedTo = null;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.OpenChanged, (bool v) => changedTo = v));

        cut.Find(".atom-drawer-backdrop").Click();

        Assert.Equal(false, changedTo);
    }

    [Fact]
    public void Backdrop_click_does_not_close_when_disabled()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var changedCount = 0;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.CloseOnBackdropClick, false)
            .Add(x => x.OpenChanged, (bool _) => changedCount++));

        cut.Find(".atom-drawer-backdrop").Click();

        Assert.Equal(0, changedCount);
        Assert.Contains("atom-drawer-open", cut.Find("aside").ClassList);
    }

    [Fact]
    public void OnOpen_fires_once_on_open_not_on_unrelated_rerender()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var openCount = 0;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.OnOpen, () => openCount++));
        Assert.Equal(1, openCount);

        // An unrelated parameter change while already open must NOT re-fire OnOpen.
        cut.Render(p => p.Add(x => x.Width, "400"));
        Assert.Equal(1, openCount);
    }

    [Fact]
    public void OnClose_fires_when_parent_sets_open_false()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var closeCount = 0;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.OnClose, () => closeCount++));

        cut.Render(p => p.Add(x => x.Open, false));

        Assert.Equal(1, closeCount);
    }

    [Fact]
    public void Modal_defaults_are_on()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>();
        Assert.True(cut.Instance.CloseOnEscape);
        Assert.True(cut.Instance.TrapFocus);
        Assert.True(cut.Instance.LockScroll);
    }

    [Fact]
    public void Opening_activates_modal_behaviour_via_js()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true));
        Assert.Contains(ctx.JSInterop.Invocations, i => i.Identifier == "activate");
    }

    [Fact]
    public void All_modal_options_off_skips_js_activation()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.CloseOnEscape, false)
            .Add(x => x.TrapFocus, false)
            .Add(x => x.LockScroll, false));
        Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "activate");
    }

    [Fact]
    public async Task CloseFromJsAsync_closes_and_notifies_binding()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        bool? changedTo = null;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.OpenChanged, (bool v) => changedTo = v));

        await cut.InvokeAsync(() => cut.Instance.CloseFromJsAsync());

        Assert.Equal(false, changedTo);
    }

    [Fact]
    public void No_shadow_by_default()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p.Add(x => x.Open, true));
        Assert.DoesNotContain("--atom-drawer-shadow", cut.Find("aside").GetAttribute("style"));
    }

    [Fact]
    public void ShowShadow_emits_composed_box_shadow_value()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.ShowShadow, true)
            .Add(x => x.ShadowOffsetX, 2)
            .Add(x => x.ShadowOffsetY, 4)
            .Add(x => x.ShadowBlur, 12)
            .Add(x => x.ShadowSpread, 1)
            .Add(x => x.ShadowColor, "rgba(0,0,0,0.4)"));

        var style = cut.Find("aside").GetAttribute("style");
        Assert.Contains("--atom-drawer-shadow:2px 4px 12px 1px rgba(0,0,0,0.4)", style);
    }

    [Theory]
    [InlineData(AtomDrawerPosition.Left, "16px 0px")]   // biases right (positive X) — that's the visible edge
    [InlineData(AtomDrawerPosition.Right, "-16px 0px")] // biases left (negative X)
    [InlineData(AtomDrawerPosition.Top, "0px 16px")]    // biases down (positive Y)
    [InlineData(AtomDrawerPosition.Bottom, "0px -16px")] // biases up (negative Y)
    public void Unset_shadow_offset_auto_biases_away_from_the_pinned_edge(AtomDrawerPosition pos, string expectedOffsets)
    {
        // Default ShadowBlur is 16 and no offset is supplied — the component should bias the shadow
        // entirely toward the one edge of the panel that isn't flush against the viewport, since a
        // symmetric (0,0) offset would waste half the shadow's reach off-screen.
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Position, pos)
            .Add(x => x.ShowShadow, true));

        var style = cut.Find("aside").GetAttribute("style");
        Assert.Contains($"--atom-drawer-shadow:{expectedOffsets}", style);
    }

    [Fact]
    public void Explicit_shadow_offset_overrides_the_auto_bias()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Position, AtomDrawerPosition.Right) // would auto-bias to -16px
            .Add(x => x.ShowShadow, true)
            .Add(x => x.ShadowOffsetX, 0.0) // explicit zero must win, not fall back to the bias
            .Add(x => x.ShadowOffsetY, 0.0));

        var style = cut.Find("aside").GetAttribute("style");
        Assert.Contains("--atom-drawer-shadow:0px 0px", style);
    }

    [Fact]
    public void Grow_transition_emits_shadow_reach_vars_for_clip_path()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Transition, AtomDrawerTransition.Grow)
            .Add(x => x.ShowShadow, true)
            .Add(x => x.ShadowBlur, 10)
            .Add(x => x.ShadowSpread, 2));

        // top/left reach = blur+spread-offset; bottom/right = blur+spread+offset. With no explicit
        // offset (Position defaults to Left, biasing +16 in X), reach differs left vs right.
        var style = cut.Find("aside").GetAttribute("style");
        Assert.Contains("--atom-drawer-shadow-top:12px", style);
        Assert.Contains("--atom-drawer-shadow-bottom:12px", style);
    }

    [Fact]
    public void No_shadow_reach_vars_when_shadow_off()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Transition, AtomDrawerTransition.Grow));

        var style = cut.Find("aside").GetAttribute("style");
        Assert.DoesNotContain("--atom-drawer-shadow-top", style);
    }

    [Fact]
    public void Renders_header_body_footer_slots()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose; // drawer imports atom-layout.js for modal behaviour
        var cut = ctx.Render<AtomDrawer>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.HeaderContent, b => b.AddMarkupContent(0, "<span class=\"h\">H</span>"))
            .Add(x => x.ChildContent, b => b.AddMarkupContent(0, "<span class=\"b\">B</span>"))
            .Add(x => x.FooterContent, b => b.AddMarkupContent(0, "<span class=\"f\">F</span>")));

        Assert.NotNull(cut.Find(".atom-drawer-header .h"));
        Assert.NotNull(cut.Find(".atom-drawer-body .b"));
        Assert.NotNull(cut.Find(".atom-drawer-footer .f"));
    }
}
