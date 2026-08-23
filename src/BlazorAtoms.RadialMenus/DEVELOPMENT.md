# BlazorAtoms.RadialMenus — Development Notes

Internal notes for maintainers. See `README.md` for consumer-facing usage.

Design tier **B** (opt-in JS) per `src/LIBRARY-CATALOG.md`, with one deviation noted under
*Always-on module* below.

## Shape of the package

```
RadialLayout.cs           pure geometry: RadialLayoutRequest -> RadialLayoutResult
RadialShapeGeometry.cs    pure geometry: n-gon vertices, inradius, text-fit sizing
RadialMenuItem.cs         consumer data model
RadialMenuEnums.cs        every public enum, package-noun prefixed
AtomRadialMenu.razor          flat markup — no recursion, no second component
AtomRadialMenu.razor.cs       parameters + ring construction
AtomRadialMenu.Interaction.cs interaction, focus, interop, style plumbing
AtomRadialMenu.razor.css      what the custom properties mean; computes nothing
wwwroot/atom-radialmenus.js   the three things only the browser knows
```

The split into two `.cs` partials is for file size only — one class, and the markup reads members
from both halves. Everything else follows the repo's usual `.razor` / `.razor.cs` / `.razor.css`
layout.

## Geometry is a pure function, and that is the point

`RadialLayout.Solve` takes a record and returns a record. No Blazor types, no DOM, no JS, no
component state. Every angle, radius, overflow policy and size decision is therefore unit-testable
without a renderer, and `RadialLayoutTests` asserts against numbers computed by hand from the
documented formulas rather than against the implementation's own output.

The component's whole job is to walk the open branches, call `Solve` once per ring, and turn the
result into `--radialmenu-*` custom properties. If a position is wrong, the bug is in one of two
files and neither of them can be reached from the browser.

### The separation that matters is measured, not nominal

A 350&deg; arc holding three items has a nominal step of 175&deg;, but the first and last are
**10&deg;** apart across the wrap. Solving the radius against 175 gives 28px; the truth is 321px.

So `Solve` computes every angle first, then takes the minimum circular gap over the sorted set,
wrap-around included. Correct by construction for every distribution rather than by case analysis —
`FixedStep` running past 360&deg;, `Endpoints` on a closed arc, and a `Spin` window all fall out of
the same code. `Radius_is_solved_against_the_measured_wrap_gap_not_the_nominal_step` pins it.

Note that a 2-item case cannot distinguish the two, because `sin((360-s)/2) = sin(s/2)`. The
regression test uses three items for that reason.

### The one collision a pure per-ring solve cannot see

`Solve` is given one ring, so the strongest thing it can prove is *within* that ring: its items clear
each other, and they clear their hub. It cannot prove anything about a ring it was never shown.

Two overlaps therefore live outside it, both real and both visible on screen:

1. **Sibling subtrees under `Cascade`.** Each branch is solved independently from its own item as the
   hub. Nothing tells branch A's subtree that branch B's exists.
2. **A deep `Cascade` ring folding back over the center button.** At depth &ge; 1 the hub *is* the
   parent item, so the hub-clearance term in that solve is about the parent, not about the center
   button 100px away. A child arc pointing back down its own spoke walks straight into it.

`DetectCrossRingCollisions` in `AtomRadialMenu.razor.cs` closes that gap, after `BuildRings`, where
every slot's **absolute** position (`ring.OriginX + slot.X`) is known. It compares every pair of
placed items plus a synthetic entry for the center button, skipping pairs from the same ring —
same-ring is `Solve`'s job and it already accounts for the wrap gap and for multi-ring staggering, so
re-checking would double-report every finding.

It runs only under `Debug`: it is quadratic in the rendered slot count and its only product is an
advisory string. Worst deficit first, three named, the rest counted, then one line naming the levers.

`Debug_reports_an_overlap_no_single_ring_solve_could_have_seen` pins case 2 with hand-computable
numbers (root item at 64px, child aimed back at 180&deg; landing 8px from a center button needing
64px). `Concentric_reports_no_cross_ring_overlap_however_deep_the_tree_runs` pins the other
direction: the mode that cannot overlap must not be told that it did.

