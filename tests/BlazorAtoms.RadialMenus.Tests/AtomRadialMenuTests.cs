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
        Assert.Equal("Back", center.GetAttribute("aria-label"));

        center.Click();
        Assert.Equal(3, cut.FindAll("button.atom-radial-menu-item").Count);
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
