using System.Globalization;

namespace BlazorAtoms.RadialMenus;

/// <summary>
/// One computed position on a ring: which item it holds, which ring it is on, its angle, its
/// offset in pixels from the menu center, and its resolved diameter.
/// </summary>
/// <param name="ItemIndex">Index into the consumer's item list. Meaningless (-1) for the
/// synthetic pagination steppers — check <paramref name="Kind"/> first.</param>
/// <param name="Ring">0-based ring, counting outward. Only <see cref="RadialMenuOverflow.Rings"/>
/// ever produces more than one.</param>
/// <param name="AngleDegrees">Normalized to [0, 360). 0 is straight up, increasing clockwise.</param>
/// <param name="X">Pixels right of center.</param>
/// <param name="Y">Pixels *down* from center — already negated for CSS, so a slot at angle 0
/// has a negative <paramref name="Y"/>.</param>
/// <param name="Size">Resolved diameter (bounding box) in pixels for this slot.</param>
/// <param name="Kind">A real item, or a pagination stepper.</param>
public readonly record struct RadialSlot(
    int ItemIndex,
    int Ring,
    double AngleDegrees,
    double X,
    double Y,
    double Size,
    RadialMenuSlotKind Kind);

/// <summary>
/// Everything <see cref="RadialLayout.Solve"/> needs. Deliberately a plain value type with no
/// Blazor, DOM or JS types anywhere in reach — the whole geometry of the component is a pure
/// function of this record, so it is unit-testable without a renderer.
/// </summary>
public sealed record RadialLayoutRequest
{
    /// <summary>How many items this ring must place. Zero or negative yields an empty layout.</summary>
    public int ItemCount { get; init; }

    /// <summary>Arc start in degrees. 0 is straight up.</summary>
    public double StartAngle { get; init; }

    /// <summary>Arc end in degrees. Equal to <see cref="StartAngle"/> (or a full turn away) means a
    /// complete circle.</summary>
    public double EndAngle { get; init; } = 360;

    /// <summary>How items spread across the arc.</summary>
    public RadialMenuDistribution Distribution { get; init; } = RadialMenuDistribution.Auto;

    /// <summary>Explicit degrees between items. Only read for
    /// <see cref="RadialMenuDistribution.FixedStep"/>.</summary>
    public double? AngleStep { get; init; }

    /// <summary>Which way the arc sweeps from <see cref="StartAngle"/>.</summary>
    public RadialMenuDirection Direction { get; init; } = RadialMenuDirection.Clockwise;

    /// <summary>Requested ring radius. Under <see cref="RadialMenuRadiusMode.Auto"/> this is a
    /// lower bound, not a cap.</summary>
    public double? Radius { get; init; }

    /// <summary>How <see cref="Radius"/> is interpreted.</summary>
    public RadialMenuRadiusMode RadiusMode { get; init; } = RadialMenuRadiusMode.Auto;

    /// <summary>Hard ceiling on the ring radius, usually the measured host box. When set and the
    /// solved radius exceeds it, <see cref="Overflow"/> decides what gives.</summary>
    public double? MaxRadius { get; init; }

    /// <summary>Diameter of the center button — items are kept clear of it.</summary>
    public double CenterSize { get; init; } = 64;

    /// <summary>Requested item diameter. <see cref="RadialMenuOverflow.Shrink"/> is the only policy
    /// that reduces it.</summary>
    public double ItemSize { get; init; } = 48;

    /// <summary>Floor for <see cref="RadialMenuOverflow.Shrink"/>.</summary>
    public double MinItemSize { get; init; } = 24;

    /// <summary>Clear space required between neighbouring items, and between an item and the
    /// center button.</summary>
    public double ItemGap { get; init; } = 8;

    /// <summary>Radial clear space between concentric rings
    /// (<see cref="RadialMenuOverflow.Rings"/>).</summary>
    public double RingGap { get; init; } = 16;

    /// <summary>What gives when the ring will not fit.</summary>
    public RadialMenuOverflow Overflow { get; init; } = RadialMenuOverflow.GrowRadius;