### Coincident angles

If two slots resolve to the same angle the true separation is 0, which demands an infinite radius.
`Solve` raises an advisory and falls back to the nominal step, so the menu renders with a reported
overlap rather than flying off screen. The usual cause is `Endpoints` on a closed arc, and the
advisory names `Cyclic` as the fix.

### Why `Cyclic` exists

`Padded` insets every item half a step from both ends, which is right for an arc butted against
something. On a *closed* arc it puts four items at 45/135/225/315 — collision-free but surprising.
`Cyclic` (`Start + k·sweep/n`) puts them on 0/90/180/270, which is what a full circle should look
like, and its wrap gap equals its step so it cannot collide either. `Auto` picks `Cyclic` for a
closed arc and `Endpoints` otherwise.

## The sizing formula

A label has to fit inside the polygon's **inscribed** circle, not its bounding box:

```
inradius   r = (S/2)·cos(180/n)
fit w×h inside radius r   ⇔   r ≥ √(w²+h²)/2
                          ⇒   S = √(w²+h²) / cos(180/n)
```

Which is why the same label needs 1.41&times; its diagonal in a circle, 1.63&times; in a hexagon and
2.83&times; in a triangle. Low-sided shapes are mostly unusable corner. `RadialShapeGeometryTests`
pins each factor.

`Fixed` is the default because it is the only mode whose geometry is fully known before the browser
lays anything out — so it is the only mode that renders identically under prerender. `FromFont` is an
honest estimate (`EstimateTextWidth_cannot_tell_wide_glyphs_from_narrow_ones` documents the
limitation that justifies `Measure` existing at all). `Measure` holds the ring `visibility: hidden`
until the batched measure call returns, so the estimate is computed but never seen.

Ring sizes are quantized down to `SizeStep`, so a container resize cannot make items jitter by
fractions of a pixel.

## Rendering decisions that are not obvious

### One flat list of rings, not a recursive component

The Razor SDK emits `public partial class` for every `.razor`, so a "node renderer" child component
would leak onto the consumer's surface. Instead `BuildRings` walks the open branches iteratively and
flattens them into `List<RadialRing>` (internal), and the markup is two plain `for` loops. No
recursion in markup, no second component, and all the tree-walking stays testable C#.

`MaxDepth = 16` guards against a `RadialMenuItem` that appears among its own descendants — easy to
build by accident from a cache, and otherwise an infinite loop.
`An_item_graph_that_contains_itself_stops_rather_than_recursing_forever` pins it.

### True depth and visible depth are different numbers

`MaxVisibleDepth` re-roots the walk: `VisibleRootPath` picks the ancestor `window - 1` levels above
the deepest open path, and `BuildRings` seeds the stack from *its* children instead of from `Items`.

Rings therefore carry **two** depths. `Depth` is the real nesting level and feeds `data-depth`, which
means `data-path` and `data-depth` still address the real tree however the frame is rooted.
`VisibleDepth` is the level within the frame, and it is what everything the viewer can judge by eye
uses: `RingItemSize`, the Concentric radius floor, and the `isFrameRoot` test that decides which ring
owns the `Radius` floor and the page/spin state. Get that backwards and a re-rooted ring renders at
`ItemSize · SizeScalePerDepth^8` at the base radius — a ring of dots, shrunk by a depth nothing on
screen shows.

`BuildDrillRing` seeds `VisibleDepth = 0` for the same reason, which **changed** Drill: a drilled ring
used to shrink by its true depth and lose its page state. Nothing pinned the old behaviour, and the
new one is what the mode was for.

Two consequences worth knowing:

- The window follows the *single* deepest open path, so with `SingleBranchOpen = false` a branch open
  outside that path is not rendered at all. `BuildRings` raises an advisory saying so rather than
  silently dropping it.
- Re-rooting and going back both change how many rings exist, so `_focusKey` can point past the end of
  `_rings`. `DropFocusIfItLeftTheFrame` clears it — otherwise the roving tabindex hands its single `0`
  to a button that is not rendered and nothing in the menu is tabbable.

