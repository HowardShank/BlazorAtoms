using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.RadialMenus.Tests;

/// <summary>
/// bUnit coverage for <see cref="AtomRadialMenu"/>: open/close, nesting, the emitted positional
/// custom properties (checked against <see cref="RadialLayout"/> rather than against themselves),
/// shapes, labels, pagination, keyboard wiring, the JS module contract and teardown.
/// </summary>
/// <remarks>
/// The module is set up explicitly rather than with <c>JSRuntimeMode.Loose</c>, so the interop
/// contract — which functions exist and what they are called with — is asserted rather than
/// silently absorbed.
/// </remarks>
public class AtomRadialMenuTests
{
    private const string ModulePath = "./_content/BlazorAtoms.RadialMenus/atom-radialmenus.js";

    private static BunitJSModuleInterop Module(BunitContext ctx)
    {
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("attach", _ => true).SetVoidResult();
        module.SetupVoid("detach", _ => true).SetVoidResult();
        module.Setup<double[]>("measure", _ => true).SetResult([]);
        return module;
    }

    private static RadialMenuItem Leaf(string label) => new() { Label = label };

    private static IReadOnlyList<RadialMenuItem> Leaves(params string[] labels) =>
        labels.Select(Leaf).ToArray();

    /// <summary>A three-level tree: Shape -> Fill -> Gradient.</summary>
    private static IReadOnlyList<RadialMenuItem> Tree() =>
    [
        new RadialMenuItem
        {
            Label = "Shape",
            Children =
            [
                new RadialMenuItem { Label = "Fill", Children = [Leaf("Gradient"), Leaf("Solid")] },
                Leaf("Stroke"),
            ],
        },
        Leaf("Text"),
        new RadialMenuItem { Label = "Layer", Children = [Leaf("Up"), Leaf("Down")] },
    ];

    private static IRenderedComponent<AtomRadialMenu> RenderOpen(
        BunitContext ctx,
        IReadOnlyList<RadialMenuItem> items,
        Action<ComponentParameterCollectionBuilder<AtomRadialMenu>>? extra = null)
        => ctx.Render<AtomRadialMenu>(p =>
        {
            p.Add(x => x.Items, items);
            p.Add(x => x.Trigger, RadialMenuTrigger.Always);
            extra?.Invoke(p);
        });

    // ---- open / close -------------------------------------------------------------------------

    [Fact]
    public void Closed_by_default_renders_only_the_center_button()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = ctx.Render<AtomRadialMenu>(p => p.Add(x => x.Items, Leaves("A", "B", "C")));