    /// <summary>Hard cap on items per ring for <see cref="RadialMenuOverflow.Rings"/>. When null the
    /// wrap point is derived from <see cref="MaxRadius"/> instead.</summary>
    public int? MaxPerRing { get; init; }

    /// <summary>Items per page for <see cref="RadialMenuOverflow.Paginate"/>. The steppers take a
    /// slot each on top of this.</summary>
    public int PageSize { get; init; } = 8;

    /// <summary>Which page <see cref="RadialMenuOverflow.Paginate"/> is showing. Clamped.</summary>
    public int PageIndex { get; init; }

    /// <summary>Size of the visible window for <see cref="RadialMenuOverflow.Spin"/>. Also fixes the
    /// item spacing, so the ring does not resize as it rotates.</summary>
    public int VisibleCount { get; init; } = 8;

    /// <summary>Current rotation in degrees for <see cref="RadialMenuOverflow.Spin"/>.</summary>
    public double SpinOffset { get; init; }

    /// <summary>Quantum the shrunk item size is rounded down to, so repeated solves cannot jitter
    /// by fractions of a pixel. Zero or negative disables quantization.</summary>
    public double SizeStep { get; init; } = 4;

    /// <summary>Slots on one ring beyond which an advisory is raised. Advisory only — the layout
    /// never refuses a count.</summary>
    public int CrowdingWarnThreshold { get; init; } = 12;
}

/// <summary>The solved geometry of one ring (or of a concentric stack, under
/// <see cref="RadialMenuOverflow.Rings"/>).</summary>
public sealed record RadialLayoutResult
{
    /// <summary>Every placed slot, in the order the items were given.</summary>
    public IReadOnlyList<RadialSlot> Slots { get; init; } = [];

    /// <summary>Radius of the innermost ring.</summary>
    public double Radius { get; init; }

    /// <summary>Radius of the outermost ring. Equals <see cref="Radius"/> unless rings wrapped.</summary>
    public double OuterRadius { get; init; }

    /// <summary>Resolved item diameter. Below the requested <c>ItemSize</c> only under
    /// <see cref="RadialMenuOverflow.Shrink"/>.</summary>
    public double ItemSize { get; init; }

    /// <summary>How many concentric rings were used.</summary>
    public int RingCount { get; init; }

    /// <summary>Half-width of the box the menu needs: <see cref="OuterRadius"/> plus half an item.
    /// The center button can still be wider — the component takes the max.</summary>
    public double Extent { get; init; }

    /// <summary>Total pages under <see cref="RadialMenuOverflow.Paginate"/>, else 1.</summary>
    public int PageCount { get; init; } = 1;

    /// <summary>The page actually rendered, after clamping.</summary>
    public int PageIndex { get; init; }

    /// <summary>Degrees of arc actually spanned, after normalization. A closed arc reports 360.</summary>
    public double Sweep { get; init; }

    /// <summary>The distribution actually used — <see cref="RadialMenuDistribution.Auto"/> resolved,
    /// or a fallback after a bad <c>AngleStep</c>.</summary>
    public RadialMenuDistribution ResolvedDistribution { get; init; }

    /// <summary>Nominal degrees between consecutive items on a ring.</summary>
    public double StepDegrees { get; init; }

    /// <summary>Smallest angular gap between any two slots on the innermost ring, wrap-around
    /// included. This — not <see cref="StepDegrees"/> — is what the radius is solved against.</summary>
    public double MinSeparationDegrees { get; init; }

    /// <summary>Human-readable notes about anything the layout had to compromise on. Never throws;
    /// the component surfaces these under its <c>Debug</c> parameter.</summary>
    public IReadOnlyList<string> Advisories { get; init; } = [];
}

