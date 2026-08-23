# BlazorAtoms.RadialMenus

Radial (pie / wheel) menu for Blazor. Ships **`AtomRadialMenu`** — a center button whose items
radiate outward on an arc you define. **0&deg; is straight up and angles increase clockwise**, like a
compass. Any item can be a leaf action or a branch that opens a ring of its own, to any depth.

Two things make radial menus awkward, and both are handled rather than left to you:

- **Item count is never capped.** The ring radius is *solved* from collision geometry, so adding
  items grows the ring instead of overlapping it. When space is bounded, an `Overflow` policy decides
  what gives.
- **Labels have to fit inside the shape, not its bounding box.** A hexagon needs 1.63&times; a
  label's diagonal in diameter; a triangle needs 2.83&times;. The sizing modes solve for the shape's
  inscribed circle, so text does not spill out of a corner.

Shapes are real SVG polygons, so borders and focus rings work normally. Container-fit, real text
measurement and outside-click come from a small JS module the component imports itself — no
`<script>` tag, no DI registration, no setup.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.RadialMenus\BlazorAtoms.RadialMenus.csproj" />
```
```razor
@using BlazorAtoms.RadialMenus
```

## AtomRadialMenu

```razor
@* The whole thing: a full circle of leaves. Nothing else needed. *@
<AtomRadialMenu Items="@items" OnItemInvoked="@Run" />

@* A quarter arc opening up and to the right, hexagons, labels outside the shapes *@
<AtomRadialMenu Items="@items"
                StartAngle="0" EndAngle="90"
                ItemShape="RadialMenuShape.Hexagon"
                LabelPlacement="RadialMenuLabelPlacement.Outside"
                OnItemInvoked="@Run" />

@* Icon-only, always open, with connector spokes back to the button each item hangs off *@
<AtomRadialMenu Items="@items"
                Trigger="RadialMenuTrigger.Always"
                LabelPlacement="RadialMenuLabelPlacement.TooltipOnly"
                SpokeMode="RadialMenuSpokeMode.ToShapeEdge"
                OnItemInvoked="@Run" />

@* Deep tree in a bounded box: children replace the ring, the center goes back and names the level *@
<AtomRadialMenu Items="@tree"
                ExpandMode="RadialMenuExpandMode.Drill"
                OnItemInvoked="@Run" />
```

```csharp
private readonly RadialMenuItem[] items =
[
    new() { Label = "Cut",   Icon = "bi bi-scissors" },
    new() { Label = "Copy",  Icon = "bi bi-copy" },
    new() { Label = "Paste", Icon = "bi bi-clipboard", Disabled = true },
    new()
    {
        Label = "Share",
        Children =
        [
            new() { Label = "Link" },
            new() { Label = "Email" },
        ],
    },
];