        Assert.NotNull(cut.Find("button.atom-radial-menu-center"));
        Assert.Empty(cut.FindAll("button.atom-radial-menu-item"));
        Assert.Equal("false", cut.Find("div.atom-radial-menu").GetAttribute("data-open"));
    }

    [Fact]
    public void Clicking_the_center_opens_the_ring_and_reports_it()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var states = new List<bool>();
        var cut = ctx.Render<AtomRadialMenu>(p => p
            .Add(x => x.Items, Leaves("A", "B", "C"))
            .Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, states.Add)));

        cut.Find("button.atom-radial-menu-center").Click();

        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);
        Assert.Equal([true], states);

        cut.Find("button.atom-radial-menu-center").Click();
        Assert.Empty(cut.FindAll("button.atom-radial-menu-item"));
        Assert.Equal([true, false], states);
    }

    [Fact]
    public void Trigger_Always_needs_no_click_and_offers_no_toggle()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A", "B"));

        Assert.Equal(2, cut.FindAll("button.atom-radial-menu-item").Count);
        Assert.True(cut.Find("button.atom-radial-menu-center").HasAttribute("disabled"));
    }

    // ---- hover -------------------------------------------------------------------------------

    [Fact]
    public void Hovering_the_center_opens_the_ring()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = ctx.Render<AtomRadialMenu>(p => p
            .Add(x => x.Items, Leaves("A", "B", "C"))
            .Add(x => x.Trigger, RadialMenuTrigger.Hover));

        cut.Find("button.atom-radial-menu-center").PointerEnter();

        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);
    }

    [Fact]
    public void Leaving_the_host_does_not_close_the_ring_before_the_grace_period_elapses()
    {
        // The reason the grace period exists: items are positioned OUTSIDE the host's own box, so
        // travelling from the center button to an item crosses empty space owned by no element and
        // raises pointerleave on the host. Closing on that leave makes the items unreachable — the
        // pointer never gets there. A long delay here keeps the test deterministic.
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = ctx.Render<AtomRadialMenu>(p => p
            .Add(x => x.Items, Leaves("A", "B", "C"))
            .Add(x => x.Trigger, RadialMenuTrigger.Hover)
            .Add(x => x.HoverCloseDelay, 30_000));

        cut.Find("button.atom-radial-menu-center").PointerEnter();
        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);

        cut.Find("div.atom-radial-menu").PointerLeave();

        // Still open: the pointer is mid-flight across the gap.
        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);
    }

    [Fact]
    public async Task Arriving_at_an_item_calls_off_the_pending_close()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = ctx.Render<AtomRadialMenu>(p => p
            .Add(x => x.Items, Leaves("A", "B", "C"))
            .Add(x => x.Trigger, RadialMenuTrigger.Hover)
            .Add(x => x.HoverCloseDelay, 30_000));

        cut.Find("button.atom-radial-menu-center").PointerEnter();
        var host = cut.Find("div.atom-radial-menu");

        host.PointerLeave();    // crossing the gap
        host.PointerEnter();    // landed on an item, which fires enter on the host too

        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);

        // And the item is genuinely usable once reached. Find and click inside InvokeAsync so the
        // element reference cannot go stale between the two calls.
        await cut.InvokeAsync(() => cut.FindAll("button.atom-radial-menu-item")[1].Click());
        Assert.Empty(cut.FindAll("button.atom-radial-menu-item"));
    }

    [Fact]
    public void A_zero_delay_closes_on_leave_immediately()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = ctx.Render<AtomRadialMenu>(p => p
            .Add(x => x.Items, Leaves("A", "B", "C"))
            .Add(x => x.Trigger, RadialMenuTrigger.Hover)
            .Add(x => x.HoverCloseDelay, 0));

        cut.Find("button.atom-radial-menu-center").PointerEnter();
        cut.Find("div.atom-radial-menu").PointerLeave();

        Assert.Empty(cut.FindAll("button.atom-radial-menu-item"));
    }

    [Fact]
    public async Task The_grace_period_does_eventually_close_the_menu()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = ctx.Render<AtomRadialMenu>(p => p
            .Add(x => x.Items, Leaves("A", "B", "C"))
            .Add(x => x.Trigger, RadialMenuTrigger.Hover)
            .Add(x => x.HoverCloseDelay, 20));

        cut.Find("button.atom-radial-menu-center").PointerEnter();
        cut.Find("div.atom-radial-menu").PointerLeave();

        cut.WaitForAssertion(
            () => Assert.Empty(cut.FindAll("button.atom-radial-menu-item")),
            TimeSpan.FromSeconds(5));

        await Task.CompletedTask;
    }

    [Fact]
    public void A_click_trigger_ignores_hover_entirely()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = ctx.Render<AtomRadialMenu>(p => p.Add(x => x.Items, Leaves("A", "B")));

        cut.Find("button.atom-radial-menu-center").PointerEnter();
        Assert.Empty(cut.FindAll("button.atom-radial-menu-item"));

        cut.Find("button.atom-radial-menu-center").Click();
        cut.Find("div.atom-radial-menu").PointerLeave();

        // A leave must not close a click-opened menu.
        Assert.Equal(2, cut.FindAll("button.atom-radial-menu-item").Count);
    }

    // ---- cancellation -------------------------------------------------------------------------

    [Fact]
    public void A_cancelled_token_collapses_the_whole_component()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A", "B", "C"),
            p => p.Add(x => x.CancellationToken, new CancellationToken(canceled: true)));

        Assert.Empty(cut.FindAll("div.atom-radial-menu"));
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void A_cancelled_token_marshals_no_element_reference_to_the_module()
    {
        // Nothing was committed, so the root @ref was never captured. Passing that uncaptured
        // ElementReference to JS throws InvalidOperationException out of OnAfterRenderAsync — and
        // that exception replaces whatever really went wrong, hiding the true cause entirely.
        using var ctx = new BunitContext();
        var module = Module(ctx);

        RenderOpen(ctx, Leaves("A"), p => p
            .Add(x => x.CancellationToken, new CancellationToken(canceled: true))
            .Add(x => x.SizeMode, RadialMenuSizeMode.Measure));

        Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "attach");
        Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "measure");
        Assert.NotNull(module);
    }

    [Fact]
    public void Cancelling_after_a_normal_render_removes_the_menu()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A", "B", "C"));
        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);

        cut.Render(p => p.Add(x => x.CancellationToken, new CancellationToken(canceled: true)));
        Assert.Empty(cut.FindAll("div.atom-radial-menu"));
    }

    // ---- geometry reaches the DOM -------------------------------------------------------------

    [Fact]
    public void Positions_in_the_markup_match_what_RadialLayout_solved()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("N", "E", "S", "W"));
        var buttons = cut.FindAll("button.atom-radial-menu-item");

        var expected = RadialLayout.Solve(new RadialLayoutRequest { ItemCount = 4 });
        Assert.Equal(64, expected.Radius);

        Assert.Contains("--radialmenu-x:0px", buttons[0].GetAttribute("style"));
        Assert.Contains("--radialmenu-y:-64px", buttons[0].GetAttribute("style"));
        Assert.Contains("--radialmenu-x:64px", buttons[1].GetAttribute("style"));
        Assert.Contains("--radialmenu-y:0px", buttons[1].GetAttribute("style"));
        Assert.Contains("--radialmenu-y:64px", buttons[2].GetAttribute("style"));
        Assert.Contains("--radialmenu-x:-64px", buttons[3].GetAttribute("style"));
    }

    [Fact]
    public void A_trigonometric_residue_never_reaches_the_stylesheet_as_scientific_notation()
    {
        // cos(90 deg) is 6.1e-17, not 0. Unrounded that becomes "-3.9E-15px", which is not a CSS
        // length and is dropped silently by the browser.
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("N", "E", "S", "W"));

        Assert.DoesNotContain("E-", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("e-1", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void More_items_grow_the_ring_instead_of_dropping_any()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var many = Enumerable.Range(0, 20).Select(i => Leaf($"i{i}")).ToArray();
        var cut = RenderOpen(ctx, many);

        Assert.Equal(20, cut.FindAll("button.atom-radial-menu-item").Count);
    }

    // ---- invoking -----------------------------------------------------------------------------

    [Fact]
    public void Clicking_a_leaf_invokes_it_and_closes_the_menu()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        RadialMenuItem? invoked = null;
        var cut = ctx.Render<AtomRadialMenu>(p => p
            .Add(x => x.Items, Leaves("A", "B", "C"))
            .Add(x => x.OnItemInvoked, EventCallback.Factory.Create<RadialMenuItem>(this, i => invoked = i)));

        cut.Find("button.atom-radial-menu-center").Click();
        cut.FindAll("button.atom-radial-menu-item")[1].Click();

        Assert.Equal("B", invoked?.Label);
        Assert.Empty(cut.FindAll("button.atom-radial-menu-item"));
    }

    [Fact]
    public void CloseOnLeafInvoke_false_leaves_the_menu_up()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = ctx.Render<AtomRadialMenu>(p => p
            .Add(x => x.Items, Leaves("A", "B"))
            .Add(x => x.CloseOnLeafInvoke, false));

        cut.Find("button.atom-radial-menu-center").Click();
        cut.FindAll("button.atom-radial-menu-item")[0].Click();

        Assert.Equal(2, cut.FindAll("button.atom-radial-menu-item").Count);
    }

    [Fact]
    public void A_disabled_item_is_marked_disabled_and_does_nothing()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var invoked = 0;
        var items = new RadialMenuItem[] { new() { Label = "Off", Disabled = true }, Leaf("On") };
        var cut = RenderOpen(ctx, items, p => p
            .Add(x => x.OnItemInvoked, EventCallback.Factory.Create<RadialMenuItem>(this, _ => invoked++)));

        var first = cut.FindAll("button.atom-radial-menu-item")[0];
        Assert.True(first.HasAttribute("disabled"));
        Assert.Equal(0, invoked);
    }

    // ---- nesting ------------------------------------------------------------------------------

    [Fact]
    public void Opening_a_branch_adds_its_ring_while_the_parent_stays_put()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var opened = new List<string?>();
        var cut = RenderOpen(ctx, Tree(), p => p
            .Add(x => x.OnBranchOpened, EventCallback.Factory.Create<RadialMenuItem>(this, i => opened.Add(i.Label))));

        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);

        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // Shape

        Assert.Equal(5, cut.FindAll("button.atom-radial-menu-item").Count); // 3 + Fill, Stroke
        Assert.Equal(["Shape"], opened);
        Assert.Equal("true", cut.FindAll("button.atom-radial-menu-item")[0].GetAttribute("data-expanded"));
    }

    [Fact]
    public void SingleBranchOpen_closes_the_sibling_that_was_open()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree());

        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // Shape opens: 5 buttons
        Assert.Equal(5, cut.FindAll("button.atom-radial-menu-item").Count);

        // Layer is the third root item; its slot index is unchanged because the root ring is solved
        // independently of what is open below it.
        cut.FindAll("button.atom-radial-menu-item")[2].Click();  // Layer
        Assert.Equal(5, cut.FindAll("button.atom-radial-menu-item").Count); // 3 + Up, Down
        Assert.Null(cut.FindAll("button.atom-radial-menu-item")[0].GetAttribute("data-expanded"));
    }

    [Fact]
    public void SingleBranchOpen_false_keeps_both_subtrees_open()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree(), p => p.Add(x => x.SingleBranchOpen, false));

        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // Shape
        cut.FindAll("button.atom-radial-menu-item")[2].Click();  // Layer

        Assert.Equal(7, cut.FindAll("button.atom-radial-menu-item").Count); // 3 + 2 + 2
    }

    [Fact]
    public void Closing_a_branch_takes_its_whole_subtree_with_it()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree());

        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // Shape
        cut.FindAll("button.atom-radial-menu-item")[3].Click();  // Fill (grandchild ring opens)
        Assert.Equal(7, cut.FindAll("button.atom-radial-menu-item").Count);

        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // Shape again, closing
        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);

        // Reopening must not resurrect the grandchild ring from a stale open path.
        cut.FindAll("button.atom-radial-menu-item")[0].Click();
        Assert.Equal(5, cut.FindAll("button.atom-radial-menu-item").Count);
    }

    [Fact]
    public void Concentric_mode_keeps_the_center_and_pushes_children_to_the_next_ring_out()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree(), p => p.Add(x => x.ExpandMode, RadialMenuExpandMode.Concentric));
        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // Shape, which points at 0 degrees

        var child = cut.FindAll("button.atom-radial-menu-item").First(b => b.GetAttribute("data-depth") == "1");
        var style = child.GetAttribute("style") ?? "";

        // Shared center means the child ring is measured from the menu's origin, not from the
        // parent item — so the child must sit strictly further out than the parent's own 64px,
        // by at least half the parent item plus RingGap plus half of its own.
        var y = ParsePx(style, "--radialmenu-y");
        var x = ParsePx(style, "--radialmenu-x");
        var radius = Math.Sqrt(x * x + y * y);

        Assert.True(radius >= 64 + 24 + 16, $"child ring at {radius} is not clear of the parent ring");
    }

    private static double ParsePx(string style, string name)
    {
        var token = style.Split(';').FirstOrDefault(t => t.StartsWith(name + ":", StringComparison.Ordinal));
        Assert.NotNull(token);
        var value = token![(name.Length + 1)..].Replace("px", "", StringComparison.Ordinal);
        return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    public void Every_item_carries_its_own_path_from_the_root()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree());

        // Root ring: paths are just the item index.
        Assert.Equal(["0", "1", "2"], cut.FindAll("button.atom-radial-menu-item")
            .Select(b => b.GetAttribute("data-path")).ToArray());

        cut.FindAll("button.atom-radial-menu-item")[0].Click();   // open Shape

        // Its children are addressed beneath it, and depth is the segment count minus one.
        var children = cut.FindAll("button.atom-radial-menu-item")
            .Where(b => b.GetAttribute("data-depth") == "1")
            .Select(b => b.GetAttribute("data-path"))
            .ToArray();

        Assert.Equal(["0/0", "0/1"], children);
    }

    [Fact]
    public void A_path_prefix_selects_a_whole_subtree()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree());
        cut.FindAll("button.atom-radial-menu-item")[0].Click();   // Shape
        cut.FindAll("button.atom-radial-menu-item")[3].Click();   // Shape > Fill

        // Everything under Shape, at any depth — exactly what data-depth alone cannot express.
        var subtree = cut.FindAll("[data-path^=\"0/\"]")
            .Select(b => b.GetAttribute("data-path"))
            .ToArray();

        Assert.Equal(["0/0", "0/1", "0/0/0", "0/0/1"], subtree);
    }

    [Fact]
    public void A_pagination_stepper_has_no_path_because_it_is_not_in_the_tree()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var items = Enumerable.Range(0, 10).Select(i => Leaf($"i{i}")).ToArray();
        var cut = RenderOpen(ctx, items, p => p
            .Add(x => x.Overflow, RadialMenuOverflow.Paginate)
            .Add(x => x.PageSize, 4));

        var stepper = cut.Find("button.atom-radial-menu-stepper");
        Assert.False(stepper.HasAttribute("data-path"));
        Assert.Equal("page-next", stepper.GetAttribute("data-kind"));
    }

    [Fact]
    public void The_debug_tag_shows_angle_radius_and_path_together()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A", "B", "C"), p => p.Add(x => x.Debug, true));

        Assert.Equal("0° r64 · 0", cut.FindAll("span.atom-radial-menu-debug-tag")[0].TextContent);
    }

    [Fact]
    public void A_five_level_tree_opens_all_the_way_down()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        // Deepest path 0/0/0/0/0 — five levels, matching the playground's DeepTree source.
        RadialMenuItem Branch(string label, params RadialMenuItem[] kids) =>
            new() { Label = label, Children = kids };

        var deep = new[]
        {
            Branch("L0",
                Branch("L1",
                    Branch("L2",
                        Branch("L3", Leaf("L4a"), Leaf("L4b"))))),
            Leaf("Other"),
        };

        var cut = RenderOpen(ctx, deep);

        for (var depth = 0; depth < 4; depth++)
        {
            var branch = cut.FindAll($"[data-depth=\"{depth}\"][data-branch]")[0];
            cut.InvokeAsync(() => branch.Click()).Wait();
        }

        Assert.Equal(2, cut.FindAll("[data-depth=\"4\"]").Count);
        Assert.Equal("0/0/0/0/0", cut.FindAll("[data-depth=\"4\"]")[0].GetAttribute("data-path"));
    }

    [Fact]
    public void Drill_mode_replaces_the_ring_and_turns_the_center_into_Back()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree(), p => p.Add(x => x.ExpandMode, RadialMenuExpandMode.Drill));

        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);
        Assert.Equal("Radial menu", cut.Find("button.atom-radial-menu-center").GetAttribute("aria-label"));

        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // into Shape

        Assert.Equal(2, cut.FindAll("button.atom-radial-menu-item").Count); // Fill, Stroke only
        var center = cut.Find("button.atom-radial-menu-center");
        Assert.Equal("Back from Shape", center.GetAttribute("aria-label"));

        center.Click();
        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);
    }

    /// <summary>
    /// Drill renders only the deepest open ring, and that ring holds children - so the center button
    /// is the only element that can say which level you are on.
    /// </summary>
    [Fact]
    public void Drill_center_names_the_level_you_are_in()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree(), p => p.Add(x => x.ExpandMode, RadialMenuExpandMode.Drill));

        // At the top level there is no level to name.
        Assert.Empty(cut.FindAll("span.atom-radial-menu-center-label"));

        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // into Shape

        Assert.Equal("Shape", cut.Find("span.atom-radial-menu-center-label").TextContent);
        Assert.Equal("Back from Shape", cut.Find("button.atom-radial-menu-center").GetAttribute("aria-label"));

        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // into Fill

        Assert.Equal("Fill", cut.Find("span.atom-radial-menu-center-label").TextContent);

        cut.Find("button.atom-radial-menu-center").Click();      // back out to Shape

        Assert.Equal("Shape", cut.Find("span.atom-radial-menu-center-label").TextContent);
    }

    [Fact]
    public void An_item_graph_that_contains_itself_stops_rather_than_recursing_forever()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        // A shared instance used as its own descendant — easy to build by accident from a cache.
        var kids = new List<RadialMenuItem>();
        var loop = new RadialMenuItem { Label = "Loop", Children = kids };
        kids.Add(loop);

        var cut = RenderOpen(ctx, new[] { loop }, p => p
            .Add(x => x.SingleBranchOpen, false)
            .Add(x => x.Debug, true));

        // Open as far down as the guard allows; the assertion is simply that this returns.
        for (var i = 0; i < 24 && cut.FindAll("button.atom-radial-menu-item").Count > 0; i++)
        {
            var buttons = cut.FindAll("button.atom-radial-menu-item");
            buttons[^1].Click();
        }

        Assert.Contains("Nesting stopped at depth", cut.Markup);
    }

    // ---- shapes -------------------------------------------------------------------------------

    [Fact]
    public void The_default_shape_is_a_real_svg_circle()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A"));
        Assert.NotEmpty(cut.FindAll("button.atom-radial-menu-item circle"));
    }

    [Theory]
    [InlineData(RadialMenuShape.Hexagon, 6)]
    [InlineData(RadialMenuShape.Triangle, 3)]
    [InlineData(RadialMenuShape.Octagon, 8)]
    public void A_polygon_shape_emits_that_many_vertices(RadialMenuShape shape, int vertices)
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A"), p => p.Add(x => x.ItemShape, shape));

        var polygon = cut.Find("button.atom-radial-menu-item polygon");
        var points = polygon.GetAttribute("points") ?? "";
        Assert.Equal(vertices, points.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void The_emitted_polygon_is_exactly_what_RadialShapeGeometry_computed()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A"), p => p
            .Add(x => x.ItemShape, RadialMenuShape.Hexagon)
            .Add(x => x.ShapeRotation, 30));

        Assert.Equal(
            RadialShapeGeometry.PolygonPoints(6, 30),
            cut.Find("button.atom-radial-menu-item polygon").GetAttribute("points"));
    }

    [Fact]
    public void A_per_item_shape_overrides_the_menus_shape()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var items = new RadialMenuItem[]
        {
            new() { Label = "Round" },
            new() { Label = "Six", Shape = RadialMenuShape.Hexagon },
        };
        var cut = RenderOpen(ctx, items);

        var buttons = cut.FindAll("button.atom-radial-menu-item");
        Assert.Contains("<circle", buttons[0].InnerHtml);
        Assert.Contains("<polygon", buttons[1].InnerHtml);
    }

    [Fact]
    public void A_custom_path_is_used_verbatim()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A"), p => p
            .Add(x => x.ItemShape, RadialMenuShape.Custom)
            .Add(x => x.CustomPath, "M0 50 L50 0 L100 50 L50 100 Z"));

        Assert.Equal("M0 50 L50 0 L100 50 L50 100 Z",
            cut.Find("button.atom-radial-menu-item path").GetAttribute("d"));
    }

    // ---- labels -------------------------------------------------------------------------------

    [Fact]
    public void Inside_labels_render_in_the_shape_and_tooltip_only_labels_do_not()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var inside = RenderOpen(ctx, Leaves("Copy"));
        Assert.Equal("Copy", inside.Find("span.atom-radial-menu-label").TextContent);

        using var ctx2 = new BunitContext();
        Module(ctx2);
        var tooltip = RenderOpen(ctx2, Leaves("Copy"),
            p => p.Add(x => x.LabelPlacement, RadialMenuLabelPlacement.TooltipOnly));

        Assert.Empty(tooltip.FindAll("span.atom-radial-menu-label"));
        Assert.Equal("Copy", tooltip.Find("button.atom-radial-menu-item").GetAttribute("title"));
        Assert.Equal("Copy", tooltip.Find("button.atom-radial-menu-item").GetAttribute("aria-label"));
    }

    [Fact]
    public void An_outside_label_gets_its_own_element_outside_the_shape()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("Copy"),
            p => p.Add(x => x.LabelPlacement, RadialMenuLabelPlacement.Outside));

        Assert.Equal("Copy", cut.Find("span.atom-radial-menu-outside-label").TextContent);
        Assert.Empty(cut.FindAll("span.atom-radial-menu-label"));
    }

    [Fact]
    public void MaxLabelChars_truncates_the_rendered_text_but_not_the_accessible_name()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("Duplicate layer"), p => p.Add(x => x.MaxLabelChars, 4));

        Assert.Equal("Dupl…", cut.Find("span.atom-radial-menu-label").TextContent);
        Assert.Equal("Duplicate layer", cut.Find("button.atom-radial-menu-item").GetAttribute("aria-label"));
    }

    [Fact]
    public void An_item_template_replaces_the_default_content_but_not_the_shape()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        RenderFragment<RadialMenuItem> template =
            item => builder => builder.AddMarkupContent(0, $"<i>{item.Label}</i>");

        var cut = RenderOpen(ctx, Leaves("A"), p => p.Add(x => x.ItemTemplate, template));

        Assert.Contains("<i>A</i>", cut.Markup);
        Assert.NotEmpty(cut.FindAll("button.atom-radial-menu-item circle"));
    }

    // ---- sizing -------------------------------------------------------------------------------

    [Fact]
    public void FromFont_grows_a_shape_to_fit_a_long_label()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var fixedSize = RenderOpen(ctx, Leaves("Duplicate selection"));
        var fixedStyle = fixedSize.Find("button.atom-radial-menu-item").GetAttribute("style") ?? "";
        Assert.Contains("--radialmenu-size:48px", fixedStyle);

        using var ctx2 = new BunitContext();
        Module(ctx2);
        var grown = RenderOpen(ctx2, Leaves("Duplicate selection"),
            p => p.Add(x => x.SizeMode, RadialMenuSizeMode.FromFont));

        var grownStyle = grown.Find("button.atom-radial-menu-item").GetAttribute("style") ?? "";
        Assert.DoesNotContain("--radialmenu-size:48px", grownStyle);
    }

    [Fact]
    public void A_deeper_ring_is_scaled_down_by_SizeScalePerDepth()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree(), p => p.Add(x => x.SizeScalePerDepth, 0.5));
        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // Shape

        var deeper = cut.FindAll("button.atom-radial-menu-item")
            .First(b => b.GetAttribute("data-depth") == "1");

        Assert.Contains("--radialmenu-size:24px", deeper.GetAttribute("style"));
    }

    // ---- MaxVisibleDepth ----------------------------------------------------------------------

    /// <summary>L0 &gt; L1 &gt; L2 &gt; L3 &gt; (L4a, L4b), so the deepest open path is 0/0/0/0.</summary>
    private static IReadOnlyList<RadialMenuItem> Chain()
    {
        RadialMenuItem Branch(string label, params RadialMenuItem[] kids) =>
            new() { Label = label, Children = kids };

        return
        [
            Branch("L0", Branch("L1", Branch("L2", Branch("L3", Leaf("L4a"), Leaf("L4b"))))),
            Leaf("Other"),
        ];
    }

    /// <summary>Opens every branch on the chain, deepest last.</summary>
    private static void OpenTheChain(IRenderedComponent<AtomRadialMenu> cut)
    {
        foreach (var path in new[] { "0", "0/0", "0/0/0", "0/0/0/0" })
        {
            var b = cut.FindAll("button.atom-radial-menu-item")
                .FirstOrDefault(x => x.GetAttribute("data-path") == path);
            if (b is null) return;
            cut.InvokeAsync(() => b.Click()).Wait();
        }
    }

    /// <summary>
    /// The window re-roots rather than clipping: the frame's own first ring goes back to the base
    /// radius at full size, which is the whole point — a clipped ring would keep the radius and the
    /// depth-shrunk size of a level whose ancestors are no longer on screen.
    /// </summary>
    [Fact]
    public void MaxVisibleDepth_re_roots_the_frame_and_renders_only_that_many_levels()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Chain(), p => p
            .Add(x => x.ExpandMode, RadialMenuExpandMode.Concentric)
            .Add(x => x.MaxVisibleDepth, 2));

        OpenTheChain(cut);

        var buttons = cut.FindAll("button.atom-radial-menu-item");
        var depths = buttons.Select(b => b.GetAttribute("data-depth")).Distinct().OrderBy(d => d).ToArray();

        // Deepest open path is 0/0/0/0 (depth 4 for its children), so a 2-level window is rooted at
        // 0/0/0 and shows depths 3 and 4 only.
        Assert.Equal(["3", "4"], depths);

        // data-path still reports the true address, not a re-rooted one.
        Assert.Equal("0/0/0/0", buttons.First(b => b.GetAttribute("data-depth") == "3").GetAttribute("data-path"));

        var frameRoot = buttons.First(b => b.GetAttribute("data-depth") == "3");
        var style = frameRoot.GetAttribute("style") ?? "";
        var x = ParsePx(style, "--radialmenu-x");
        var y = ParsePx(style, "--radialmenu-y");

        // Base radius again: (CenterSize 64 + ItemSize 48) / 2 + ItemGap 8 = 64. And full size, not
        // 48 * 0.9^3 = 35.
        Assert.Equal(64, Math.Sqrt(x * x + y * y), 3);
        Assert.Contains("--radialmenu-size:48px", style);

        // The center button stands for the branch the frame hangs off.
        Assert.Equal("Back from L2", cut.Find("button.atom-radial-menu-center").GetAttribute("aria-label"));
        Assert.Equal("L2", cut.Find("span.atom-radial-menu-center-label").TextContent);
    }

    /// <summary>Going back has to slide the window inward, not just drop a ring off the outside.</summary>
    [Fact]
    public void Going_back_slides_the_window_towards_the_root()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Chain(), p => p
            .Add(x => x.ExpandMode, RadialMenuExpandMode.Concentric)
            .Add(x => x.MaxVisibleDepth, 2));

        OpenTheChain(cut);
        cut.Find("button.atom-radial-menu-center").Click();

        var depths = cut.FindAll("button.atom-radial-menu-item")
            .Select(b => b.GetAttribute("data-depth")).Distinct().OrderBy(d => d).ToArray();

        Assert.Equal(["2", "3"], depths);
        Assert.Equal("Back from L1", cut.Find("button.atom-radial-menu-center").GetAttribute("aria-label"));
    }

    /// <summary>A window of one is Drill's framing reached from the other direction.</summary>
    [Fact]
    public void A_window_of_one_shows_a_single_ring_like_Drill_does()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Chain(), p => p
            .Add(x => x.ExpandMode, RadialMenuExpandMode.Concentric)
            .Add(x => x.MaxVisibleDepth, 1));

        OpenTheChain(cut);

        var depths = cut.FindAll("button.atom-radial-menu-item")
            .Select(b => b.GetAttribute("data-depth")).Distinct().ToArray();

        Assert.Equal(["4"], depths);
        Assert.Equal(2, cut.FindAll("button.atom-radial-menu-item").Count);   // L4a, L4b
        Assert.Equal("Back from L3", cut.Find("button.atom-radial-menu-center").GetAttribute("aria-label"));
    }

    /// <summary>Drill already shows exactly one level, so the window is not its business.</summary>
    [Fact]
    public void Drill_ignores_MaxVisibleDepth()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Chain(), p => p
            .Add(x => x.ExpandMode, RadialMenuExpandMode.Drill)
            .Add(x => x.MaxVisibleDepth, 3));

        OpenTheChain(cut);

        Assert.Equal(2, cut.FindAll("button.atom-radial-menu-item").Count);   // L4a, L4b, one ring
        Assert.Equal("Back from L3", cut.Find("button.atom-radial-menu-center").GetAttribute("aria-label"));
    }

    /// <summary>A level count below one is not a window; it is a typo, and it is reported as one.</summary>
    [Fact]
    public void A_window_below_one_is_ignored_and_reported()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Chain(), p => p
            .Add(x => x.ExpandMode, RadialMenuExpandMode.Concentric)
            .Add(x => x.MaxVisibleDepth, 0)
            .Add(x => x.Debug, true));

        OpenTheChain(cut);

        var depths = cut.FindAll("button.atom-radial-menu-item")
            .Select(b => b.GetAttribute("data-depth")).Distinct().OrderBy(d => d).ToArray();

        Assert.Equal(["0", "1", "2", "3", "4"], depths);
        Assert.Contains("MaxVisibleDepth=0 is not a level count",
            cut.Find("ul.atom-radial-menu-advisories").TextContent);
    }

    /// <summary>
    /// The reason the parameter exists: containment narrows each level's arc by the branching factor,
    /// so an unwindowed Concentric radius roughly doubles per level once the neighbour-chord term
    /// overtakes the ring-gap floor. Capping the levels caps how far the arc narrows.
    /// </summary>
    [Fact]
    public void The_window_is_what_keeps_a_deep_concentric_radius_bounded()
    {
        double OuterRadius(int? window)
        {
            using var ctx = new BunitContext();
            Module(ctx);

            var cut = RenderOpen(ctx, Chain(), p =>
            {
                p.Add(x => x.ExpandMode, RadialMenuExpandMode.Concentric);
                p.Add(x => x.StartAngle, 90);
                p.Add(x => x.EndAngle, 270);
                if (window is int w) p.Add(x => x.MaxVisibleDepth, w);
            });

            OpenTheChain(cut);

            return cut.FindAll("button.atom-radial-menu-item")
                .Select(b => b.GetAttribute("style") ?? "")
                .Select(st => Math.Sqrt(Math.Pow(ParsePx(st, "--radialmenu-x"), 2)
                                        + Math.Pow(ParsePx(st, "--radialmenu-y"), 2)))
                .Max();
        }

        var unbounded = OuterRadius(null);
        var windowed = OuterRadius(2);

        Assert.True(unbounded > 250, $"expected the unwindowed menu to run away, got {unbounded}");
        Assert.True(windowed < 140, $"expected a 2-level window to stay near the base radius, got {windowed}");
    }

    // ---- overflow -----------------------------------------------------------------------------

    [Fact]
    public void Paginate_renders_steppers_and_they_change_the_page()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var items = Enumerable.Range(0, 10).Select(i => Leaf($"i{i}")).ToArray();
        var cut = RenderOpen(ctx, items, p => p
            .Add(x => x.Overflow, RadialMenuOverflow.Paginate)
            .Add(x => x.PageSize, 4));

        Assert.Single(cut.FindAll("button.atom-radial-menu-stepper"));
        Assert.Equal("i0", cut.Find("span.atom-radial-menu-label").TextContent);

        cut.Find("button.atom-radial-menu-stepper").Click();   // next

        Assert.Equal(2, cut.FindAll("button.atom-radial-menu-stepper").Count); // prev and next now
        Assert.Contains("i4", cut.Markup);
        Assert.DoesNotContain(">i0<", cut.Markup);
    }

    [Fact]
    public void Spin_shows_only_its_window()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var items = Enumerable.Range(0, 12).Select(i => Leaf($"i{i}")).ToArray();
        var cut = RenderOpen(ctx, items, p => p
            .Add(x => x.Overflow, RadialMenuOverflow.Spin)
            .Add(x => x.VisibleCount, 5));

        Assert.Equal(5, cut.FindAll("button.atom-radial-menu-item").Count);
    }

    [Fact]
    public void Rings_marks_each_item_with_the_ring_it_landed_on()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var items = Enumerable.Range(0, 10).Select(i => Leaf($"i{i}")).ToArray();
        var cut = RenderOpen(ctx, items, p => p
            .Add(x => x.Overflow, RadialMenuOverflow.Rings)
            .Add(x => x.MaxPerRing, 4));

        var buttons = cut.FindAll("button.atom-radial-menu-item");
        Assert.Equal(10, buttons.Count);

        // Ring 1 sits one item-plus-gap further out than ring 0 (64 -> 128 at the defaults).
        Assert.Contains("--radialmenu-y:-64px", buttons[0].GetAttribute("style"));
        Assert.Contains("--radialmenu-x:", buttons[4].GetAttribute("style"));
    }

    // ---- spokes, debug ------------------------------------------------------------------------

    [Fact]
    public void Spokes_are_absent_by_default_and_one_per_slot_when_asked_for()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var plain = RenderOpen(ctx, Leaves("A", "B", "C"));
        Assert.Empty(plain.FindAll("span.atom-radial-menu-spoke"));

        using var ctx2 = new BunitContext();
        Module(ctx2);
        var spoked = RenderOpen(ctx2, Leaves("A", "B", "C"),
            p => p.Add(x => x.SpokeMode, RadialMenuSpokeMode.ToShapeEdge));

        Assert.Equal(3, spoked.FindAll("span.atom-radial-menu-spoke").Count);
    }

    /// <summary>Every spoke's start point and angle, in render order.</summary>
    private static (double X, double Y, double Angle)[] Spokes(IRenderedComponent<AtomRadialMenu> cut) =>
        cut.FindAll("span.atom-radial-menu-spoke")
            .Select(s => s.GetAttribute("style") ?? "")
            .Select(style => (
                ParsePx(style, "--radialmenu-x"),
                ParsePx(style, "--radialmenu-y"),
                ParseDeg(style, "--radialmenu-angle")))
            .ToArray();

    private static double ParseDeg(string style, string name)
    {
        var token = style.Split(';').FirstOrDefault(t => t.StartsWith(name + ":", StringComparison.Ordinal));
        Assert.NotNull(token);
        var value = token![(name.Length + 1)..].Replace("deg", "", StringComparison.Ordinal);
        return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A spoke joins two buttons, so it cannot be drawn from the ring's origin along the slot's own
    /// angle. Under Concentric the ring's origin is the menu center while the button its items belong
    /// to is out on the previous ring, so that shortcut drew every nested spoke from the center
    /// button - a fan of lines all converging on the hub instead of on the item that was clicked.
    /// </summary>
    [Fact]
    public void Concentric_spokes_start_at_the_parent_item_not_at_the_center_button()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree(), p => p
            .Add(x => x.ExpandMode, RadialMenuExpandMode.Concentric)
            .Add(x => x.SpokeMode, RadialMenuSpokeMode.ToCenter));

        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // Shape, at 0 degrees, so (0, -64)

        var spokes = Spokes(cut);

        Assert.Equal(5, spokes.Length);                                            // 3 root slots + 2 children
        Assert.Equal(3, spokes.Count(s => s.X == 0 && s.Y == 0));                  // root ring hangs off the center
        Assert.Equal(2, spokes.Count(s => s.X == 0 && s.Y == -64));                // children hang off Shape

        // Child ring radius: 64 + 48/2 + RingGap 16 + 43.2/2 = 125.6, and its arc is the parent's
        // own 120-degree slice, so the two children sit at -60 and +60 FROM THE MENU CENTER. From
        // Shape at (0, -64) that is (-108.77, -62.8) and (108.77, -62.8): 1.2px further out and a
        // long way sideways, so the spokes run almost due west and due east - not at -60 and +60,
        // which is what the ring-origin shortcut produced.
        var children = spokes.Where(s => s.Y == -64).OrderBy(s => s.Angle).ToArray();
        Assert.Equal(-90.6, children[0].Angle, 1);
        Assert.Equal(90.6, children[1].Angle, 1);
    }

    /// <summary>
    /// The regression guard for the fix above: under Cascade the ring's origin and the parent button
    /// are the same point, so the generalised geometry has to reproduce the slot's own direction.
    /// </summary>
    /// <remarks>
    /// Compared modulo 360, because the two differ in representation and not in direction: the layout
    /// normalises a -60 degree slot to 300, while <c>Atan2</c> returns the -180..180 branch and gives
    /// -60 back. CSS <c>rotate()</c> cannot tell them apart, so only the direction is pinned here.
    /// </remarks>
    [Fact]
    public void Cascade_spokes_still_run_along_the_slots_own_direction()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree(), p => p.Add(x => x.SpokeMode, RadialMenuSpokeMode.ToCenter));

        cut.FindAll("button.atom-radial-menu-item")[0].Click();  // Shape, at 0 degrees, so (0, -64)

        var children = Spokes(cut)
            .Where(s => s.X == 0 && s.Y == -64)
            .Select(s => ((s.Angle % 360) + 360) % 360)
            .OrderBy(a => a)
            .ToArray();

        // The child arc is 0 +/- ChildSweep/2, so the two children run at 60 and 300 degrees.
        Assert.Equal(2, children.Length);
        Assert.Equal(60, children[0], 3);
        Assert.Equal(300, children[1], 3);
    }

    /// <summary>
    /// <c>ToShapeEdge</c> has to trim against the shape it actually starts from. At depth 0 that is
    /// the center button and <c>CenterShape</c>; deeper it is a parent item and <c>ItemShape</c>.
    /// The shapes are chosen so the two answers cannot be confused: a triangle's inradius is half its
    /// radius, a circle's is all of it.
    /// </summary>
    [Fact]
    public void ToShapeEdge_trims_against_the_button_the_spoke_starts_from()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree(), p => p
            .Add(x => x.ExpandMode, RadialMenuExpandMode.Concentric)
            .Add(x => x.SpokeMode, RadialMenuSpokeMode.ToShapeEdge)
            .Add(x => x.CenterShape, RadialMenuShape.Triangle)
            .Add(x => x.ItemShape, RadialMenuShape.Circle));

        cut.FindAll("button.atom-radial-menu-item")[0].Click();

        var starts = cut.FindAll("span.atom-radial-menu-spoke")
            .Select(s => s.GetAttribute("style") ?? "")
            .Select(style => (Y: ParsePx(style, "--radialmenu-y"), Start: ParsePx(style, "--radialmenu-spoke-start")))
            .ToArray();

        // Center button: 64/2 * cos(60) = 16.
        Assert.All(starts.Where(s => s.Y == 0), s => Assert.Equal(16, s.Start, 3));

        // Parent item, a circle at the default 48: 48/2 * 1 = 24.
        Assert.All(starts.Where(s => s.Y == -64), s => Assert.Equal(24, s.Start, 3));
    }

    [Fact]
    public void Debug_off_emits_no_overlay_at_all()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A", "B"));

        Assert.Empty(cut.FindAll("svg.atom-radial-menu-debug"));
        Assert.Empty(cut.FindAll("span.atom-radial-menu-debug-tag"));
        Assert.Empty(cut.FindAll("ul.atom-radial-menu-advisories"));
    }

    [Fact]
    public void Debug_on_draws_the_arc_bounds_a_tick_per_item_and_the_angle()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A", "B", "C"), p => p.Add(x => x.Debug, true));

        Assert.Single(cut.FindAll("svg.atom-radial-menu-debug"));
        Assert.Equal(2, cut.FindAll("line.atom-radial-menu-debug-arc").Count);
        Assert.Equal(3, cut.FindAll("line.atom-radial-menu-debug-tick").Count);
        Assert.Contains("0° r64", cut.Find("span.atom-radial-menu-debug-tag").TextContent);
    }

    [Fact]
    public void Debug_surfaces_the_layouts_advisories()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A", "B", "C", "D"), p => p
            .Add(x => x.Distribution, RadialMenuDistribution.Endpoints)
            .Add(x => x.Debug, true));

        Assert.Contains("Cyclic", cut.Find("ul.atom-radial-menu-advisories").TextContent);
    }

    /// <summary>
    /// The overlap RadialLayout structurally cannot see: a child ring's hub under Cascade is its
    /// parent item, so nothing in that solve knows the center button exists. Every number here is
    /// hand-computable, which is the point - the arc is narrowed to 20 degrees so the single root
    /// item sits at 0 degrees, and the child is aimed straight back down the spoke it came from.
    /// </summary>
    [Fact]
    public void Debug_reports_an_overlap_no_single_ring_solve_could_have_seen()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var items = new[]
        {
            new RadialMenuItem
            {
                Label = "Up",
                StartAngle = 170,
                EndAngle = 190,
                Children = [Leaf("Back at center")],
            },
        };

        var cut = RenderOpen(ctx, items, p => p
            .Add(x => x.StartAngle, -10)
            .Add(x => x.EndAngle, 10)
            .Add(x => x.SizeScalePerDepth, 1)
            .Add(x => x.Debug, true));

        cut.FindAll("button.atom-radial-menu-item")[0].Click();

        // Root item: hub clearance (64 + 48)/2 + 8 = 64, at 0 degrees, so (0, -64).
        // Child ring: hub is that item, clearance (48 + 48)/2 + 8 = 56, at 180 degrees, so (0, -8).
        // Center button needs (64 + 48)/2 + 8 = 64 and has 8.
        var text = cut.Find("ul.atom-radial-menu-advisories").TextContent;

        Assert.Contains("the center button and item 0/0 are 8px apart but need 64px to clear.", text);
        Assert.Contains("ExpandMode=Cascade solves each branch on its own", text);
    }

    /// <summary>
    /// The same check must not cry wolf on the mode that cannot overlap: a Concentric child ring is
    /// floored a whole RingGap outside its parent's radius, so no pair across two rings can ever be
    /// closer than ItemGap.
    /// </summary>
    [Fact]
    public void Concentric_reports_no_cross_ring_overlap_however_deep_the_tree_runs()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        RadialMenuItem Branch(string label, params RadialMenuItem[] kids) =>
            new() { Label = label, Children = kids };

        var deep = new[]
        {
            Branch("L0", Branch("L1", Branch("L2", Branch("L3", Leaf("L4a"), Leaf("L4b"))))),
            Leaf("Other"),
        };

        var cut = RenderOpen(ctx, deep, p => p
            .Add(x => x.ExpandMode, RadialMenuExpandMode.Concentric)
            .Add(x => x.Debug, true));

        for (var depth = 0; depth < 4; depth++)
        {
            var branch = cut.FindAll($"[data-depth=\"{depth}\"][data-branch]")[0];
            cut.InvokeAsync(() => branch.Click()).Wait();
        }

        Assert.Equal(2, cut.FindAll("[data-depth=\"4\"]").Count);

        // Other advisories are fair game here (a deep Concentric tree is legitimately crowded); the
        // cross-ring check specifically must stay silent.
        var advisories = cut.FindAll("ul.atom-radial-menu-advisories li").Select(li => li.TextContent);
        Assert.DoesNotContain(advisories, a => a.Contains("to clear.", StringComparison.Ordinal));
    }

    // ---- accessibility ------------------------------------------------------------------------

    [Fact]
    public void Exactly_one_button_in_the_whole_menu_is_tabbable()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree());
        cut.FindAll("button.atom-radial-menu-item")[0].Click();

        var tabbable = cut.FindAll("button")
            .Count(b => b.GetAttribute("tabindex") == "0");

        Assert.Equal(1, tabbable);
    }

    [Fact]
    public void A_branch_reports_its_expanded_state_to_assistive_technology()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree());
        var branch = cut.FindAll("button.atom-radial-menu-item")[0];

        Assert.Equal("true", branch.GetAttribute("aria-haspopup"));
        Assert.Equal("false", branch.GetAttribute("aria-expanded"));

        // A leaf is not a popup and must not claim to be one.
        var leaf = cut.FindAll("button.atom-radial-menu-item")[1];
        Assert.False(leaf.HasAttribute("aria-haspopup"));
        Assert.False(leaf.HasAttribute("aria-expanded"));
    }

    [Fact]
    public void Arrow_keys_walk_the_ring_and_Escape_closes_a_branch()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree());
        var host = cut.Find("div.atom-radial-menu");

        host.KeyDown("ArrowRight");
        Assert.Equal("0", cut.FindAll("button.atom-radial-menu-item")[0].GetAttribute("tabindex"));

        host.KeyDown("ArrowRight");
        Assert.Equal("0", cut.FindAll("button.atom-radial-menu-item")[1].GetAttribute("tabindex"));

        host.KeyDown("ArrowLeft");
        host.KeyDown("ArrowDown");  // opens Shape and moves into its ring
        Assert.Equal(5, cut.FindAll("button.atom-radial-menu-item").Count);

        host.KeyDown("Escape");
        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);
    }

    [Fact]
    public void KeyboardNavigation_false_ignores_the_arrow_keys()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Tree(), p => p.Add(x => x.KeyboardNavigation, false));
        cut.Find("div.atom-radial-menu").KeyDown("ArrowDown");

        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);
    }

    // ---- styling escape hatch -----------------------------------------------------------------

    [Fact]
    public void Styling_parameters_become_custom_properties_on_the_root()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A"), p => p
            .Add(x => x.ItemColor, "#111")
            .Add(x => x.ItemBackground, "#eee")
            .Add(x => x.CenterBackground, "tomato")
            .Add(x => x.CenterSize, 80)
            .Add(x => x.BorderWidth, 3));

        var style = cut.Find("div.atom-radial-menu").GetAttribute("style") ?? "";
        Assert.Contains("--radialmenu-color:#111;", style);
        Assert.Contains("--radialmenu-bg:#eee;", style);
        Assert.Contains("--radialmenu-center-bg:tomato;", style);
        Assert.Contains("--radialmenu-center-size:80px;", style);
        Assert.Contains("--radialmenu-border-width:3px;", style);
    }

    [Fact]
    public void The_inherited_escape_hatch_reaches_the_root_element()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A"), p => p
            .Add(x => x.CssClass, "my-menu")
            .Add(x => x.Style, "opacity:0.5;")
            .AddUnmatched("title", "Tools")
            .AddUnmatched("data-testid", "radial"));

        var root = cut.Find("div.atom-radial-menu");
        Assert.Contains("my-menu", root.GetAttribute("class"));
        Assert.EndsWith("opacity:0.5;", root.GetAttribute("style"));
        Assert.Equal("Tools", root.GetAttribute("title"));
        Assert.Equal("radial", root.GetAttribute("data-testid"));
    }

    // ---- interop ------------------------------------------------------------------------------

    [Fact]
    public void The_module_is_attached_once_on_the_first_render()
    {
        using var ctx = new BunitContext();
        var module = Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A", "B"));
        cut.Render();   // a second render must not attach again

        Assert.Single(module.Invocations["attach"]);
    }

    [Fact]
    public void Attach_declares_which_features_actually_need_the_browser()
    {
        using var ctx = new BunitContext();
        var module = Module(ctx);

        RenderOpen(ctx, Leaves("A"), p => p
            .Add(x => x.RadiusMode, RadialMenuRadiusMode.FitContainer)
            .Add(x => x.CloseOnOutsideClick, false));

        var call = module.VerifyInvoke("attach");
        var options = call.Arguments[2]!;
        var type = options.GetType();

        Assert.True((bool)type.GetProperty("watchResize")!.GetValue(options)!);
        Assert.False((bool)type.GetProperty("outsideClick")!.GetValue(options)!);
    }

    [Fact]
    public void A_resize_report_from_the_browser_recomputes_the_layout()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var items = Enumerable.Range(0, 12).Select(i => Leaf($"i{i}")).ToArray();
        var cut = RenderOpen(ctx, items, p => p
            .Add(x => x.RadiusMode, RadialMenuRadiusMode.FitContainer)
            .Add(x => x.Overflow, RadialMenuOverflow.Shrink));

        var before = cut.Find("button.atom-radial-menu-item").GetAttribute("style");

        cut.InvokeAsync(() => cut.Instance.OnHostResized(220, 220));

        Assert.NotEqual(before, cut.Find("button.atom-radial-menu-item").GetAttribute("style"));
    }

    [Fact]
    public async Task An_outside_click_reported_by_the_browser_closes_the_menu()
    {
        using var ctx = new BunitContext();
        Module(ctx);

        var cut = ctx.Render<AtomRadialMenu>(p => p.Add(x => x.Items, Leaves("A", "B")));
        cut.Find("button.atom-radial-menu-center").Click();
        Assert.Equal(2, cut.FindAll("button.atom-radial-menu-item").Count);

        await cut.InvokeAsync(() => cut.Instance.OnOutsideClick());

        Assert.Empty(cut.FindAll("button.atom-radial-menu-item"));
    }

    [Fact]
    public async Task Disposing_detaches_the_module()
    {
        using var ctx = new BunitContext();
        var module = Module(ctx);

        var cut = RenderOpen(ctx, Leaves("A"));
        await cut.Instance.DisposeAsync();

        module.VerifyInvoke("detach");
    }

    [Fact]
    public async Task Disposing_survives_a_circuit_that_has_already_gone_away()
    {
        // A fast route-away tears the circuit down before the component's own teardown runs. If
        // that throws out of DisposeAsync, Blazor surfaces it as an unhandled exception.
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("attach", _ => true).SetVoidResult();
        module.SetupVoid("detach", _ => true).SetException(new JSDisconnectedException("circuit gone"));

        var cut = RenderOpen(ctx, Leaves("A"));

        await cut.Instance.DisposeAsync();  // must not throw
        await cut.Instance.DisposeAsync();  // and must be idempotent
    }

    [Fact]
    public void Measure_mode_asks_the_browser_for_the_real_widths()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("attach", _ => true).SetVoidResult();
        module.SetupVoid("detach", _ => true).SetVoidResult();
        module.Setup<double[]>("measure", _ => true).SetResult([90, 40]);

        var cut = RenderOpen(ctx, Leaves("Duplicate selection", "Cut"),
            p => p.Add(x => x.SizeMode, RadialMenuSizeMode.Measure));

        var call = module.VerifyInvoke("measure");
        Assert.Equal("13px", call.Arguments[1]);
        Assert.Equal(["Duplicate selection", "Cut"], (string[])call.Arguments[2]!);

        // 90px wide at 15.6px tall needs a 96px circle; the ring is sized from the measurement,
        // not from the 48px default.
        cut.WaitForAssertion(() => Assert.Contains(
            "--radialmenu-size:96px",
            cut.Find("button.atom-radial-menu-item").GetAttribute("style")));
    }
}