Why the parameter exists at all is geometry, not taste: slice containment divides the arc by the
branching factor per level, and `R ≥ (S + gap) / (2·sin(arc/2))` then doubles per level. `InheritSweep`
buys a linear radius by abandoning containment, which puts a deep item nowhere near its parent.
Shrinking items cannot keep up because `MinItemSize` floors them. Capping the levels caps the
narrowing, and that is the only lever that fixes the cause.
`The_window_is_what_keeps_a_deep_concentric_radius_bounded` pins both sides of it.

### A spoke connects two buttons, not a ring to its own centre

`SpokeStyle` originally drew from `ring.OriginX/Y` along `slot.AngleDegrees` for `Hypot(slot.X, slot.Y)`
— three values already to hand, and correct under `Cascade` (where the ring's origin *is* the parent
button) and under `Drill` (one ring, origin at the center button). Under `Concentric` it was wrong in
all three terms at once: the ring's origin is the menu centre, so every nested spoke ran from the
center button outward, producing a fan of lines converging on the hub instead of on the item that was
clicked.

Rings therefore carry a `SpokeAnchor` — the point, diameter and is-it-the-center-button of the *button*
their items hang off, separate from the ring's origin. `SpokeStyle` derives the angle from the two
endpoints with `Atan2(dx, -dy)` (arguments swapped and `y` negated, to land in the component's
up-is-zero clockwise convention), and `ToShapeEdge` trims the start against `CenterShape` or
`ItemShape` depending on which button it left.

Note the angle now arrives on the -180..180 branch where the layout's own angles are normalised to
0..360. `rotate()` cannot tell them apart, so `Cascade_spokes_still_run_along_the_slots_own_direction`
compares modulo 360 rather than pinning the representation.

### Drill's center button carries the level name

`Drill` renders exactly one ring, and that ring holds *children* — so nothing on screen says which
branch you are inside. The center button is the only place it can go, which is why it renders the
back chevron **and** `DrillItem`'s icon and label, and why the chevron drops to 26% width to make
room (the hamburger it replaces sits in the button alone at 45%).

`DrillItem` resolves the deepest entry in `_openPaths` back to an item by walking the index segments.
It returns null on any malformed or out-of-range segment rather than throwing: the path is internal
state, but `Items` can change under it between renders.

`CenterAccessibleName` has to say the same thing the button shows, action first — `"Back from Shape"`,
not `"Shape"`. `Drill_center_names_the_level_you_are_in` pins both the label and the accessible name,
including that the top level has no level to name.

### One button element for items *and* pagination steppers

Two sibling `<button>`s in an `if`/`else` cannot be used. When a keyed, `@ref`-bearing element has to
be swapped for the other branch at the same position — exactly what happens when a "next" stepper
becomes a "prev" one on page change — Blazor's
`RenderTreeDiffBuilder.RemoveOldFrame` throws `NotImplementedException: Unexpected frame type during
RemoveOldFrame: ElementReferenceCapture`.

Keeping the element identical and varying only attribute values and child content avoids that diff
path entirely; child frames carry no reference capture. The `Slot*` helpers in
`AtomRadialMenu.Interaction.cs` exist to answer every attribute for either case.
`Paginate_renders_steppers_and_they_change_the_page` is the regression test — it failed with that
exact exception before the elements were unified.

### The entrance animation must never touch `transform`

The emerge animation lives on an inner `.atom-radial-menu-body` wrapper and only ever animates
opacity and scale. The button's own `transform` is purely positional.

The first cut had the animation on the button itself, with the resting position written into the `to`
keyframe. That makes an item's **position** depend on the animation having advanced: in a throttled
background tab, a non-compositing view, or anywhere animations are deferred, `animation-fill-mode:
both` holds the `from` frame and every item sits stacked on the center at 40% scale. Verified in the
browser — all six items reported radius 0 and 19px. Geometry must not be reachable from animation
state.

### `StyleVars` needs pre-rounded numbers

