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

    private readonly List<RadialRing> _rings = [];
    private readonly HashSet<string> _openPaths = [];
    private readonly Dictionary<string, ElementReference> _refs = [];
    private readonly Dictionary<string, double> _measured = [];
    private readonly List<string> _buildAdvisories = [];

    private ElementReference _hostRef;
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
    /// when <see cref="ExpandMode"/> is <see cref="RadialMenuExpandMode.Drill"/> and the menu is
    /// below the top level.</summary>
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

        if (ExpandMode == RadialMenuExpandMode.Drill)
        {
            BuildDrillRing();
            return;
        }

        // Depth-first so a ring's children land immediately after it, which keeps the rendered
        // stacking order (and therefore the tab order) matching the visual hierarchy.
        var pending = new Stack<PendingRing>();
        pending.Push(new PendingRing(0, "", 0, 0, CenterSize, null, Items, StartAngle, EndAngle, null));

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

        var pending = new PendingRing(DepthOf(path), path, 0, 0, CenterSize, null, items, StartAngle, EndAngle, null);
        _rings.Add(SolveRing(pending));
    }

    private PendingRing BuildChild(PendingRing parent, RadialRing parentRing, RadialSlot slot, RadialMenuItem item, string key)
    {
        var (start, end) = ChildArc(item, slot, parentRing.Layout);
        var depth = parent.Depth + 1;

        if (ExpandMode == RadialMenuExpandMode.Concentric)
        {
            // Same center, further out. The floor keeps the child ring clear of the parent ring it
            // has to sit outside of, measured from the parent slot's own distance from center.
            var parentRadius = Math.Sqrt(slot.X * slot.X + slot.Y * slot.Y);
            var floor = parentRadius + slot.Size / 2 + RingGap + SizeForDepth(depth) / 2;
            return new PendingRing(depth, key, 0, 0, CenterSize, floor, item.Children!, start, end, slot.AngleDegrees);
        }

        // Cascade: the parent item becomes the hub the children radiate from, so they clear it the
        // same way the first ring clears the center button.
        return new PendingRing(
            depth, key,
            parent.OriginX + slot.X,
            parent.OriginY + slot.Y,
            slot.Size,
            null,
            item.Children!,
            start, end,
            slot.AngleDegrees);
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
        var size = RingItemSize(p.Items, p.Depth);
        var request = new RadialLayoutRequest
        {
            ItemCount = p.Items.Count,
            StartAngle = p.StartAngle,
            EndAngle = p.EndAngle,
            Distribution = Distribution,
            AngleStep = AngleStep,
            Direction = Direction,
            Radius = p.RadiusFloor ?? (p.Depth == 0 ? Radius : null),
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
            PageIndex = p.Depth == 0 ? _pageIndex : 0,
            VisibleCount = VisibleCount,
            SpinOffset = p.Depth == 0 ? _spinOffset : 0,
            SizeStep = SizeStep,
            CrowdingWarnThreshold = CrowdingWarnThreshold,
        };

        return new RadialRing(
            p.Depth, p.PathKey, p.OriginX, p.OriginY, p.HubSize,
            p.StartAngle, p.EndAngle, p.ParentAngle,
            p.Items, RadialLayout.Solve(request));
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

    private static int DepthOf(string pathKey) =>
        pathKey.Length == 0 ? 0 : pathKey.Count(c => c == '/') + 1;

    private static string ChildKey(string parentKey, int index) =>
        parentKey.Length == 0 ? index.ToString(CultureInfo.InvariantCulture) : $"{parentKey}/{index}";

    private double SizeForDepth(int depth) =>
        Math.Max(MinItemSize, ItemSize * Math.Pow(SizeScalePerDepth, depth));

    /// <summary>A ring queued for solving. Internal, so nothing here reaches the consumer.</summary>
    private sealed record PendingRing(
        int Depth,
        string PathKey,
        double OriginX,
        double OriginY,
        double HubSize,
        double? RadiusFloor,
        IReadOnlyList<RadialMenuItem> Items,
        double StartAngle,
        double EndAngle,
        double? ParentAngle);
}

/// <summary>One solved ring, ready to render. Internal by design — the consumer sees items and
/// parameters, never the layout state behind them.</summary>
internal sealed record RadialRing(
    int Depth,
    string PathKey,
    double OriginX,
    double OriginY,
    double HubSize,
    double ArcStart,
    double ArcEnd,
    double? ParentAngle,
    IReadOnlyList<RadialMenuItem> Items,
    RadialLayoutResult Layout);
