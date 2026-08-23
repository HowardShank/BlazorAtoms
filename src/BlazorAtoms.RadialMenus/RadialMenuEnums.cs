namespace BlazorAtoms.RadialMenus;

/// <summary>
/// How the items of one ring are spread across the arc defined by <c>StartAngle</c>/<c>EndAngle</c>.
/// </summary>
/// <remarks>
/// Prefixed per the repo-wide enum convention. An unprefixed <c>Distribution</c> would be a poor
/// neighbour to the many other spread/spacing enums already shipped (<c>ChartLegendPlacement</c>,
/// <c>BadgePlacement</c>, <c>ToolbarPlacement</c>) once a page <c>@using</c>s two packages.
/// </remarks>
public enum RadialMenuDistribution
{
    /// <summary>Full circle (sweep &gt;= 360) uses <see cref="Cyclic"/>; a partial arc uses
    /// <see cref="Endpoints"/>. The right answer in nearly every case — start here.</summary>
    Auto = 0,

    /// <summary>First item sits exactly on <c>StartAngle</c>, last exactly on <c>EndAngle</c>;
    /// step = <c>sweep / (n - 1)</c>. The natural reading of a partial arc. On a full circle the
    /// first and last item land on the same spot — use <see cref="Cyclic"/> there instead.</summary>
    Endpoints,

    /// <summary>First item on <c>StartAngle</c>, then one step per item; step = <c>sweep / n</c>,
    /// so the last item stops one step short of <c>EndAngle</c>. Correct for an arc that closes on
    /// itself: four items on a full circle land on 0/90/180/270.</summary>
    Cyclic,

    /// <summary>Every item inset half a step from both arc ends; step = <c>sweep / n</c>,
    /// item <c>k</c> at <c>Start + sweep * (k + 0.5) / n</c>. Use when nothing should touch the
    /// arc boundary — e.g. an arc butted against a panel edge.</summary>
    Padded,

    /// <summary>Consumer owns the spacing: item <c>k</c> at <c>Start + k * AngleStep</c>.
    /// <c>EndAngle</c> is ignored, and the arc is free to run past it or stop short.</summary>
    FixedStep,
}

/// <summary>Which way angles advance from <c>StartAngle</c>.</summary>
/// <remarks>
/// Prefixed per the repo-wide enum convention: <c>ScrollDirection</c>, <c>FanDirection</c>,
/// <c>CardRevealDirection</c> and <c>VerticalDirection</c> already exist in sibling packages, so a
/// bare <c>Direction</c> would be ambiguous the moment two of them are imported together.
/// </remarks>
public enum RadialMenuDirection
{
    /// <summary>Angles increase clockwise from the top, matching a compass and CSS <c>rotate()</c>.</summary>
    Clockwise = 0,

    /// <summary>Angles decrease from <c>StartAngle</c> — the arc sweeps anticlockwise.</summary>
    CounterClockwise,
}

/// <summary>How the ring radius is decided.</summary>
/// <remarks>Prefixed per the repo-wide enum convention; several packages already ship a
/// <c>*Mode</c> enum (<c>CanvasMode</c>, <c>DropMode</c>).</remarks>
public enum RadialMenuRadiusMode
{
    /// <summary>Radius is solved so neighbours cannot overlap and nothing overlaps the center
    /// button. A supplied <c>Radius</c> acts as a lower bound, never a cap.</summary>
    Auto = 0,

    /// <summary>The supplied <c>Radius</c> is used exactly. Overlap is the consumer's choice; the
    /// layout still reports it as an advisory.</summary>
    Fixed,

    /// <summary>Radius is solved as in <see cref="Auto"/> but capped to the measured host box, with
    /// <c>Overflow</c> deciding what gives when the cap bites. Needs the JS module.</summary>
    FitContainer,
}

/// <summary>What gives when the solved radius exceeds the space available.</summary>
/// <remarks>Prefixed per the repo-wide enum convention — <c>TextAreaResize</c> and
/// <c>GaugeArcStyle</c> show the same package-noun-first shape.</remarks>
public enum RadialMenuOverflow
{
    /// <summary>Let the ring grow without limit. Item count never breaks the layout; the menu just
    /// gets bigger.</summary>
    GrowRadius = 0,