/// <summary>
/// The radial menu's geometry engine: a pure function from <see cref="RadialLayoutRequest"/> to
/// <see cref="RadialLayoutResult"/>.
/// </summary>
/// <remarks>
/// <para><b>Angle convention.</b> 0 degrees is straight up and angles increase clockwise, matching a
/// compass and CSS <c>rotate()</c>. A slot's offset from center is
/// <c>x = R*sin(theta)</c>, <c>y = -R*cos(theta)</c>.</para>
/// <para><b>Item count is never capped.</b> Overlap is prevented by solving the radius instead. Two
/// neighbours <c>sep</c> degrees apart are <c>2*R*sin(sep/2)</c> pixels apart, so clearing an item
/// plus its gap needs <c>R &gt;= (size + gap) / (2*sin(sep/2))</c>; a second constraint keeps the ring
/// clear of the center button. The larger wins.</para>
/// <para><b>The separation that matters is the measured minimum, not the nominal step.</b> A
/// 350-degree arc holding two items has a nominal step of 350 but its neighbours are 10 degrees
/// apart across the wrap. So every angle is computed first, then the minimum circular gap is taken
/// over the sorted set — correct by construction for every distribution rather than by case
/// analysis.</para>
/// </remarks>
public static class RadialLayout
{
    private const double FullCircleEpsilon = 0.01;
    private const double Deg2Rad = Math.PI / 180.0;

    /// <summary>Solves one ring (or concentric stack) of the menu.</summary>
    public static RadialLayoutResult Solve(RadialLayoutRequest request)
    {
        var advisories = new List<string>();
        var n = request.ItemCount;
        if (n <= 0)
        {
            return new RadialLayoutResult
            {
                ItemSize = request.ItemSize,
                ResolvedDistribution = RadialMenuDistribution.Cyclic,
                Sweep = ResolveSweep(request.StartAngle, request.EndAngle),
            };
        }

        var sweep = ResolveSweep(request.StartAngle, request.EndAngle);

        // Paginate and Spin change WHICH items are on the ring, so they run before any geometry.
        // Both are unconditional when selected: PageSize and VisibleCount are explicit consumer
        // numbers, and honouring them only once some radius cap happened to bite would be
        // unpredictable. Rings/Shrink/GrowRadius, by contrast, only differ once a cap bites.
        return request.Overflow switch
        {
            RadialMenuOverflow.Paginate => SolvePaginated(request, sweep, advisories),
            RadialMenuOverflow.Spin => SolveSpun(request, sweep, advisories),
            RadialMenuOverflow.Rings => SolveRings(request, sweep, advisories),
            _ => SolveSingleRing(request, sweep, BuildIdentityMap(n), 1, 0, advisories),
        };
    }

    // ---- sweep + distribution -----------------------------------------------------------------

    /// <summary>
    /// Degrees of arc from start to end, always positive and always in (0, 360]. A full turn or
    /// more clamps to 360, and a zero-width arc is read as a closed circle rather than as a
    /// degenerate request — the only reading under which a menu can still be drawn.
    /// </summary>
    internal static double ResolveSweep(double start, double end)
    {
        var raw = end - start;
        if (Math.Abs(raw) >= 360) return 360;
        var s = Norm360(raw);
        return s <= FullCircleEpsilon ? 360 : s;
    }

    private static RadialMenuDistribution Resolve(
        RadialMenuDistribution requested, double sweep, double? angleStep, List<string> advisories)
    {
        if (requested == RadialMenuDistribution.Auto)
            return IsClosed(sweep) ? RadialMenuDistribution.Cyclic : RadialMenuDistribution.Endpoints;

        if (requested == RadialMenuDistribution.FixedStep && !(angleStep > 0))
        {
            advisories.Add("Distribution=FixedStep needs a positive AngleStep; fell back to Auto.");
            return IsClosed(sweep) ? RadialMenuDistribution.Cyclic : RadialMenuDistribution.Endpoints;
        }

        return requested;
    }

    private static bool IsClosed(double sweep) => sweep >= 360 - FullCircleEpsilon;

    /// <summary>
    /// Angular offsets from <c>StartAngle</c> for <paramref name="count"/> slots, plus the nominal
    /// step between them. Offsets are unsigned — the caller applies sweep direction.
    /// </summary>
    internal static (double[] Offsets, double Step) Offsets(
        int count, double sweep, RadialMenuDistribution dist, double? angleStep)
    {
        if (count <= 0) return ([], 0);

        // A lone item has no neighbour to space against, so the only question is where on the arc
        // it sits. Cyclic and FixedStep both mean "start at StartAngle"; the other two centre it.
        if (count == 1)
        {
            var solo = dist is RadialMenuDistribution.Cyclic or RadialMenuDistribution.FixedStep
                ? 0
                : sweep / 2;
            return ([solo], sweep);
        }

        var step = dist switch
        {
            RadialMenuDistribution.Endpoints => sweep / (count - 1),
            RadialMenuDistribution.FixedStep => angleStep!.Value,
            _ => sweep / count, // Cyclic and Padded
        };

        var offsets = new double[count];
        var bias = dist == RadialMenuDistribution.Padded ? 0.5 : 0.0;
        for (var k = 0; k < count; k++) offsets[k] = (k + bias) * step;
        return (offsets, step);
    }

