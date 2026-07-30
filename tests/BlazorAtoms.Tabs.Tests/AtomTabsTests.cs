using static BlazorAtoms.Tabs.Tests.TabsHarness;

namespace BlazorAtoms.Tabs.Tests;

/// <summary>bUnit coverage for <see cref="AtomTabs"/> — selection, the ARIA wiring, panel-rendering
/// strategies and the styling axes. Keyboard navigation lives in
/// <see cref="AtomTabsKeyboardTests"/>.</summary>
public class AtomTabsTests
{
    private static IRenderedComponent<AtomTabs> RenderTabs(BunitContext ctx,
        Action<ComponentParameterCollectionBuilder<AtomTabs>>? extra = null)
    {
        // Every AtomTabs imports the key-guard module on first render; plan it so strict mode is happy.
        PlanKeyGuardModule(ctx);

        return ctx.Render<AtomTabs>(p =>
        {
            p.Add(x => x.TabList, DefaultTabList);
            p.Add(x => x.Panels, DefaultPanels);
            extra?.Invoke(p);
        });
    }

    [Fact]
    public void Renders_a_tablist_with_one_tab_per_child()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx);

        var list = cut.Find(".atom-tabs-list");
        Assert.Equal("tablist", list.GetAttribute("role"));
        Assert.Equal(3, cut.FindAll("[role='tab']").Count);
        Assert.Equal("Alpha", cut.FindAll(".atom-tab")[0].TextContent.Trim());
    }

    [Fact]
    public void Value_selects_the_matching_tab_and_panel()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p.Add(x => x.Value, "b"));

        var tabs = cut.FindAll("[role='tab']");
        Assert.Equal("false", tabs[0].GetAttribute("aria-selected"));
        Assert.Equal("true", tabs[1].GetAttribute("aria-selected"));
        Assert.Contains("panel b", cut.Find("[role='tabpanel']").TextContent);
    }

    [Fact]
    public void Unset_value_falls_back_to_the_first_tab_without_writing_it_back()
    {
        using var ctx = new BunitContext();
        var raised = 0;

        // Derived, not assigned: the caller's bound field must not be mutated during initialization,
        // before any interaction has happened.
        var cut = RenderTabs(ctx, p => p
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, _ => raised++)));

        Assert.Equal("true", cut.FindAll("[role='tab']")[0].GetAttribute("aria-selected"));
        Assert.Equal(0, raised);
    }

    [Fact]
    public void Unknown_value_also_falls_back_to_the_first_tab()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p.Add(x => x.Value, "does-not-exist"));

        Assert.Equal("true", cut.FindAll("[role='tab']")[0].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Fallback_skips_a_disabled_first_tab()
    {
        using var ctx = new BunitContext();

        PlanKeyGuardModule(ctx);

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, TabList(
                new Tab("a", "Alpha", Disabled: true), new Tab("b", "Bravo")))
            .Add(x => x.Panels, Panels(("a", "<p>panel a</p>"), ("b", "<p>panel b</p>"))));

        var tabs = cut.FindAll("[role='tab']");
        Assert.Equal("false", tabs[0].GetAttribute("aria-selected"));
        Assert.Equal("true", tabs[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Clicking_a_tab_raises_ValueChanged_with_its_value()
    {
        using var ctx = new BunitContext();
        var selected = new List<string>();

        var cut = RenderTabs(ctx, p => p
            .Add(x => x.Value, "a")
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, v => selected.Add(v))));

        cut.FindAll("[role='tab']")[2].Click();

        Assert.Equal(["c"], selected);
    }

    [Fact]
    public void Clicking_the_already_selected_tab_raises_nothing()
    {
        using var ctx = new BunitContext();
        var raised = 0;

        var cut = RenderTabs(ctx, p => p
            .Add(x => x.Value, "b")
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, _ => raised++)));

        cut.FindAll("[role='tab']")[1].Click();

        Assert.Equal(0, raised);
    }

    [Fact]
    public void A_disabled_tab_cannot_be_selected()
    {
        using var ctx = new BunitContext();
        var raised = 0;

        PlanKeyGuardModule(ctx);

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.Value, "a")
            .Add(x => x.TabList, TabList(new Tab("a", "Alpha"), new Tab("b", "Bravo", Disabled: true)))
            .Add(x => x.Panels, DefaultPanels)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, _ => raised++)));

        var disabled = cut.FindAll("[role='tab']")[1];
        Assert.True(disabled.HasAttribute("disabled"));
        Assert.Equal("true", disabled.GetAttribute("aria-disabled"));

        // The native disabled attribute already blocks the click; the guard in SelectAsync is the
        // belt-and-braces for a programmatic path.
        disabled.Click();
        Assert.Equal(0, raised);
    }

    // ---- ARIA wiring ---------------------------------------------------------------------------

    [Fact]
    public void Each_tab_and_its_panel_reference_each_other_by_id()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p
            .Add(x => x.Value, "b")
            .Add(x => x.PanelRender, TabPanelRender.Always));

        var tabs = cut.FindAll("[role='tab']");
        var panels = cut.FindAll("[role='tabpanel']");

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(panels[i].GetAttribute("id"), tabs[i].GetAttribute("aria-controls"));
            Assert.Equal(tabs[i].GetAttribute("id"), panels[i].GetAttribute("aria-labelledby"));
            Assert.False(string.IsNullOrWhiteSpace(tabs[i].GetAttribute("id")));
        }
    }

    [Fact]
    public void Two_tab_sets_on_one_page_do_not_share_ids()
    {
        using var ctx = new BunitContext();

        var first = RenderTabs(ctx);
        var second = RenderTabs(ctx);

        // Per-instance Guid prefix — ids must not collide, or aria-controls points at the wrong panel.
        Assert.NotEqual(
            first.Find("[role='tab']").GetAttribute("id"),
            second.Find("[role='tab']").GetAttribute("id"));
    }

    [Fact]
    public void Values_that_are_not_id_safe_are_slugged()
    {
        using var ctx = new BunitContext();

        PlanKeyGuardModule(ctx);

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, TabList(new Tab("my tab/one", "Alpha")))
            .Add(x => x.Panels, Panels(("my tab/one", "<p>x</p>"))));

        var id = cut.Find("[role='tab']").GetAttribute("id")!;
        // A space in an id breaks the aria-controls reference outright.
        Assert.DoesNotContain(' ', id);
        Assert.DoesNotContain('/', id);
        Assert.Equal(id, cut.Find("[role='tabpanel']").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Roving_tabindex_puts_exactly_one_tab_in_the_tab_order()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p.Add(x => x.Value, "b"));

        var tabs = cut.FindAll("[role='tab']");
        Assert.Equal("-1", tabs[0].GetAttribute("tabindex"));
        Assert.Equal("0", tabs[1].GetAttribute("tabindex"));
        Assert.Equal("-1", tabs[2].GetAttribute("tabindex"));
    }

    [Fact]
    public void The_active_panel_is_focusable_and_inactive_ones_are_not()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p
            .Add(x => x.Value, "a")
            .Add(x => x.PanelRender, TabPanelRender.Always));

        var panels = cut.FindAll("[role='tabpanel']");
        Assert.Equal("0", panels[0].GetAttribute("tabindex"));
        Assert.Equal("-1", panels[1].GetAttribute("tabindex"));
    }

    [Fact]
    public void Aria_orientation_matches_the_layout_axis()
    {
        using var ctx = new BunitContext();

        var horizontal = RenderTabs(ctx);
        Assert.Equal("horizontal", horizontal.Find(".atom-tabs-list").GetAttribute("aria-orientation"));

        var vertical = RenderTabs(ctx, p => p.Add(x => x.Orientation, TabsOrientation.Vertical));
        Assert.Equal("vertical", vertical.Find(".atom-tabs-list").GetAttribute("aria-orientation"));
        Assert.Equal("vertical", vertical.Find(".atom-tabs").GetAttribute("data-orientation"));
    }

    [Fact]
    public void AriaLabel_names_the_tablist()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p.Add(x => x.AriaLabel, "Account settings"));

        Assert.Equal("Account settings", cut.Find(".atom-tabs-list").GetAttribute("aria-label"));
    }

    // ---- panel rendering strategies ------------------------------------------------------------

    [Fact]
    public void Active_renders_only_the_selected_panel()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p.Add(x => x.Value, "b"));

        var panels = cut.FindAll("[role='tabpanel']");
        Assert.Single(panels);
        Assert.Contains("panel b", panels[0].TextContent);
    }

    [Fact]
    public void Always_renders_every_panel_and_hides_the_inactive_ones()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p
            .Add(x => x.Value, "b")
            .Add(x => x.PanelRender, TabPanelRender.Always));

        var panels = cut.FindAll("[role='tabpanel']");
        Assert.Equal(3, panels.Count);
        // hidden, not removed — so scroll position and uncommitted input survive a switch.
        Assert.True(panels[0].HasAttribute("hidden"));
        Assert.False(panels[1].HasAttribute("hidden"));
        Assert.True(panels[2].HasAttribute("hidden"));
    }

    [Fact]
    public void Lazy_renders_a_panel_on_first_activation_and_keeps_it_afterwards()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p
            .Add(x => x.Value, "a")
            .Add(x => x.PanelRender, TabPanelRender.Lazy));

        // Only the one that has been active so far.
        Assert.Single(cut.FindAll("[role='tabpanel']"));

        cut.FindAll("[role='tab']")[1].Click();
        Assert.Equal(2, cut.FindAll("[role='tabpanel']").Count);

        // Back to the first: both stay in the DOM, the inactive one merely hidden.
        cut.FindAll("[role='tab']")[0].Click();
        var panels = cut.FindAll("[role='tabpanel']");
        Assert.Equal(2, panels.Count);
        Assert.False(panels[0].HasAttribute("hidden"));
        Assert.True(panels[1].HasAttribute("hidden"));
    }

    // ---- axes and theming ----------------------------------------------------------------------

    [Theory]
    [InlineData(TabsVariant.Line, "line")]
    [InlineData(TabsVariant.Enclosed, "enclosed")]
    [InlineData(TabsVariant.Pill, "pill")]
    [InlineData(TabsVariant.Bar, "bar")]
    public void Variant_is_emitted(TabsVariant variant, string expected)
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p.Add(x => x.Variant, variant));

        Assert.Equal(expected, cut.Find(".atom-tabs").GetAttribute("data-variant"));
    }

    [Fact]
    public void Size_align_and_scrollable_are_emitted()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p
            .Add(x => x.Size, TabsSize.Large)
            .Add(x => x.Align, TabsAlign.Stretch)
            .Add(x => x.Scrollable, true));

        var root = cut.Find(".atom-tabs");
        Assert.Equal("large", root.GetAttribute("data-size"));
        Assert.Equal("stretch", root.GetAttribute("data-align"));
        Assert.Equal("true", root.GetAttribute("data-scrollable"));
    }

    [Fact]
    public void Default_effect_emits_no_attribute_and_multiword_effects_are_kebab_cased()
    {
        using var ctx = new BunitContext();

        var none = RenderTabs(ctx);
        Assert.Null(none.Find(".atom-tabs").GetAttribute("data-effect"));

        var fade = RenderTabs(ctx, p => p.Add(x => x.Effect, TabsEffect.FadePanel));
        Assert.Equal("fade-panel", fade.Find(".atom-tabs").GetAttribute("data-effect"));
    }

    [Fact]
    public void Theming_parameters_become_custom_properties()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p
            .Add(x => x.AccentColor, "#7c3aed")
            .Add(x => x.IndicatorThickness, 4d)
            .Add(x => x.Radius, 0d)
            .Add(x => x.Gap, 12d)
            .Add(x => x.Duration, 0.4));

        var style = cut.Find(".atom-tabs").GetAttribute("style") ?? "";
        Assert.Contains("--tabs-accent:#7c3aed", style);
        Assert.Contains("--tabs-indicator-thickness:4px", style);
        Assert.Contains("--tabs-radius:0px", style);
        Assert.Contains("--tabs-gap:12px", style);
        // Invariant culture: "0,4s" would be an invalid declaration.
        Assert.Contains("--tabs-duration:0.4s", style);
    }

    [Fact]
    public void Unset_theming_parameters_emit_nothing()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx);

        Assert.True(string.IsNullOrEmpty(cut.Find(".atom-tabs").GetAttribute("style")));
    }

    [Fact]
    public void Visible_false_hides_without_leaving_the_dom()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p.Add(x => x.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-tabs").GetAttribute("style"));
        Assert.Equal(3, cut.FindAll("[role='tab']").Count);
    }

    [Fact]
    public void CssClass_and_Style_layer_onto_the_root()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, p => p
            .Add(x => x.CssClass, "mine")
            .Add(x => x.Style, "opacity:.5")
            .Add(x => x.Gap, 4d));

        var root = cut.Find(".atom-tabs");
        Assert.Contains("mine", root.GetAttribute("class"));
        var style = root.GetAttribute("style") ?? "";
        Assert.True(style.IndexOf("opacity:.5", StringComparison.Ordinal)
                  > style.IndexOf("--tabs-gap", StringComparison.Ordinal));
    }
}