private void Run(RadialMenuItem item) => Console.WriteLine(item.Label);
```

### Items

`RadialMenuItem` is a plain init-only class — no library base type, no interface to implement.

| Member | Purpose |
|---|---|
| `Label` | Text on (or beside) the shape, and the accessible name |
| `Icon` | CSS class for an icon-font glyph. For arbitrary markup use `ItemTemplate` |
| `Tooltip` | `title` text; falls back to `Label` |
| `Disabled` | Rendered but not interactive, and skipped by keyboard navigation |
| `Children` | Non-empty makes it a branch |
| `Data` | Your payload — a command, a route, a domain object. Never inspected |
| `StartAngle` / `EndAngle` | Pin this item's own child arc, overriding `ArcMode` |
| `Shape` | Override `ItemShape` for this one item |
| `CssClass` | Extra class on this item's button |

### The arc

`StartAngle` and `EndAngle` bound the arc; equal values (or a full turn apart) mean a complete
circle. An arc may wrap through 0 — `StartAngle="300" EndAngle="60"` is a 120&deg; arc across the top.

`Distribution` decides how items spread across it:

| Mode | Item *k* at | When |
|---|---|---|
| `Auto` **(default)** | closed arc &rarr; `Cyclic`, partial arc &rarr; `Endpoints` | right nearly always |
| `Endpoints` | `Start + k·sweep/(n-1)` | first item on `StartAngle`, last on `EndAngle` |
| `Cyclic` | `Start + k·sweep/n` | an arc that closes; four items land on 0/90/180/270 |
| `Padded` | `Start + sweep·(k+0.5)/n` | nothing should touch the arc boundary |
| `FixedStep` | `Start + k·AngleStep` | you own the spacing; `EndAngle` is ignored |

`Direction="RadialMenuDirection.CounterClockwise"` sweeps the other way.

### Radius, and why you rarely set it

By default the radius is solved so that neighbours cannot overlap and nothing overlaps the center
button. Two items `sep` degrees apart are `2·R·sin(sep/2)` pixels apart, so clearing an item plus its
gap needs `R ≥ (ItemSize + ItemGap) / (2·sin(sep/2))`; a second constraint clears the hub. The larger
wins.

A `Radius` you supply is a **floor**, not a cap — the solve can still push the ring further out.
`RadiusMode` changes that:

- `Auto` **(default)** — solved, with `Radius` as a floor.
- `Fixed` — your `Radius` exactly. Overlap is your call.
- `FitContainer` — solved, then capped to the measured host box. Needs the JS module.

### Overflow

What gives when the ring will not fit inside `MaxRadius` (or the measured box):

| Policy | Behaviour |
|---|---|
| `GrowRadius` **(default)** | grow anyway; the menu just gets bigger |
| `Rings` | wrap the surplus into concentric rings, staggered half a step. Set `MaxPerRing`, or let it derive |
| `Shrink` | hold the radius, shrink items down to `MinItemSize` |
| `Paginate` | one page of `PageSize` items per ring, with prev/next steppers |
| `Spin` | a `VisibleCount` window; scroll the wheel over the menu to rotate |

`Paginate` and `Spin` always apply once selected — they change *which* items are on the ring, so
honouring them only when some cap happened to bite would be unpredictable. The other three differ
only when a cap bites.

### Nesting

You should not have to work out angles for nested rings, and by default you do not.

| `ExpandMode` | Behaviour | Footprint | Can rings overlap? |
|---|---|---|---|
| `Cascade` **(default)** | children radiate from the branch item, on an arc centred on the direction it already points | grows fast | **yes**, past two levels |
| `Concentric` | children go on the next ring out from the same center, confined to the parent's slice | one radius per level | no, by construction |
| `Drill` | children replace the ring; the center button goes back and names the level you are on | the same at any depth | no, one ring is on screen |

That last column is the thing to know before you nest deeply. `Cascade` reads best, because a child
ring visibly belongs to the item it came from — but each branch is solved on its own, so two sibling
subtrees know nothing about each other and a deep tree can put one on top of the other. `Concentric`
cannot overlap: a child ring is floored a whole `RingGap` outside its parent's radius and confined to
the parent's slice, which is also why its slices get thin fast. `Drill` shows one ring at a time, so
the footprint never changes — it is the mode for a genuinely deep tree, and the center button carries
the current level's name so you can still tell where you are.

Levers when `Cascade` gets too big: a narrower `ChildSweep`, a lower `SizeScalePerDepth`, or
`LabelPlacement="TooltipOnly"` to stop labels driving the item size. `Debug` names the overlaps
outright — see below.

### Deep trees: `MaxVisibleDepth`

`Concentric` runs off screen for a reason that no arc mode can fix. Keeping a child inside its
parent's slice means each level's arc is the parent's divided by the branching factor, and equal-size
items on a narrowing arc need `R ≥ (ItemSize + ItemGap) / (2·sin(arc/2))` — so **the radius roughly
doubles per level** once that term overtakes the `RingGap` floor. With a 180° root arc and three-way
branching the crossover is level 3, and the radii run 64, 126, 183, **329**, 604.

`ArcMode="InheritSweep"` keeps the radius linear but gives every level the full arc, so a deep item
sits nowhere near its parent — it trades the symptom for a worse one. Shrinking items cannot keep up
either: `MinItemSize` floors them long before the arc stops halving.

`MaxVisibleDepth` caps the number of levels on screen, so the arc can only narrow that many times and
the radius is bounded by construction:

```razor
<AtomRadialMenu Items="@items"
                ExpandMode="RadialMenuExpandMode.Concentric"
                MaxVisibleDepth="2" />