    // ---- radius -------------------------------------------------------------------------------

    /// <summary>
    /// Smallest radius at which two slots <paramref name="sepDeg"/> degrees apart clear each other.
    /// From the chord length <c>2*R*sin(sep/2)</c>. Returns 0 when there is no neighbour to clear.
    /// </summary>
    internal static double NeighborRadius(double size, double gap, double sepDeg)
    {
        if (sepDeg >= 360 - FullCircleEpsilon || sepDeg <= 0) return 0;
        var sin = Math.Sin(sepDeg / 2 * Deg2Rad);
        return sin <= 1e-9 ? 0 : (size + gap) / (2 * sin);
    }

    /// <summary>Smallest radius at which the ring clears the center button.</summary>
    internal static double HubRadius(double centerSize, double size, double gap)
        => (centerSize + size) / 2 + gap;

    /// <summary>
    /// Smallest angular gap between any two of <paramref name="angles"/>, measured around the
    /// circle so the wrap from the last back to the first counts. 360 for fewer than two angles.
    /// </summary>
    internal static double MinSeparation(IReadOnlyList<double> angles)
    {
        if (angles.Count < 2) return 360;
        var sorted = angles.Select(Norm360).OrderBy(a => a).ToArray();
        var min = 360 - sorted[^1] + sorted[0];
        for (var i = 1; i < sorted.Length; i++) min = Math.Min(min, sorted[i] - sorted[i - 1]);
        return min;
    }

    // ---- single ring --------------------------------------------------------------------------

    private static RadialLayoutResult SolveSingleRing(
        RadialLayoutRequest r,
        double sweep,
        IReadOnlyList<(int ItemIndex, RadialMenuSlotKind Kind)> map,
        int pageCount,
        int pageIndex,
        List<string> advisories)
    {
        var count = map.Count;
        var dist = Resolve(r.Distribution, sweep, r.AngleStep, advisories);
        var (offsets, step) = Offsets(count, sweep, dist, r.AngleStep);

        var sign = r.Direction == RadialMenuDirection.Clockwise ? 1 : -1;
        var angles = new double[count];
        for (var k = 0; k < count; k++) angles[k] = Norm360(r.StartAngle + sign * offsets[k]);

        var sep = MinSeparation(angles);
        if (sep < FullCircleEpsilon && count > 1)
        {
            // Two slots land on the same spot — Endpoints on a closed arc is the usual cause. The
            // true separation is 0, which would demand an infinite radius, so the radius is solved
            // against the nominal step instead and the overlap is reported rather than "fixed".
            advisories.Add(dist == RadialMenuDistribution.Endpoints && IsClosed(sweep)
                ? "Distribution=Endpoints on a closed arc puts the first and last item in the same place; use Cyclic."
                : "Two or more items resolve to the same angle; radius was solved against the nominal step.");
            sep = step > 0 ? Math.Min(step, 180) : 360;
        }

        if (count > r.CrowdingWarnThreshold)
            advisories.Add(Invariant($"{count} items on one ring exceeds CrowdingWarnThreshold={r.CrowdingWarnThreshold}; the ring will be large."));

        var size = r.ItemSize;
        var auto = Math.Max(NeighborRadius(size, r.ItemGap, sep), HubRadius(r.CenterSize, size, r.ItemGap));
        var radius = ApplyRadiusMode(r, auto, advisories);

        // Only Shrink and Rings react to a cap; Rings is a separate entry point, so Shrink is the
        // one policy handled here.
        if (r.Overflow == RadialMenuOverflow.Shrink && r.MaxRadius is double cap && auto > cap)
        {
            radius = cap;
            var fits = 2 * cap * Math.Sin(sep / 2 * Deg2Rad) - r.ItemGap;
            size = Quantize(Math.Clamp(fits, r.MinItemSize, r.ItemSize), r.SizeStep, r.MinItemSize);
            var hub = HubRadius(r.CenterSize, size, r.ItemGap);
            if (cap < hub)
                advisories.Add(Invariant($"MaxRadius={cap:0.#} is below the {hub:0.#} needed to clear the center button even at MinItemSize={r.MinItemSize:0.#}; items overlap the center."));
            else if (fits < r.MinItemSize)
                advisories.Add(Invariant($"Shrink hit MinItemSize={r.MinItemSize:0.#} and items still overlap; raise MaxRadius, narrow the arc, or switch Overflow to Rings."));
        }
        else if (r.MaxRadius is double c2 && radius > c2 && r.Overflow == RadialMenuOverflow.GrowRadius)
        {
            advisories.Add(Invariant($"Radius {radius:0.#} exceeds MaxRadius={c2:0.#}; Overflow=GrowRadius lets it. Use Rings, Shrink, Paginate or Spin to stay inside."));
        }

        var slots = new RadialSlot[count];
        for (var k = 0; k < count; k++)
            slots[k] = MakeSlot(map[k], 0, angles[k], radius, size);

        return new RadialLayoutResult
        {
            Slots = slots,
            Radius = radius,
            OuterRadius = radius,
            ItemSize = size,
            RingCount = 1,
            Extent = radius + size / 2,
            PageCount = pageCount,
            PageIndex = pageIndex,
            Sweep = sweep,
            ResolvedDistribution = dist,
            StepDegrees = step,
            MinSeparationDegrees = sep,
            Advisories = advisories,
        };
    }

