using System.Globalization;
using BlazorAtoms.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorAtoms.RadialMenus;

/// <summary>
/// A radial (pie / wheel) menu. A center button sits at the middle and its items radiate outward on
/// an arc you define — 0 degrees is straight up, angles increase clockwise. Any item can be a leaf
/// action or a branch that opens a ring of its own, to any depth.
/// </summary>
/// <remarks>
/// <para><b>Item count is never capped.</b> The ring radius is solved from collision geometry, so
/// adding items grows the ring instead of breaking it. When there is a ceiling on space, the
/// <c>Overflow</c> policy decides what gives — grow anyway, wrap into concentric rings, shrink the
/// items, paginate, or spin.</para>
/// <para><b>You should not have to work out angles for nested rings.</b> A branch's children default
/// to an arc centered on the direction the branch already points, so nesting needs no configuration.
/// Per-item <c>StartAngle</c>/<c>EndAngle</c> exist for the one item that needs to sit somewhere
/// specific.</para>
/// <para><b>All geometry is in <see cref="RadialLayout"/> and <see cref="RadialShapeGeometry"/>,</b>
/// which are pure functions with no Blazor, DOM or JS types in reach. This component turns their
/// output into CSS custom properties and handles state, focus and interop.</para>
/// </remarks>
public partial class AtomRadialMenu : AtomComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorAtoms.RadialMenus/atom-radialmenus.js";

    /// <summary>Depth beyond which nesting stops being followed. Guards against an item graph that
    /// contains itself — sharing one <see cref="RadialMenuItem"/> instance as its own descendant is
    /// easy to do by accident and would otherwise recurse forever.</summary>
    private const int MaxDepth = 16;

    /// <summary>How many overlapping pairs the cross-ring check names before it just counts the
    /// rest. A collapsed tree can produce dozens of them, and a wall of advisories is unreadable.</summary>
    private const int MaxReportedCollisions = 3;

    private readonly List<RadialRing> _rings = [];
    private readonly HashSet<string> _openPaths = [];
    private readonly Dictionary<string, ElementReference> _refs = [];
    private readonly Dictionary<string, double> _measured = [];
    private readonly List<string> _buildAdvisories = [];

    private ElementReference _hostRef;
    private CancellationTokenSource? _hoverCts;
    private IJSObjectReference? _module;
    private DotNetObjectReference<AtomRadialMenu>? _selfRef;
    private bool _attached;
    private bool _rootOpen;
    private bool _lastOpenParam;
    private double _hostHalf;
    private double _spinOffset;
    private int _pageIndex;
    private string? _focusKey;
    private bool _focusDirty;
    private bool _awaitingMeasure;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    // ---- data ---------------------------------------------------------------------------------

    /// <summary>The menu's top-level items. An item with children is a branch.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<RadialMenuItem> Items { get; set; } = [];

    /// <summary>Replaces the default icon-and-label content of every item button. The shape, the
    /// positioning and the accessibility wiring stay ours; only the inside of the button is yours.</summary>
    [Parameter] public RenderFragment<RadialMenuItem>? ItemTemplate { get; set; }

    /// <summary>Replaces the center button's content. Default is a hamburger glyph, or a back arrow
    /// plus the current level's name whenever the visible frame is not the true root — under
    /// <see cref="RadialMenuExpandMode.Drill"/>, or under a <see cref="MaxVisibleDepth"/> window that
    /// has re-rooted.</summary>
    [Parameter] public RenderFragment? CenterTemplate { get; set; }

    // ---- geometry -----------------------------------------------------------------------------

    /// <summary>Where the arc begins, in degrees. 0 is straight up. Default 0.</summary>
    [Parameter] public double StartAngle { get; set; }

    /// <summary>Where the arc ends, in degrees. Equal to <see cref="StartAngle"/> means a full
    /// circle. Default 360.</summary>
    [Parameter] public double EndAngle { get; set; } = 360;

    /// <summary>How items spread across the arc. Default
    /// <see cref="RadialMenuDistribution.Auto"/>, which is right nearly always.</summary>
    [Parameter] public RadialMenuDistribution Distribution { get; set; } = RadialMenuDistribution.Auto;

    /// <summary>Degrees between items. Only read when <see cref="Distribution"/> is
    /// <see cref="RadialMenuDistribution.FixedStep"/>.</summary>
    [Parameter] public double? AngleStep { get; set; }

    /// <summary>Which way the arc sweeps from <see cref="StartAngle"/>. Default clockwise.</summary>
    [Parameter] public RadialMenuDirection Direction { get; set; } = RadialMenuDirection.Clockwise;

    /// <summary>Requested ring radius in pixels. Under the default
    /// <see cref="RadialMenuRadiusMode.Auto"/> this is a floor, not a cap — the collision solve can
    /// still push the ring further out. Null lets the solve decide entirely.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>How <see cref="Radius"/> is interpreted. Default
    /// <see cref="RadialMenuRadiusMode.Auto"/>.</summary>
    [Parameter] public RadialMenuRadiusMode RadiusMode { get; set; } = RadialMenuRadiusMode.Auto;

    /// <summary>Hard ceiling on the ring radius. Leave null under
    /// <see cref="RadialMenuRadiusMode.FitContainer"/> to have it measured from the host box.</summary>
    [Parameter] public double? MaxRadius { get; set; }

    /// <summary>Radial gap between concentric rings. Default 16.</summary>
    [Parameter] public double RingGap { get; set; } = 16;

    /// <summary>Clear space kept between neighbouring items, and between an item and the button it
    /// radiates from. Default 8.</summary>
    [Parameter] public double ItemGap { get; set; } = 8;

    /// <summary>What gives when the ring will not fit. Default
    /// <see cref="RadialMenuOverflow.GrowRadius"/>.</summary>
    [Parameter] public RadialMenuOverflow Overflow { get; set; } = RadialMenuOverflow.GrowRadius;

    /// <summary>Items per ring under <see cref="RadialMenuOverflow.Rings"/>. Null derives the wrap
    /// point from the available radius instead.</summary>
    [Parameter] public int? MaxPerRing { get; set; }

    /// <summary>Items per page under <see cref="RadialMenuOverflow.Paginate"/>. Default 8.</summary>
    [Parameter] public int PageSize { get; set; } = 8;

    /// <summary>Visible window under <see cref="RadialMenuOverflow.Spin"/>. Default 8.</summary>
    [Parameter] public int VisibleCount { get; set; } = 8;

    /// <summary>Items on one ring past which the layout notes that the ring is crowded. Advisory
    /// only, surfaced through <see cref="Debug"/>. Default 12.</summary>
    [Parameter] public int CrowdingWarnThreshold { get; set; } = 12;

    /// <summary>How many ring levels are on screen at once. Null (the default) renders every open
    /// level.</summary>
    /// <remarks>
    /// <para>Set it and the menu <b>re-roots</b>: open a branch deeper than the window and the
    /// ancestor that falls out of view becomes the new center, whose children start again at the base
    /// radius across the full <see cref="StartAngle"/>–<see cref="EndAngle"/> arc. The center button
    /// then names that ancestor and goes back, exactly as it does under
    /// <see cref="RadialMenuExpandMode.Drill"/>.</para>
    /// <para>This is the answer to a deep <see cref="RadialMenuExpandMode.Concentric"/> menu running
    /// off screen. Keeping a child inside its parent's slice means each level's arc is the parent's
    /// divided by the branching factor, and equal-size items on a narrowing arc need
    /// <c>R ≥ (ItemSize + ItemGap) / (2·sin(arc/2))</c> — so the radius roughly doubles per level and
    /// no <see cref="RadialMenuArcMode"/> changes that. Capping the levels caps how far the arc can
    /// narrow, which caps the radius. At the default sizes with a three-way branch, <b>3 is about the
    /// largest window whose radius is still set by <see cref="RingGap"/> rather than by item
    /// collision</b>; 2 is comfortable.</para>
    /// <para>Ignored by <see cref="RadialMenuExpandMode.Drill"/>, which already shows exactly one
    /// level. Values below 1 are ignored too, with an advisory under <see cref="Debug"/>.</para>
    /// <para><c>data-depth</c> and <c>data-path</c> keep reporting an item's <b>true</b> depth and
    /// address. Only sizing, radius and the overflow state are measured from the visible frame — a
    /// re-rooted ring renders at full size rather than shrinking by a depth the viewer can no longer
    /// see.</para>
    /// </remarks>
    [Parameter] public int? MaxVisibleDepth { get; set; }

    // ---- size ---------------------------------------------------------------------------------

    /// <summary>Diameter of the center button in pixels. Default 64.</summary>
    [Parameter] public double CenterSize { get; set; } = 64;

    /// <summary>Diameter of an item in pixels — exact under
    /// <see cref="RadialMenuSizeMode.Fixed"/>, a floor under the text-driven modes. Default 48.</summary>
    [Parameter] public double ItemSize { get; set; } = 48;

    /// <summary>Smallest an item may become under <see cref="RadialMenuOverflow.Shrink"/> or deep
    /// nesting. Default 24.</summary>
    [Parameter] public double MinItemSize { get; set; } = 24;

    /// <summary>Where an item's diameter comes from. Default <see cref="RadialMenuSizeMode.Fixed"/>,
    /// the only mode whose geometry is fully known before the browser lays anything out.</summary>
    [Parameter] public RadialMenuSizeMode SizeMode { get; set; } = RadialMenuSizeMode.Fixed;

    /// <summary>Multiplier applied per level of nesting, so deeper rings read as subordinate.
    /// Default 0.9. Use 1 to keep every level the same size.</summary>
    [Parameter] public double SizeScalePerDepth { get; set; } = 0.9;

    /// <summary>Quantum computed sizes are rounded down to, so a resize cannot make items jitter by
    /// fractions of a pixel. Default 4.</summary>
    [Parameter] public double SizeStep { get; set; } = 4;

    /// <summary>Label font size in pixels. Drives text-driven sizing as well as the rendered text.
    /// Default 13.</summary>
    [Parameter] public double FontSize { get; set; } = 13;

    /// <summary>Label line height as a multiple of <see cref="FontSize"/>. Default 1.2.</summary>
    [Parameter] public double LineHeight { get; set; } = 1.2;

    /// <summary>Average glyph width as a fraction of <see cref="FontSize"/>, used by
    /// <see cref="RadialMenuSizeMode.FromFont"/>. Default 0.55, about right for a humanist
    /// sans-serif.</summary>
    [Parameter] public double CharWidthRatio { get; set; } = 0.55;

    /// <summary>Fraction of the shape's inscribed circle the label is allowed to fill. Below 1
    /// leaves breathing room. Default 0.95.</summary>
    [Parameter] public double TextFitFactor { get; set; } = 0.95;

    // ---- shape --------------------------------------------------------------------------------

    /// <summary>Outline of the center button. Default <see cref="RadialMenuShape.Circle"/>.</summary>
    [Parameter] public RadialMenuShape CenterShape { get; set; } = RadialMenuShape.Circle;

    /// <summary>Outline of the items. Default <see cref="RadialMenuShape.Circle"/> — the shape that
    /// fits the most label in the least diameter.</summary>
    [Parameter] public RadialMenuShape ItemShape { get; set; } = RadialMenuShape.Circle;

    /// <summary>Side count for <see cref="RadialMenuShape.Polygon"/>. Default 6.</summary>
    [Parameter] public int? ShapeSides { get; set; }

    /// <summary>Extra clockwise rotation applied to every polygon, on top of the rotation its name
    /// implies. 30 turns a point-up hexagon into the flat-top honeycomb orientation.</summary>
    [Parameter] public double ShapeRotation { get; set; }

    /// <summary>SVG path <c>d</c> for <see cref="RadialMenuShape.Custom"/>, drawn in a 100x100 box.</summary>
    [Parameter] public string? CustomPath { get; set; }

    // ---- content ------------------------------------------------------------------------------

    /// <summary>Where an item's label is drawn. Default
    /// <see cref="RadialMenuLabelPlacement.Inside"/>.</summary>
    [Parameter] public RadialMenuLabelPlacement LabelPlacement { get; set; } = RadialMenuLabelPlacement.Inside;

    /// <summary>Set false to drop labels from the markup entirely, keeping only icons and the
    /// accessible name. Default true.</summary>
    [Parameter] public bool ShowLabels { get; set; } = true;

    /// <summary>Truncate rendered labels past this many characters (the full text stays as the
    /// accessible name and tooltip). Null leaves them intact.</summary>
    [Parameter] public int? MaxLabelChars { get; set; }

    /// <summary>Distance from the shape's edge to an outside label. Default 6.</summary>
    [Parameter] public double LabelOffset { get; set; } = 6;

    // ---- behavior -----------------------------------------------------------------------------

    /// <summary>What opens the menu. Default <see cref="RadialMenuTrigger.Click"/>.</summary>
    [Parameter] public RadialMenuTrigger Trigger { get; set; } = RadialMenuTrigger.Click;

    /// <summary>Where a branch's children appear. Default
    /// <see cref="RadialMenuExpandMode.Cascade"/>.</summary>
    [Parameter] public RadialMenuExpandMode ExpandMode { get; set; } = RadialMenuExpandMode.Cascade;

    /// <summary>How a child ring's arc is derived. Default
    /// <see cref="RadialMenuArcMode.AutoCenteredOnParent"/>, which needs no angles from you.</summary>
    [Parameter] public RadialMenuArcMode ArcMode { get; set; } = RadialMenuArcMode.AutoCenteredOnParent;

    /// <summary>Width in degrees of the arc a branch's children fan across under
    /// <see cref="RadialMenuArcMode.AutoCenteredOnParent"/>. Default 120.</summary>
    [Parameter] public double ChildSweep { get; set; } = 120;

    /// <summary>Opening a branch closes its siblings, so one path is open at a time. Default true.</summary>
    [Parameter] public bool SingleBranchOpen { get; set; } = true;

    /// <summary>Invoking a leaf closes the whole menu. Default true.</summary>
    [Parameter] public bool CloseOnLeafInvoke { get; set; } = true;

    /// <summary>A pointer press outside the menu closes it. Needs the JS module. Default true.</summary>
    [Parameter] public bool CloseOnOutsideClick { get; set; } = true;

    /// <summary>
    /// Grace period in milliseconds before <see cref="RadialMenuTrigger.Hover"/> closes the menu
    /// after the pointer leaves. Default 250.
    /// </summary>
    /// <remarks>
    /// Not cosmetic — without it the menu is unusable. Items are positioned outside the host's own
    /// box, so travelling from the center button to an item crosses a band of empty space that
    /// belongs to no element: at the defaults that is
    /// <c>Radius 64 - CenterSize/2 - ItemSize/2 = 8px</c>. Crossing it raises
    /// <c>pointerleave</c> on the host, which would close the ring before the pointer ever arrives.
    /// The same gap exists between a branch and its child ring under
    /// <see cref="RadialMenuExpandMode.Cascade"/>. Re-entering the menu cancels the pending close, so
    /// the delay is only ever spent on a genuine exit. Set 0 to close immediately.
    /// </remarks>
    [Parameter] public double HoverCloseDelay { get; set; } = 250;

    /// <summary>Whether the menu is open. Bindable with <c>@bind-Open</c>; left unbound, the
    /// component manages it and reports changes through <see cref="OpenChanged"/>.</summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>Fires whenever the open state changes.</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    // ---- look ---------------------------------------------------------------------------------

    /// <summary>Whether a line is drawn from the center out to each item. Default none.</summary>
    [Parameter] public RadialMenuSpokeMode SpokeMode { get; set; } = RadialMenuSpokeMode.None;

    /// <summary>Spoke thickness in pixels. Default 2.</summary>
    [Parameter] public double SpokeWidth { get; set; } = 2;

    /// <summary>Spoke colour. Maps to <c>--radialmenu-spoke-color</c>.</summary>
    [Parameter] public string? SpokeColor { get; set; }

    /// <summary>Item foreground. Maps to <c>--radialmenu-color</c>.</summary>
    [Parameter] public string? ItemColor { get; set; }

    /// <summary>Item fill. Maps to <c>--radialmenu-bg</c>.</summary>
    [Parameter] public string? ItemBackground { get; set; }

    /// <summary>Item outline colour. Maps to <c>--radialmenu-border</c>.</summary>
    [Parameter] public string? ItemBorderColor { get; set; }

    /// <summary>Outline thickness in pixels, constant at any item size. Default 1.</summary>
    [Parameter] public double BorderWidth { get; set; } = 1;

    /// <summary>Center button foreground. Maps to <c>--radialmenu-center-color</c>.</summary>
    [Parameter] public string? CenterColor { get; set; }

    /// <summary>Center button fill. Maps to <c>--radialmenu-center-bg</c>.</summary>
    [Parameter] public string? CenterBackground { get; set; }

    /// <summary>Fill on hover. Maps to <c>--radialmenu-hover-bg</c>.</summary>
    [Parameter] public string? HoverBackground { get; set; }

    /// <summary>Fill of an open branch. Maps to <c>--radialmenu-active-bg</c>.</summary>
    [Parameter] public string? ActiveBackground { get; set; }

    /// <summary>Colour of an outside label. Maps to <c>--radialmenu-label-color</c>.</summary>
    [Parameter] public string? LabelColor { get; set; }

    /// <summary>Opacity of a disabled item, 0 to 1. Default 0.4.</summary>
    [Parameter] public double DisabledOpacity { get; set; } = 0.4;

    // ---- motion -------------------------------------------------------------------------------

    /// <summary>Open/close transition length in milliseconds. Default 180. Ignored when the visitor
    /// asks for reduced motion.</summary>
    [Parameter] public double AnimationDuration { get; set; } = 180;

    /// <summary>Extra delay per item, so a ring unfolds rather than appearing at once. Default 25.</summary>
    [Parameter] public double StaggerDelay { get; set; } = 25;

    /// <summary>CSS timing function for the open/close transition. Default a gentle overshoot.</summary>
    [Parameter] public string? Easing { get; set; }

    // ---- a11y + diagnostics -------------------------------------------------------------------

    /// <summary>Accessible name for the menu as a whole. Default "Radial menu".</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Arrow-key navigation around and between rings. Default true.</summary>
    [Parameter] public bool KeyboardNavigation { get; set; } = true;

    /// <summary>Draws the arc bounds, each item's angle and radius, and any collision the layout had
    /// to report. For tuning angles during development — never leave it on in production.</summary>
    [Parameter] public bool Debug { get; set; }

    // ---- events -------------------------------------------------------------------------------

    /// <summary>Fires when a leaf item is activated.</summary>
    [Parameter] public EventCallback<RadialMenuItem> OnItemInvoked { get; set; }

    /// <summary>Fires when a branch opens.</summary>
    [Parameter] public EventCallback<RadialMenuItem> OnBranchOpened { get; set; }

    /// <summary>Fires when a branch closes.</summary>
    [Parameter] public EventCallback<RadialMenuItem> OnBranchClosed { get; set; }

    // ---- lifecycle ----------------------------------------------------------------------------

    protected override void OnParametersSet()
    {
        // Adopt the parameter only when the CONSUMER changed it. Comparing against the last value
        // we saw (not against our own state) is what lets the same component work bound with
        // @bind-Open and unbound with its own internal state.
        if (Open != _lastOpenParam)
        {
            _lastOpenParam = Open;
            _rootOpen = Open;
        }

        BuildRings();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await AttachAsync();
        if (_awaitingMeasure) await RunMeasurePassAsync();
        if (_focusDirty) await ApplyFocusAsync();
    }

    // ---- ring construction --------------------------------------------------------------------

    /// <summary>
    /// Walks the open branches and turns each into a solved ring. Runs on every parameter change,
    /// and after any state change that could alter the tree, so <c>_rings</c> is always the single
    /// source of truth the markup reads.
    /// </summary>
    private void BuildRings()
    {
        _rings.Clear();
        _refs.Clear();
        _buildAdvisories.Clear();

        if (!EffectiveOpen || Items.Count == 0) return;

        if (MaxVisibleDepth is int requested && requested < 1)
            _buildAdvisories.Add($"MaxVisibleDepth={requested} is not a level count; ignored, every open level is rendered.");

        if (ExpandMode == RadialMenuExpandMode.Drill)
        {
            BuildDrillRing();
            if (Debug) DetectCrossRingCollisions();
            return;
        }

        // The window's root. Empty unless MaxVisibleDepth is set AND the open chain is deeper than
        // it, in which case the frame is re-rooted at the ancestor that keeps the window full.
        var rootPath = VisibleRootPath;
        var rootItems = ItemsAt(rootPath);

        if (rootItems is null || rootItems.Count == 0)
        {
            // The path stopped resolving — Items can change underneath an open path at any time.
            rootPath = "";
            rootItems = Items;
        }

        var window = EffectiveMaxVisibleDepth;
        var rootDepth = DepthOf(rootPath);

        if (rootDepth > 0)
        {
            _buildAdvisories.Add($"MaxVisibleDepth={window} re-rooted the menu at '{rootPath}'; {rootDepth} ancestor level(s) are off screen and the center button goes back to them.");

            if (!SingleBranchOpen)
                _buildAdvisories.Add("MaxVisibleDepth follows the single deepest open path, so with SingleBranchOpen=false any branch open outside that path is not rendered at all.");
        }

        // Depth-first so a ring's children land immediately after it, which keeps the rendered
        // stacking order (and therefore the tab order) matching the visual hierarchy.
        var pending = new Stack<PendingRing>();
        pending.Push(new PendingRing(
            rootDepth, 0, rootPath, 0, 0, CenterSize, null, rootItems, StartAngle, EndAngle, null,
            new SpokeAnchor(0, 0, CenterSize, IsCenter: true)));

        while (pending.Count > 0)
        {
            var p = pending.Pop();
            var ring = SolveRing(p);
            _rings.Add(ring);

            if (p.Depth + 1 > MaxDepth)
            {
                _buildAdvisories.Add($"Nesting stopped at depth {MaxDepth}. If the menu is not really that deep, check for a RadialMenuItem that appears among its own descendants.");
                continue;
            }

            // Window full. Re-rooting already picked a root that makes this the last level, so this
            // only bites when the open set is not one chain.
            if (window is int w && p.VisibleDepth >= w - 1) continue;

            foreach (var slot in ring.Layout.Slots)
            {
                if (slot.Kind != RadialMenuSlotKind.Item) continue;
                var item = ring.Items[slot.ItemIndex];
                if (!item.IsBranch) continue;

                var key = ChildKey(p.PathKey, slot.ItemIndex);
                if (!_openPaths.Contains(key)) continue;

                pending.Push(BuildChild(p, ring, slot, item, key));
            }
        }

        DropFocusIfItLeftTheFrame();
        if (Debug) DetectCrossRingCollisions();
    }

    /// <summary>
    /// Re-rooting and going back both change how many rings exist, so a remembered focus key can
    /// point past the end of the list. Left alone, the roving tabindex would give its 0 to a button
    /// that is not rendered and nothing in the menu would be tabbable.
    /// </summary>
    private void DropFocusIfItLeftTheFrame()
    {
        if (_focusKey is null || _focusKey == CenterKey) return;

        var (ringIndex, slotIndex) = ParseKey(_focusKey);
        if (ringIndex < 0 || ringIndex >= _rings.Count
            || slotIndex < 0 || slotIndex >= _rings[ringIndex].Layout.Slots.Count)
            _focusKey = null;
    }

    // ---- the visible frame --------------------------------------------------------------------

    /// <summary>The window size, or null when there is no window. Guards the parameter so the rest
    /// of the walk never has to think about zero or negative level counts.</summary>
    private int? EffectiveMaxVisibleDepth => MaxVisibleDepth is int n && n > 0 ? n : null;

    /// <summary>
    /// The path the center button stands for: the branch the visible frame hangs off, or empty at the
    /// true root. <see cref="RadialMenuExpandMode.Drill"/> is the one-level case of the same idea.
    /// </summary>
    private string VisibleRootPath
    {
        get
        {
            if (ExpandMode == RadialMenuExpandMode.Drill) return DeepestOpenPath();
            if (EffectiveMaxVisibleDepth is not int window) return "";

            // Rings run from the root's depth to the deepest open depth inclusive, so filling a
            // window of `window` levels means rooting `window - 1` levels above the deepest.
            var deepest = DeepestOpenPath();
            var rootDepth = DepthOf(deepest) - window + 1;
            return rootDepth <= 0 ? "" : AncestorAt(deepest, rootDepth);
        }
    }

    /// <summary>The deepest open path, or empty when nothing is open. Ties are broken ordinally so
    /// the frame cannot flicker between two equally deep branches across renders.</summary>
    private string DeepestOpenPath() => _openPaths.Count == 0
        ? ""
        : _openPaths.OrderByDescending(DepthOf).ThenBy(p => p, StringComparer.Ordinal).First();

    /// <summary>The prefix of <paramref name="path"/> holding <paramref name="depth"/> segments.</summary>
    private static string AncestorAt(string path, int depth)
    {
        if (depth <= 0) return "";
        var segments = path.Split('/');
        return depth >= segments.Length ? path : string.Join('/', segments.Take(depth));
    }

    /// <summary>The items a ring rooted at <paramref name="path"/> would show, or null if the path no
    /// longer resolves against <see cref="Items"/>.</summary>
    private IReadOnlyList<RadialMenuItem>? ItemsAt(string path) =>
        path.Length == 0 ? Items : ItemAt(path)?.Children;

    /// <summary>The item at a slash-joined index path, or null on any segment that does not resolve.
    /// The path is our own state, but <see cref="Items"/> can change under it between renders.</summary>
    private RadialMenuItem? ItemAt(string path)
    {
        if (path.Length == 0) return null;

        var items = Items;
        RadialMenuItem? current = null;

        foreach (var segment in path.Split('/'))
        {
            if (items is null
                || !int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                || index < 0 || index >= items.Count)
                return null;

            current = items[index];
            items = current.Children;
        }

        return current;
    }

    /// <summary>
    /// Reports items from <em>different</em> rings that landed on top of each other.
    /// </summary>
    /// <remarks>
    /// <see cref="RadialLayout"/> only ever sees one ring, so it can prove that a ring's own items
    /// clear each other and clear their hub - it cannot know that two sibling subtrees were each
    /// solved independently and collided, nor that a deep <see cref="RadialMenuExpandMode.Cascade"/>
    /// ring (whose hub is its parent item, not the center button) drifted back across the center.
    /// Both are real and both are invisible to the per-ring solve, so the check belongs here, where
    /// every slot's absolute position is known.
    /// <para>Runs only under <see cref="Debug"/>: it is quadratic in the rendered slot count and its
    /// only product is an advisory.</para>
    /// </remarks>
    private void DetectCrossRingCollisions()
    {
        // Ring -1 is the center button - an obstacle that only ring 0's own solve accounts for.
        var placed = new List<(int Ring, string Name, double X, double Y, double Size)>
        {
            (-1, "the center button", 0, 0, CenterSize),
        };

        for (var r = 0; r < _rings.Count; r++)
        {
            var ring = _rings[r];
            foreach (var slot in ring.Layout.Slots)
            {
                if (slot.Kind != RadialMenuSlotKind.Item) continue;

                var path = SlotPath(ring, slot);
                placed.Add((
                    r,
                    path is null ? $"a slot at depth {ring.Depth}" : $"item {path}",
                    ring.OriginX + slot.X,
                    ring.OriginY + slot.Y,
                    slot.Size));
            }
        }

        var hits = new List<(double Deficit, string Note)>();

        for (var i = 0; i < placed.Count; i++)
        {
            for (var j = i + 1; j < placed.Count; j++)
            {
                var a = placed[i];
                var b = placed[j];

                // Same ring is RadialLayout's job, and it already accounts for the wrap gap and for
                // multi-ring staggering. Re-checking it here would double-report every finding.
                if (a.Ring == b.Ring) continue;

                var needed = (a.Size + b.Size) / 2 + ItemGap;
                var apart = Hypot(b.X - a.X, b.Y - a.Y);
                if (apart >= needed) continue;

                hits.Add((needed - apart, string.Create(CultureInfo.InvariantCulture,
                    $"{a.Name} and {b.Name} are {apart:0.#}px apart but need {needed:0.#}px to clear.")));
            }
        }

        if (hits.Count == 0) return;

        // Worst first: the deepest overlap is the one worth looking at, not whichever pair the walk
        // happened to reach first.
        foreach (var hit in hits.OrderByDescending(h => h.Deficit).Take(MaxReportedCollisions))
            _buildAdvisories.Add(hit.Note);

        if (hits.Count > MaxReportedCollisions)
            _buildAdvisories.Add($"{hits.Count - MaxReportedCollisions} further overlap(s) between rings are not listed.");

        _buildAdvisories.Add(ExpandMode == RadialMenuExpandMode.Cascade
            ? "ExpandMode=Cascade solves each branch on its own, so sibling subtrees can collide once the tree runs more than two levels deep. Narrow ChildSweep, lower SizeScalePerDepth, or switch to Concentric (children stay inside the parent's slice) or Drill (one ring at a time, same footprint at any depth)."
            : "These rings were solved independently. Widen the arc, raise RingGap, or lower SizeScalePerDepth.");
    }

    /// <summary>
    /// Drill mode renders exactly one ring — the deepest open level — with the center button acting
    /// as Back. Nothing else is on screen, so the footprint is the same at any depth.
    /// </summary>
    private void BuildDrillRing()
    {
        var items = Items;
        var path = "";

        for (var depth = 0; depth < MaxDepth; depth++)
        {
            var next = items
                .Select((item, i) => (item, i))
                .FirstOrDefault(t => t.item.IsBranch && _openPaths.Contains(ChildKey(path, t.i)));

            if (next.item is null) break;
            path = ChildKey(path, next.i);
            items = next.item.Children!;
        }

        // VisibleDepth 0: Drill's ring is alone on screen at the base radius, so it is sized and
        // paginated as the frame's own first level, not as the deep level it happens to be.
        var pending = new PendingRing(
            DepthOf(path), 0, path, 0, 0, CenterSize, null, items, StartAngle, EndAngle, null,
            new SpokeAnchor(0, 0, CenterSize, IsCenter: true));

        _rings.Add(SolveRing(pending));
        DropFocusIfItLeftTheFrame();
    }

    private PendingRing BuildChild(PendingRing parent, RadialRing parentRing, RadialSlot slot, RadialMenuItem item, string key)
    {
        var (start, end) = ChildArc(item, slot, parentRing.Layout);
        var depth = parent.Depth + 1;
        var visibleDepth = parent.VisibleDepth + 1;

        // Where the parent BUTTON is. Both modes hang their spokes off it; they differ only in where
        // the ring itself is centred, which is a separate question.
        var anchor = new SpokeAnchor(
            parent.OriginX + slot.X,
            parent.OriginY + slot.Y,
            slot.Size,
            IsCenter: false);

        if (ExpandMode == RadialMenuExpandMode.Concentric)
        {
            // Same center, further out. The floor keeps the child ring clear of the parent ring it
            // has to sit outside of, measured from the parent slot's own distance from center.
            var parentRadius = Math.Sqrt(slot.X * slot.X + slot.Y * slot.Y);
            var floor = parentRadius + slot.Size / 2 + RingGap + SizeForDepth(visibleDepth) / 2;
            return new PendingRing(depth, visibleDepth, key, 0, 0, CenterSize, floor, item.Children!, start, end, slot.AngleDegrees, anchor);
        }

        // Cascade: the parent item becomes the hub the children radiate from, so they clear it the
        // same way the first ring clears the center button. Here the ring's origin and the spoke
        // anchor coincide - under Concentric they do not, which is the whole reason the anchor is
        // carried separately.
        return new PendingRing(
            depth, visibleDepth, key,
            parent.OriginX + slot.X,
            parent.OriginY + slot.Y,
            slot.Size,
            null,
            item.Children!,
            start, end,
            slot.AngleDegrees,
            anchor);
    }

    /// <summary>
    /// A child ring's arc. Per-item angles always win; otherwise the mode decides, and the default
    /// mode needs no input from the consumer at all.
    /// </summary>
    private (double Start, double End) ChildArc(RadialMenuItem item, RadialSlot slot, RadialLayoutResult parent)
    {
        var mode = EffectiveArcMode;
        var (start, end) = mode switch
        {
            RadialMenuArcMode.InheritSweep => (StartAngle, EndAngle),
            RadialMenuArcMode.SliceOfParent => (
                slot.AngleDegrees - parent.StepDegrees / 2,
                slot.AngleDegrees + parent.StepDegrees / 2),
            RadialMenuArcMode.Explicit => (StartAngle, EndAngle),
            _ => (
                slot.AngleDegrees - ChildSweep / 2,
                slot.AngleDegrees + ChildSweep / 2),
        };

        return (item.StartAngle ?? start, item.EndAngle ?? end);
    }

    /// <summary>
    /// A concentric ring confined to its parent's slice is what "centered on the parent" means once
    /// the child shares the parent's center — so the default mode resolves to that there, and the
    /// consumer still gets sensible nesting without picking an arc mode per expand mode.
    /// </summary>
    private RadialMenuArcMode EffectiveArcMode =>
        ExpandMode == RadialMenuExpandMode.Concentric && ArcMode == RadialMenuArcMode.AutoCenteredOnParent
            ? RadialMenuArcMode.SliceOfParent
            : ArcMode;

    private RadialRing SolveRing(PendingRing p)
    {
        // Everything the viewer can judge by eye is measured from the VISIBLE depth: size, the base
        // radius, and which ring owns the page/spin state. Depth stays true so data-depth and
        // data-path still address the real tree.
        var size = RingItemSize(p.Items, p.VisibleDepth);
        var isFrameRoot = p.VisibleDepth == 0;
        var request = new RadialLayoutRequest
        {
            ItemCount = p.Items.Count,
            StartAngle = p.StartAngle,
            EndAngle = p.EndAngle,
            Distribution = Distribution,
            AngleStep = AngleStep,
            Direction = Direction,
            Radius = p.RadiusFloor ?? (isFrameRoot ? Radius : null),
            RadiusMode = RadiusMode,
            MaxRadius = EffectiveMaxRadius,
            CenterSize = p.HubSize,
            ItemSize = size,
            MinItemSize = MinItemSize,
            ItemGap = ItemGap,
            RingGap = RingGap,
            Overflow = Overflow,
            MaxPerRing = MaxPerRing,
            PageSize = PageSize,
            PageIndex = isFrameRoot ? _pageIndex : 0,
            VisibleCount = VisibleCount,
            SpinOffset = isFrameRoot ? _spinOffset : 0,
            SizeStep = SizeStep,
            CrowdingWarnThreshold = CrowdingWarnThreshold,
        };

        return new RadialRing(
            p.Depth, p.VisibleDepth, p.PathKey, p.OriginX, p.OriginY, p.HubSize,
            p.StartAngle, p.EndAngle, p.ParentAngle,
            p.SpokeFrom, p.Items, RadialLayout.Solve(request));
    }

    /// <summary>
    /// One diameter for every item on a ring. Uniform by design: mixed sizes on a ring read as an
    /// accident, and the largest label would set the spacing anyway.
    /// </summary>
    private double RingItemSize(IReadOnlyList<RadialMenuItem> items, int depth)
    {
        var basis = Math.Max(MinItemSize, ItemSize * Math.Pow(SizeScalePerDepth, depth));

        // Only a label drawn INSIDE the shape can force the shape to grow. Outside and tooltip-only
        // labels are exactly why those placements exist.
        if (SizeMode == RadialMenuSizeMode.Fixed || LabelPlacement != RadialMenuLabelPlacement.Inside)
            return basis;

        var sides = RadialShapeGeometry.Sides(ItemShape, ShapeSides);
        var textHeight = FontSize * LineHeight;
        var needed = 0.0;

        foreach (var item in items)
        {
            var width = SizeMode == RadialMenuSizeMode.Measure
                ? MeasuredWidth(item.Label)
                : RadialShapeGeometry.EstimateTextWidth(item.Label, FontSize, CharWidthRatio);
            needed = Math.Max(needed, RadialShapeGeometry.RequiredSize(width, textHeight, sides, TextFitFactor));
        }

        var scaled = RadialLayout.Quantize(needed * Math.Pow(SizeScalePerDepth, depth), SizeStep, MinItemSize);
        return Math.Max(basis, scaled);
    }

    private double MeasuredWidth(string? label)
    {
        if (string.IsNullOrEmpty(label)) return 0;
        if (_measured.TryGetValue(label, out var w)) return w;

        // Not measured yet. Fall back to the estimate for this render and queue a measure pass —
        // the ring is held hidden until it completes, so the estimate is never actually seen.
        _awaitingMeasure = true;
        return RadialShapeGeometry.EstimateTextWidth(label, FontSize, CharWidthRatio);
    }

    private double? EffectiveMaxRadius => MaxRadius
        ?? (RadiusMode == RadialMenuRadiusMode.FitContainer && _hostHalf > 0 ? _hostHalf : null);

    /// <summary>
    /// The branch the visible frame hangs off, or null at the true root. A re-rooted menu shows only
    /// descendants, so the center button is the one place that can say where you are — under
    /// <see cref="RadialMenuExpandMode.Drill"/> and under a <see cref="MaxVisibleDepth"/> window
    /// alike.
    /// </summary>
    private RadialMenuItem? CenterItem => ItemAt(VisibleRootPath);

    private static int DepthOf(string pathKey) =>
        pathKey.Length == 0 ? 0 : pathKey.Count(c => c == '/') + 1;

    private static string ChildKey(string parentKey, int index) =>
        parentKey.Length == 0 ? index.ToString(CultureInfo.InvariantCulture) : $"{parentKey}/{index}";

    private double SizeForDepth(int depth) =>
        Math.Max(MinItemSize, ItemSize * Math.Pow(SizeScalePerDepth, depth));

    /// <summary>A ring queued for solving. Internal, so nothing here reaches the consumer.</summary>
    private sealed record PendingRing(
        int Depth,
        int VisibleDepth,
        string PathKey,
        double OriginX,
        double OriginY,
        double HubSize,
        double? RadiusFloor,
        IReadOnlyList<RadialMenuItem> Items,
        double StartAngle,
        double EndAngle,
        double? ParentAngle,
        SpokeAnchor SpokeFrom);
}

/// <summary>One solved ring, ready to render. Internal by design — the consumer sees items and
/// parameters, never the layout state behind them.</summary>
internal sealed record RadialRing(
    int Depth,
    int VisibleDepth,
    string PathKey,
    double OriginX,
    double OriginY,
    double HubSize,
    double ArcStart,
    double ArcEnd,
    double? ParentAngle,
    SpokeAnchor SpokeFrom,
    IReadOnlyList<RadialMenuItem> Items,
    RadialLayoutResult Layout);

/// <summary>
/// The button a ring's spokes are drawn from: its center point, its diameter, and whether it is the
/// center button (which may carry a different shape from the items, so a different inradius).
/// </summary>
/// <remarks>
/// Deliberately <b>not</b> the same thing as the ring's origin. Under
/// <see cref="RadialMenuExpandMode.Cascade"/> the two coincide, which is why the distinction went
/// unnoticed - but under <see cref="RadialMenuExpandMode.Concentric"/> a child ring is centred on the
/// menu while the button its items belong to sits out on the previous ring, so a spoke drawn from the
/// origin runs from the center button instead of from the item that was clicked.
/// </remarks>
internal sealed record SpokeAnchor(double X, double Y, double Size, bool IsCenter);