```

The menu **re-roots**. Open a branch deeper than the window and the ancestor that falls out of view
becomes the center button, which names it and goes back — the same centre behaviour as `Drill`, which
is this idea with a window of 1. At the default sizes **3 is about the largest window whose radius is
still set by `RingGap` rather than by item collision**; 2 is comfortable.

`data-depth` and `data-path` keep reporting the item's true depth and address. Only sizing, radius and
the `Overflow` state are measured from the visible frame, so a re-rooted ring renders at full size
instead of shrinking by a depth the viewer can no longer see.

`ArcMode` picks how a child arc is derived — `AutoCenteredOnParent` (default, a `ChildSweep`-wide
fan), `InheritSweep`, `SliceOfParent`, or `Explicit`. A per-item `StartAngle`/`EndAngle` always wins.

`SingleBranchOpen` (default `true`) keeps one path open at a time.

### Shapes

`Circle` (default), `Squircle`, `Triangle`, `Square`, `Diamond`, `Pentagon`, `Hexagon`, `Heptagon`,
`Octagon`, `Polygon` (with `ShapeSides`), or `Custom` (with `CustomPath`, an SVG path `d` drawn in a
100&times;100 box).

Named shapes carry the rotation their name implies — `Square` is flat-top, `Diamond` is point-up,
`Octagon` is road-sign. `ShapeRotation` adds to that: `ShapeRotation="30"` turns a hexagon flat-top.

### Sizing

| `SizeMode` | Diameter from |
|---|---|
| `Fixed` **(default)** | `ItemSize`, with long labels ellipsized. The only mode fully known before layout, so the only one that prerenders identically |
| `FromFont` | estimated from label length &times; `FontSize` &times; `CharWidthRatio`. No round trip, but a proportional font makes it an estimate |
| `Measure` | the browser's real text width. Exact, at the cost of one batched measure call before the ring shows |

Every item on a ring gets the same diameter — the largest label would set the spacing anyway, and
mixed sizes read as an accident. `SizeScalePerDepth` (default `0.9`) shrinks deeper rings.

**`LabelPlacement="RadialMenuLabelPlacement.Outside"` sidesteps sizing entirely:** the label hangs
beyond the shape, so its length can never force the shape to grow. `TooltipOnly` does the same for an
icon-only menu.

### Keyboard

| Key | Action |
|---|---|
| <kbd>Enter</kbd> / <kbd>Space</kbd> | activate the focused button |
| <kbd>&larr;</kbd> / <kbd>&rarr;</kbd> | previous / next around the current ring, skipping disabled items |
| <kbd>&darr;</kbd> | open the focused branch and move into it |
| <kbd>&uarr;</kbd> | back to the branch this ring belongs to, or the center |
| <kbd>Home</kbd> / <kbd>End</kbd> | first / last item on the ring |
| <kbd>Esc</kbd> | close the current ring, then the menu |

A roving `tabindex` means exactly one button in the menu is tabbable, so <kbd>Tab</kbd> moves past
the menu rather than through every item. Set `KeyboardNavigation="false"` to opt out.

### Styling

Colours and metrics are CSS custom properties, settable as parameters or overridden in your own
stylesheet:

`--radialmenu-color` `-bg` `-border` `-border-width` `-center-color` `-center-bg` `-hover-bg`
`-active-bg` `-label-color` `-spoke-color` `-spoke-width` `-center-size` `-item-size` `-font-size`
`-line-height` `-label-offset` `-duration` `-easing` `-disabled-opacity`

The matching parameters are `ItemColor`, `ItemBackground`, `ItemBorderColor`, `BorderWidth`,
`CenterColor`, `CenterBackground`, `HoverBackground`, `ActiveBackground`, `LabelColor`, `SpokeColor`,
`SpokeWidth`, `AnimationDuration`, `StaggerDelay`, `Easing`, `DisabledOpacity`. `CssClass`, `Style`
and any unmatched attribute go on the root element, as on every Atom component.

`--radialmenu-extent` reports half the box the open menu wants, if you need to reserve room for it —
the ring itself deliberately overflows, the way a menu pops over its surroundings.

`prefers-reduced-motion: reduce` drops the entrance animation automatically.

### Debugging an arc

`Debug="true"` draws the arc bounds, a tick and an angle/radius-and-path tag per item, the ring
circles, and lists anything the layout had to compromise on. Advisories are never exceptions; the
menu always renders. Leave `Debug` off in production.

There are two kinds of advisory, from two different places:

- **Within one ring** — a crowded ring, a `Fixed` radius that overlaps, a `Shrink` that hit
  `MinItemSize`, `Endpoints` on a closed arc, a `MaxRadius` that cannot clear the center button.
  These come from the layout solve, which only ever sees one ring at a time.
- **Between rings** — items from two *different* rings that landed on top of each other, named by
  `data-path` with how far apart they are and how far apart they needed to be. Nothing in a per-ring
  solve can see these: under `Cascade` a child ring's hub is its parent item, so that solve does not
  know the center button is there, let alone another branch's subtree. This check runs only under
  `Debug`, and it is the one that catches a deep `Cascade` tree folding back over itself.

A cross-ring advisory is a real overlap on screen, not a warning about one. If you get them, the
guidance line that follows names the levers — or switch `ExpandMode` per the table above.

### Events

`OnItemInvoked` (leaf activated), `OnBranchOpened`, `OnBranchClosed`, and `Open` / `OpenChanged` for
`@bind-Open`.

## Render modes

Works in every render mode. Under static SSR or prerender the default `SizeMode="Fixed"` produces the
final geometry server-side, so there is no reflow when interactivity starts. `SizeMode="Measure"`,
`RadiusMode="FitContainer"` and `CloseOnOutsideClick` need a browser and come online once the
component is interactive.
