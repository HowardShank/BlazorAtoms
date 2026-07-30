using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using static BlazorAtoms.Tabs.Tests.TabsHarness;

namespace BlazorAtoms.Tabs.Tests;

/// <summary>
/// Keyboard navigation per the ARIA tabs pattern.
/// </summary>
/// <remarks>
/// <para>Most tests here set <c>JSInterop.Mode = Loose</c>, which covers both interop calls the family
/// makes without planning either: the <c>atom-tabs.js</c> import on first render, and
/// <c>ElementReference.FocusAsync()</c> on an arrow key. The assertions are about selection and the
/// roving tabindex, which are the observable results either way.</para>
/// <para><b>The <c>FocusAsync</c> guard is deliberately untested.</b> bUnit does not route
/// <c>ElementReference.FocusAsync()</c> through its JSInterop mock at all — verified: a strict-mode
/// render with the module planned but focus unplanned passes — so there is no way from here to make
/// that call fail. Its guard exists for the production cases (a disconnected Blazor Server circuit, a
/// detached element) and is asserted only by inspection. The <c>atom-tabs.js</c> calls <i>do</i> go
/// through the mock, so those failure modes are covered.</para>
/// </remarks>
public class AtomTabsKeyboardTests
{
    /// <summary>The starting selection is an argument rather than a default the caller re-adds — bUnit
    /// rejects the same parameter being supplied twice.</summary>
    private static IRenderedComponent<AtomTabs> RenderTabs(BunitContext ctx, string value = "a",
        Action<ComponentParameterCollectionBuilder<AtomTabs>>? extra = null)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        return ctx.Render<AtomTabs>(p =>
        {
            p.Add(x => x.TabList, DefaultTabList);
            p.Add(x => x.Panels, DefaultPanels);
            p.Add(x => x.Value, value);
            extra?.Invoke(p);
        });
    }

    private static void Key(IRenderedComponent<AtomTabs> cut, string key) =>
        cut.Find(".atom-tabs-list").KeyDown(new KeyboardEventArgs { Key = key });

    private static string? Selected(IRenderedComponent<AtomTabs> cut) =>
        cut.FindAll("[role='tab']")
            .FirstOrDefault(t => t.GetAttribute("aria-selected") == "true")
            ?.TextContent.Trim();

    /// <summary>The tab currently in the tab order — in Manual mode this is the focused tab, which can
    /// differ from the selected one.</summary>
    private static string? Roving(IRenderedComponent<AtomTabs> cut) =>
        cut.FindAll("[role='tab']")
            .FirstOrDefault(t => t.GetAttribute("tabindex") == "0")
            ?.TextContent.Trim();

    [Fact]
    public void ArrowRight_selects_the_next_tab()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx);
        Key(cut, "ArrowRight");

        Assert.Equal("Bravo", Selected(cut));
    }

    [Fact]
    public void ArrowRight_wraps_at_the_end()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, "c");
        Key(cut, "ArrowRight");

        Assert.Equal("Alpha", Selected(cut));
    }

    [Fact]
    public void ArrowLeft_wraps_backwards_from_the_first_tab()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx);
        Key(cut, "ArrowLeft");

        Assert.Equal("Charlie", Selected(cut));
    }

    [Fact]
    public void Home_and_End_jump_to_the_ends()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, "b");

        Key(cut, "End");
        Assert.Equal("Charlie", Selected(cut));

        Key(cut, "Home");
        Assert.Equal("Alpha", Selected(cut));
    }

    [Fact]
    public void Horizontal_strips_ignore_the_vertical_arrows_for_stepping()
    {
        using var ctx = new BunitContext();

        // ArrowDown is the vertical axis's key; in a horizontal strip it must not step, or a page
        // using both orientations would behave differently from what aria-orientation advertises.
        var cut = RenderTabs(ctx);
        Key(cut, "ArrowDown");

        Assert.Equal("Alpha", Selected(cut));
    }

    [Fact]
    public void Vertical_strips_navigate_with_ArrowDown_and_ArrowUp()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, extra: p => p.Add(x => x.Orientation, TabsOrientation.Vertical));

        Key(cut, "ArrowDown");
        Assert.Equal("Bravo", Selected(cut));

        Key(cut, "ArrowUp");
        Assert.Equal("Alpha", Selected(cut));
    }

    [Fact]
    public void Vertical_strips_ignore_the_horizontal_arrows_for_stepping()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, extra: p => p.Add(x => x.Orientation, TabsOrientation.Vertical));
        Key(cut, "ArrowRight");

        Assert.Equal("Alpha", Selected(cut));
    }

    [Fact]
    public void Arrow_navigation_skips_disabled_tabs()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, TabList(
                new Tab("a", "Alpha"),
                new Tab("b", "Bravo", Disabled: true),
                new Tab("c", "Charlie")))
            .Add(x => x.Panels, DefaultPanels)
            .Add(x => x.Value, "a"));

        Key(cut, "ArrowRight");

        // Bravo is skipped entirely rather than selected-and-inert.
        Assert.Equal("Charlie", Selected(cut));
    }

    [Fact]
    public void End_lands_on_the_last_enabled_tab_not_the_last_tab()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, TabList(
                new Tab("a", "Alpha"),
                new Tab("b", "Bravo"),
                new Tab("c", "Charlie", Disabled: true)))
            .Add(x => x.Panels, DefaultPanels)
            .Add(x => x.Value, "a"));

        Key(cut, "End");

        Assert.Equal("Bravo", Selected(cut));
    }

    [Fact]
    public void Unhandled_keys_change_nothing()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx);

        Key(cut, "a");
        Key(cut, "Escape");
        Key(cut, "Tab");

        Assert.Equal("Alpha", Selected(cut));
    }

    [Fact]
    public void Automatic_activation_raises_ValueChanged_as_focus_moves()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var selected = new List<string>();

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, DefaultTabList)
            .Add(x => x.Panels, DefaultPanels)
            .Add(x => x.Value, "a")
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, v => selected.Add(v))));

        Key(cut, "ArrowRight");
        Key(cut, "ArrowRight");

        Assert.Equal(["b", "c"], selected);
    }

    [Fact]
    public void Manual_activation_moves_focus_without_selecting()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var raised = 0;

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, DefaultTabList)
            .Add(x => x.Panels, DefaultPanels)
            .Add(x => x.Value, "a")
            .Add(x => x.ActivationMode, TabsActivation.Manual)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, _ => raised++)));

        Key(cut, "ArrowRight");

        // Selection unmoved; only the roving tabindex followed the focus.
        Assert.Equal("Alpha", Selected(cut));
        Assert.Equal("Bravo", Roving(cut));
        Assert.Equal(0, raised);
    }

    [Fact]
    public void Manual_activation_then_Enter_selects_via_the_native_button()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, DefaultTabList)
            .Add(x => x.Panels, DefaultPanels)
            .Add(x => x.Value, "a")
            .Add(x => x.ActivationMode, TabsActivation.Manual));

        Key(cut, "ArrowRight");

        // The browser turns Enter/Space on a focused <button> into a click, which is why the component
        // handles no activation keys of its own — this asserts that click path still selects.
        cut.FindAll("[role='tab']")[1].Click();

        Assert.Equal("Bravo", Selected(cut));
    }

    [Fact]
    public void Manual_arrowing_keeps_the_roving_tabindex_on_one_tab_only()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, DefaultTabList)
            .Add(x => x.Panels, DefaultPanels)
            .Add(x => x.Value, "a")
            .Add(x => x.ActivationMode, TabsActivation.Manual));

        Key(cut, "ArrowRight");
        Key(cut, "ArrowRight");

        var zero = cut.FindAll("[role='tab']").Count(t => t.GetAttribute("tabindex") == "0");
        Assert.Equal(1, zero);
        Assert.Equal("Charlie", Roving(cut));
    }

    [Fact]
    public void A_failing_key_guard_leaves_navigation_working()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;

        // The module loads but its attach call fails — a real JSException, which is what a genuine
        // interop failure surfaces as. The guard only cancels a default scroll, so losing it must cost
        // nothing but that: selection still moves, and no exception escapes OnAfterRenderAsync.
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("attach", _ => true).SetException(new JSException("no dice"));
        module.SetupVoid("detach", _ => true).SetVoidResult();

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, DefaultTabList)
            .Add(x => x.Panels, DefaultPanels)
            .Add(x => x.Value, "a"));

        Key(cut, "ArrowRight");

        Assert.Equal("Bravo", Selected(cut));
    }

    [Fact]
    public void The_key_guard_module_is_attached_to_the_tablist_on_first_render()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        var module = PlanKeyGuardModule(ctx);

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, DefaultTabList)
            .Add(x => x.Panels, DefaultPanels));

        var call = module.VerifyInvoke("attach");
        // The strip, not the root — keydown bubbles to it and it carries aria-orientation, which the
        // guard reads at event time to decide which arrows to cancel.
        Assert.Single(call.Arguments);
        Assert.NotNull(cut.Find(".atom-tabs-list"));
    }

    [Fact]
    public void Attach_happens_once_not_on_every_render()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        var module = PlanKeyGuardModule(ctx);

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, DefaultTabList)
            .Add(x => x.Panels, DefaultPanels)
            .Add(x => x.Value, "a"));

        // Re-render with a different orientation: the guard reads the axis off the DOM at event time,
        // so it must not need re-attaching.
        cut.Render(p => p
            .Add(x => x.TabList, DefaultTabList)
            .Add(x => x.Panels, DefaultPanels)
            .Add(x => x.Value, "b")
            .Add(x => x.Orientation, TabsOrientation.Vertical));

        Assert.Single(module.Invocations["attach"]);
    }

    [Fact]
    public async Task Disposing_detaches_the_guard()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        var module = PlanKeyGuardModule(ctx);

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, DefaultTabList)
            .Add(x => x.Panels, DefaultPanels));

        Assert.Empty(module.Invocations["detach"]);

        await cut.Instance.DisposeAsync();

        Assert.Single(module.Invocations["detach"]);
    }

    [Fact]
    public void Modified_keypresses_are_left_to_the_browser()
    {
        using var ctx = new BunitContext();

        var cut = RenderTabs(ctx, "b");

        // Ctrl+Home scrolls the page to the top; it is not "go to the first tab". atom-tabs.js applies
        // the same test before cancelling a default, so the two stay in agreement.
        cut.Find(".atom-tabs-list").KeyDown(new KeyboardEventArgs { Key = "Home", CtrlKey = true });
        Assert.Equal("Bravo", Selected(cut));

        cut.Find(".atom-tabs-list").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", ShiftKey = true });
        Assert.Equal("Bravo", Selected(cut));
    }

    [Fact]
    public void A_strip_with_no_enabled_tabs_ignores_the_keyboard()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<AtomTabs>(p => p
            .Add(x => x.TabList, TabList(
                new Tab("a", "Alpha", Disabled: true),
                new Tab("b", "Bravo", Disabled: true)))
            .Add(x => x.Panels, DefaultPanels));

        // No enabled tab to move to, and nothing to divide by — must not throw.
        Key(cut, "ArrowRight");
        Key(cut, "Home");

        Assert.Null(Selected(cut));
    }
}