    private static double ApplyRadiusMode(RadialLayoutRequest r, double auto, List<string> advisories)
    {
        switch (r.RadiusMode)
        {
            case RadialMenuRadiusMode.Fixed:
                var fixedR = r.Radius ?? auto;
                if (fixedR < auto - 0.01)
                    advisories.Add(Invariant($"RadiusMode=Fixed at {fixedR:0.#} is below the {auto:0.#} needed to keep items clear; they will overlap."));
                return fixedR;

            // Auto and FitContainer both treat a supplied Radius as a floor. FitContainer differs
            // only in where MaxRadius comes from (measured, not declared), and the cap is applied
            // by the Overflow policy rather than here.
            default:
                return Math.Max(r.Radius ?? 0, auto);
        }
    }

    // ---- rings --------------------------------------------------------------------------------

    private static RadialLayoutResult SolveRings(RadialLayoutRequest r, double sweep, List<string> advisories)
    {
        var n = r.ItemCount;
        var dist = Resolve(r.Distribution, sweep, r.AngleStep, advisories);
        var size = r.ItemSize;

        var perRing = r.MaxPerRing is int cap and > 0
            ? Math.Min(cap, n)
            : DerivePerRing(r, sweep, dist, size, advisories);

        if (perRing >= n)
        {
            // Everything fits on one ring — nothing to wrap, so take the ordinary path and keep
            // its advisories rather than reimplementing them.
            return SolveSingleRing(r, sweep, BuildIdentityMap(n), 1, 0, advisories);
        }

        var (offsets, step) = Offsets(perRing, sweep, dist, r.AngleStep);
        var sign = r.Direction == RadialMenuDirection.Clockwise ? 1 : -1;

        var baseAngles = offsets.Select(o => Norm360(r.StartAngle + sign * o)).ToArray();
        var sep = MinSeparation(baseAngles);
        if (sep < FullCircleEpsilon && perRing > 1) sep = step > 0 ? Math.Min(step, 180) : 360;

        var inner = Math.Max(
            Math.Max(r.Radius ?? 0, NeighborRadius(size, r.ItemGap, sep)),
            HubRadius(r.CenterSize, size, r.ItemGap));

        var ringCount = (int)Math.Ceiling(n / (double)perRing);
        var slots = new RadialSlot[n];
        for (var i = 0; i < n; i++)
        {
            var ring = i / perRing;
            var withinRing = i % perRing;
            // Odd rings are nudged half a step so items stagger between rings instead of lining up
            // radially — a solid block of aligned items reads as one thick spoke, not as a grid.
            var stagger = ring % 2 == 1 ? sign * step / 2 : 0;
            var angle = Norm360(baseAngles[withinRing] + stagger);
            var radius = inner + ring * (size + r.RingGap);
            slots[i] = MakeSlot((i, RadialMenuSlotKind.Item), ring, angle, radius, size);
        }

        var outer = inner + (ringCount - 1) * (size + r.RingGap);
        if (r.MaxRadius is double max && outer > max)
            advisories.Add(Invariant($"{ringCount} rings reach {outer:0.#}, past MaxRadius={max:0.#}. Lower MaxPerRing, or combine Rings with a narrower ItemSize."));

        return new RadialLayoutResult
        {
            Slots = slots,
            Radius = inner,
            OuterRadius = outer,
            ItemSize = size,
            RingCount = ringCount,
            Extent = outer + size / 2,
            PageCount = 1,
            PageIndex = 0,
            Sweep = sweep,
            ResolvedDistribution = dist,
            StepDegrees = step,
            MinSeparationDegrees = sep,
            Advisories = advisories,
        };
    }