    /// <summary>Wrap the surplus into concentric rings, each offset half a step from the one inside
    /// it so the items stagger rather than lining up radially.</summary>
    Rings,

    /// <summary>Hold the radius and shrink the items instead, down to <c>MinItemSize</c>.</summary>
    Shrink,

    /// <summary>Show one page of items per ring, with prev/next steppers taking a slot each.</summary>
    Paginate,

    /// <summary>Show a fixed window of items and rotate the ring to reach the rest.</summary>
    Spin,
}

/// <summary>Where a branch's children appear when it opens.</summary>
/// <remarks>Prefixed per the repo-wide enum convention; <c>CanvasMode</c> and <c>DropMode</c>
/// already exist, so a bare <c>ExpandMode</c> would read as unowned.</remarks>
public enum RadialMenuExpandMode
{
    /// <summary>Children radiate from the branch item itself, on an arc centered on the direction
    /// the branch already points. The parent ring stays put, so the whole path is visible.</summary>
    Cascade = 0,

    /// <summary>Children go on the next ring out from the same center, confined to the slice of arc
    /// their parent occupies. The most compact way to show depth.</summary>
    Concentric,

    /// <summary>Children replace the current ring in place, and the center button goes back while
    /// naming the level you are on — the ring itself shows only children, so nothing else can.
    /// Bounded footprint whatever the depth, at the cost of losing sight of the parent.</summary>
    Drill,
}

/// <summary>How a child ring's arc is derived when the item does not state one.</summary>
/// <remarks>Prefixed per the repo-wide enum convention. Deliberately not named
/// <c>*ArcStyle</c>, which <c>GaugeArcStyle</c> already uses for a visual treatment rather than a
/// layout rule.</remarks>
public enum RadialMenuArcMode
{
    /// <summary>Center a <c>ChildSweep</c>-wide arc on the parent's own direction. Fans children out
    /// away from the parent with no angles for the consumer to work out.</summary>
    AutoCenteredOnParent = 0,

    /// <summary>Reuse the menu's own <c>StartAngle</c>/<c>EndAngle</c> at every depth.</summary>
    InheritSweep,

    /// <summary>Confine children to exactly the slice of arc the parent item occupies, so sibling
    /// subtrees can never overlap each other.</summary>
    SliceOfParent,

    /// <summary>Use only per-item <c>StartAngle</c>/<c>EndAngle</c>, falling back to the menu's own
    /// arc where an item states none.</summary>
    Explicit,
}

/// <summary>Where an item's label is drawn.</summary>
/// <remarks>Prefixed per the repo-wide enum convention — a bare <c>LabelPlacement</c> is already
/// taken by another package in the family, alongside <c>BadgePlacement</c>,
/// <c>ChartLegendPlacement</c>, <c>ToolbarPlacement</c> and <c>TooltipPlacement</c>.</remarks>
public enum RadialMenuLabelPlacement
{
    /// <summary>Inside the shape, ellipsized if it does not fit. The conventional radial-menu
    /// look.</summary>
    Inside = 0,

    /// <summary>Outside the shape, further along the same radius. Decouples label length from shape
    /// size entirely — nothing about the text can force the ring to grow.</summary>
    Outside,

    /// <summary>Not drawn at all; carried as the accessible name and the <c>title</c> tooltip. The
    /// right choice for an icon-only menu.</summary>
    TooltipOnly,
}

/// <summary>What opens the menu and its branches.</summary>
/// <remarks>Prefixed per the repo-wide enum convention; <c>AnimationTrigger</c> already exists in a
/// sibling package.</remarks>
public enum RadialMenuTrigger
{
    /// <summary>Click or tap. The only trigger that works on touch.</summary>
    Click = 0,

    /// <summary>Hover, closing when the pointer leaves the menu. Falls back to click on touch
    /// devices, which have no hover.</summary>
    Hover,

    /// <summary>Always open — the center button toggles nothing and the ring is part of the page.</summary>
    Always,
}