`StyleVars`'s `double` overload formats with no format string, so `cos(90°)`'s `6.1e-17` residue
reaches the stylesheet as `-3.9E-15px` — not a CSS length, and dropped silently. Every pixel value
goes through `Snap()` first, which also collapses negative zero.
`A_trigonometric_residue_never_reaches_the_stylesheet_as_scientific_notation` pins it.

### Shapes are SVG, not `clip-path`

`clip-path` is cheaper DOM but clips the border *and* the focus ring, so both would need faking. An
inline `<polygon vector-effect="non-scaling-stroke">` gives a real stroke at a constant screen width
regardless of item size, and leaves the button's own focus ring intact. Points are computed in a
fixed 100&times;100 box so they depend only on side count and rotation, never on pixel size — a ring
that shrinks reuses the same path.

Points are computed per call rather than cached: the cost is a few trig calls per item, and a cache
keyed on a consumer-supplied (possibly animated) `ShapeRotation` would grow without bound.

### Scoped CSS constraints

Keyframes names are written **literally** in `animation`, never through a `var()` — Blazor's CSS
isolation rewrites a literal name with the scope suffix but leaves one hidden inside a custom
property alone, pointing at a keyframes block that no longer exists, silently. Verify with
`grep -E "animation:|@keyframes" obj/**/scopedcss/*.rz.scp.css` — both must carry the same
`-b-xxxx`.

The shape SVG is a `@<...>` template in the `.razor`'s `@code` block rather than a `MarkupString`,
because template content gets the scope attribute and `MarkupString` content never does.

## Always-on module

The module is imported on first render regardless of which features are enabled, rather than lazily
per feature. This is a deliberate deviation from Tier B's "opt-in JS" — chosen for one code path
instead of a JS-free fallback branch to maintain alongside it.

It costs disposal discipline. `DisposeAsync` must, in order: `detach`, dispose the
`DotNetObjectReference`, dispose the `IJSObjectReference`, each separately timed and separately
guarded, swallowing `JSDisconnectedException` (a fast route-away tears the circuit down first). A
skipped `detach` leaves the document listener and the `ResizeObserver` attached, firing into a
reference that is already gone. `Disposing_survives_a_circuit_that_has_already_gone_away` pins it,
including idempotency.

The JS side supplies only what the browser alone knows: the container's real box
(`RadiusMode.FitContainer`), real text widths (`SizeMode.Measure`), and outside pointer-down
(`CloseOnOutsideClick`). Everything else — including wheel-driven `Spin` — is ordinary Blazor.

The alternative to the outside-click listener is a full-viewport backdrop element, which works but
swallows pointer events over the rest of the page and so breaks `Trigger="Hover"`.

`measure` reads the font back off the live element with `getComputedStyle` rather than rebuilding it
from parameters, so an inherited family, weight or stretch is accounted for. A width measured in the
wrong family is worse than an honest estimate.

## `Open`, bound or not

`OnParametersSet` adopts the `Open` parameter only when it differs from the last value *the component
saw* — not from its own state. That is what lets the same component work bound with `@bind-Open` and
unbound with internal state, without a parameter write fighting a click.

## Verification

- `RadialLayoutTests` — geometry, no renderer. Hand-computed expectations.
- `RadialShapeGeometryTests` — vertices, inradius, sizing factors.
- `AtomRadialMenuTests` — bUnit, with the module set up **explicitly** rather than
  `JSRuntimeMode.Loose`, so the interop contract is asserted rather than absorbed.

Mutation-checked seams (break, confirm red, revert): the `-R·cos(θ)` sign in `MakeSlot`, the
wrap-gap term in `MinSeparation`, and the `cos(180/n)` inradius term. Each is caught by the test
written for it — the last one by eight tests.

Browser-verified in the Server demo at `/playground/radial-menu`: 24 items land with closest
neighbour centres at exactly `ItemSize + ItemGap` and a radius matching
`(48+8)/(2·sin 7.5°) = 214.5`; cascade children radiate from the parent at
`parentAngle ± ChildSweep/2` with the hub clearance `(48+43.2)/2 + 8`; a 270&deg;&rarr;90&deg; arc
places `Endpoints` on 270/315/0/45/90; outside labels sit exactly `ItemSize/2 + LabelOffset` beyond
the shape.