    /// <summary>
    /// Largest number of items that fits on one ring inside <c>MaxRadius</c>. Searched from the
    /// full count downward so the answer is the loosest wrap that still fits.
    /// </summary>
    private static int DerivePerRing(
        RadialLayoutRequest r, double sweep, RadialMenuDistribution dist, double size, List<string> advisories)
    {
        if (r.MaxRadius is not double cap)
        {
            advisories.Add("Overflow=Rings needs MaxPerRing or MaxRadius to know where to wrap; behaved as GrowRadius.");
            return r.ItemCount;
        }

        var hub = HubRadius(r.CenterSize, size, r.ItemGap);
        for (var k = r.ItemCount; k >= 2; k--)
        {
            var (offsets, step) = Offsets(k, sweep, dist, r.AngleStep);
            var sep = MinSeparation(offsets.Select(Norm360).ToArray());
            if (sep < FullCircleEpsilon) sep = step > 0 ? Math.Min(step, 180) : 360;
            var need = Math.Max(NeighborRadius(size, r.ItemGap, sep), hub);
            if (need <= cap) return k;
        }

        if (hub > cap)
            advisories.Add(Invariant($"MaxRadius={cap:0.#} cannot clear the center button ({hub:0.#} needed); every ring will overlap it."));
        return 1;
    }

    // ---- paginate -----------------------------------------------------------------------------

    private static RadialLayoutResult SolvePaginated(RadialLayoutRequest r, double sweep, List<string> advisories)
    {
        var n = r.ItemCount;
        var pageSize = Math.Max(1, r.PageSize);
        var pageCount = (int)Math.Ceiling(n / (double)pageSize);
        var pageIndex = Math.Clamp(r.PageIndex, 0, pageCount - 1);

        var first = pageIndex * pageSize;
        var last = Math.Min(n, first + pageSize);
        var hasPrev = pageIndex > 0;
        var hasNext = pageIndex < pageCount - 1;

        var map = new List<(int, RadialMenuSlotKind)>(last - first + 2);
        if (hasPrev) map.Add((-1, RadialMenuSlotKind.PagePrev));
        for (var i = first; i < last; i++) map.Add((i, RadialMenuSlotKind.Item));
        if (hasNext) map.Add((-1, RadialMenuSlotKind.PageNext));

        // Handed to the single-ring solver as its own item count, so radius, crowding advisories
        // and distribution all apply to what is actually on screen — steppers included.
        var forRing = r with { ItemCount = map.Count, Overflow = RadialMenuOverflow.GrowRadius };
        return SolveSingleRing(forRing, sweep, map, pageCount, pageIndex, advisories);
    }

    // ---- spin ---------------------------------------------------------------------------------