/// <summary>Whether a connector line is drawn from each item back to the button it hangs off.</summary>
/// <remarks>
/// <para>Prefixed per the repo-wide enum convention.</para>
/// <para>At the top level that button is the center button. Deeper it is the branch that was opened,
/// whatever <see cref="RadialMenuExpandMode"/> is in play — including
/// <see cref="RadialMenuExpandMode.Concentric"/>, where the ring is centred on the menu but its items
/// still belong to an item out on the previous ring.</para>
/// </remarks>
public enum RadialMenuSpokeMode
{
    /// <summary>No spokes.</summary>
    None = 0,

    /// <summary>A line between the two button centers, passing under both shapes.</summary>
    ToCenter,

    /// <summary>A line spanning only the gap — from the edge of the shape it starts at to the edge of
    /// the item shape — so nothing is drawn under an opaque button.</summary>
    ToShapeEdge,
}

/// <summary>Outline drawn for the center button and for the items.</summary>
/// <remarks>
/// Prefixed per the repo-wide enum convention, and this is the sharpest case for it: a bare
/// <c>Shape</c> would collide with <c>AvatarShape</c>, <c>BadgeShape</c>, <c>ButtonShape</c>,
/// <c>CheckShape</c>, <c>FrameShape</c>, <c>HandleShape</c>, <c>LogoShape</c>, <c>ModuleShape</c>,
/// <c>SkeletonAvatarShape</c> and <c>TooltipShape</c> already shipped across the family.
/// </remarks>
public enum RadialMenuShape
{
    /// <summary>A true circle. The most space-efficient shape for a label — nothing else fits as
    /// much text inside a given diameter.</summary>
    Circle = 0,

    /// <summary>A rounded square (a generous <c>border-radius</c> rather than a polygon).</summary>
    Squircle,

    /// <summary>Three sides, point up. Wastes the most room of any shape — a label needs 2.83x its
    /// own diagonal in diameter to fit inside.</summary>
    Triangle,

    /// <summary>Four sides, flat top.</summary>
    Square,

    /// <summary>Four sides, point up — a square turned 45 degrees.</summary>
    Diamond,

    /// <summary>Five sides, point up.</summary>
    Pentagon,

    /// <summary>Six sides, point up. Set <c>ShapeRotation="30"</c> for the flat-top honeycomb
    /// orientation.</summary>
    Hexagon,

    /// <summary>Seven sides, point up.</summary>
    Heptagon,

    /// <summary>Eight sides, flat top — the road-sign orientation.</summary>
    Octagon,

    /// <summary>A regular polygon with <c>ShapeSides</c> sides, point up.</summary>
    Polygon,

    /// <summary>Your own SVG path, supplied via <c>CustomPath</c> and drawn in a 100x100 box.</summary>
    Custom,
}

/// <summary>Where an item's diameter comes from.</summary>
/// <remarks>Prefixed per the repo-wide enum convention; <c>ButtonSize</c>, <c>InputSize</c>,
/// <c>ProgressSize</c> and <c>TabsSize</c> already occupy the bare <c>Size</c> space.</remarks>
public enum RadialMenuSizeMode
{
    /// <summary>Every item is exactly <c>ItemSize</c> across and a label too long for the shape is
    /// ellipsized. Predictable, and the only mode whose geometry is fully known before the browser
    /// has laid anything out — so the only mode that renders identically under prerender.</summary>
    Fixed = 0,

    /// <summary>Diameter is estimated from the label length and font size, using
    /// <c>CharWidthRatio</c> as the average glyph width. No measurement, so no reflow, but a
    /// proportional font makes it an estimate rather than a fact.</summary>
    FromFont,

    /// <summary>Diameter is computed from the real measured text width. Exact, at the cost of a
    /// round trip to the browser before the ring can be sized.</summary>
    Measure,
}

/// <summary>What a computed slot on the ring actually holds.</summary>
/// <remarks>Prefixed per the repo-wide enum convention; <c>ClockKind</c> and
/// <c>HashAlgorithmKind</c> already occupy the bare <c>Kind</c> space.</remarks>
public enum RadialMenuSlotKind
{
    /// <summary>One of the consumer's items.</summary>
    Item = 0,

    /// <summary>The synthetic "previous page" stepper (<see cref="RadialMenuOverflow.Paginate"/>).</summary>
    PagePrev,

    /// <summary>The synthetic "next page" stepper (<see cref="RadialMenuOverflow.Paginate"/>).</summary>
    PageNext,
}