    private static RadialLayoutResult SolveSpun(RadialLayoutRequest r, double sweep, List<string> advisories)
    {
        var n = r.ItemCount;
        var visible = Math.Clamp(r.VisibleCount, 1, n);
        var dist = Resolve(r.Distribution, sweep, r.AngleStep, advisories);

        // Spacing is fixed by the WINDOW size, not by the item count, so the ring keeps its radius
        // and its item pitch while it rotates. Nothing resizes mid-spin.
        var (windowOffsets, step) = Offsets(visible, sweep, dist, r.AngleStep);
        var sep = MinSeparation(windowOffsets.Select(Norm360).ToArray());
        if (sep < FullCircleEpsilon && visible > 1) sep = step > 0 ? Math.Min(step, 180) : 360;

        var size = r.ItemSize;
        var auto = Math.Max(NeighborRadius(size, r.ItemGap, sep), HubRadius(r.CenterSize, size, r.ItemGap));
        var radius = ApplyRadiusMode(r, auto, advisories);

        var sign = r.Direction == RadialMenuDirection.Clockwise ? 1 : -1;
        var baseOffset = windowOffsets.Length > 0 ? windowOffsets[0] : 0;
        var track = n * step; // full length of the item belt; wrapping this makes the dial endless
        var slots = new List<RadialSlot>(visible + 1);

        // A closed arc is exclusive at its far end: an item at exactly `sweep` would land on the
        // same angle as one at 0 and the two would stack. An open arc includes its end.
        var limit = IsClosed(sweep) ? sweep - 1e-9 : sweep + 1e-9;

        for (var i = 0; i < n; i++)
        {
            // Position along an endless belt n*step long. Wrapping the belt (not the arc) is what
            // lets the dial rotate forever and bring culled items back around.
            var pos = baseOffset + Mod(i * step + r.SpinOffset, track);
            if (pos > limit) continue;
            var angle = Norm360(r.StartAngle + sign * pos);
            slots.Add(MakeSlot((i, RadialMenuSlotKind.Item), 0, angle, radius, size));
        }

        if (visible < n)
            advisories.Add(Invariant($"Overflow=Spin shows {visible} of {n} items; the rest are reachable only by rotating."));

        return new RadialLayoutResult
        {
            Slots = slots,
            Radius = radius,
            OuterRadius = radius,
            ItemSize = size,
            RingCount = 1,
            Extent = radius + size / 2,
            PageCount = 1,
            PageIndex = 0,
            Sweep = sweep,
            ResolvedDistribution = dist,
            StepDegrees = step,
            MinSeparationDegrees = sep,
            Advisories = advisories,
        };
    }

    // ---- helpers ------------------------------------------------------------------------------

    private static RadialSlot MakeSlot(
        (int ItemIndex, RadialMenuSlotKind Kind) entry, int ring, double angle, double radius, double size)
    {
        var rad = angle * Deg2Rad;
        return new RadialSlot(
            entry.ItemIndex,
            ring,
            angle,
            radius * Math.Sin(rad),
            -radius * Math.Cos(rad),
            size,
            entry.Kind);
    }

    private static (int, RadialMenuSlotKind)[] BuildIdentityMap(int n)
    {
        var map = new (int, RadialMenuSlotKind)[n];
        for (var i = 0; i < n; i++) map[i] = (i, RadialMenuSlotKind.Item);
        return map;
    }

    /// <summary>Rounds down to a multiple of <paramref name="stepPx"/>, never below
    /// <paramref name="floor"/>. Keeps repeated solves from jittering by sub-pixel amounts.</summary>
    internal static double Quantize(double value, double stepPx, double floor)
    {
        if (stepPx <= 0) return value;
        var q = Math.Floor(value / stepPx) * stepPx;
        return Math.Max(q, floor);
    }

    /// <summary>Normalizes an angle into [0, 360).</summary>
    internal static double Norm360(double a)
    {
        a %= 360;
        return a < 0 ? a + 360 : a;
    }

    private static double Mod(double a, double m)
    {
        if (m <= 0) return 0;
        a %= m;
        return a < 0 ? a + m : a;
    }

    /// <summary>Formats an advisory with invariant culture, so a locale using a decimal comma
    /// cannot change the numbers a consumer sees in the debug overlay.</summary>
    private static string Invariant(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
}
