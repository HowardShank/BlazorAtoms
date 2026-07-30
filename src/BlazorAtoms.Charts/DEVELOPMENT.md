# BlazorAtoms.Charts — Development Notes

Internal architecture notes for maintainers. See `README.md` for consumer-facing usage.

## Three bases, and why

```
AtomChartBase              box, series colour, animation, aria wrapper,
 │                         + Heading / Caption / Legend / EmptyState slots + LegendPlacement
 ├─ AtomGauge              one Value + Min/Max — no series at all
 └─ AtomSeriesChartBase    Values / Labels / Formatter / Min / Max + the geometry helpers
     ├─ AtomSparkline      no plot chrome, by definition
     ├─ AtomDonut          shares of a total, no axes
     └─ AtomCartesianChartBase   the seven cartesian slots + the tick model
         ├─ AtomLineChart
         └─ AtomBarChart

AtomChartElementBase       the 14 opt-in elements. Not a chart: no Values, no box, no viewBox —
                           just a cascaded ChartContext and its own CssClass/Style.
```

Each level exists because a parameter one level up would be meaningless below it: `Values` on a gauge, a
`Gridlines` slot on a donut or a sparkline. Same rule as `AtomProgressBase`/`AtomProgressValueBase`, and
the reflection tests pin it — `Gauge_takes_a_single_Value_and_no_series`,
`Sparkline_offers_no_plot_chrome_slots`, `Donut_and_gauge_offer_no_cartesian_slots`,
`The_slice_and_range_label_slots_stay_on_the_chart_that_has_them`. Those tests are the enforcement;
without them the hierarchy is just a suggestion.

The four page-furniture slots *are* on `AtomChartBase`, sparkline included, and that is deliberate: a
heading, a caption, a legend and an empty state are page furniture rather than marks on the plot, so none
of them contradicts what a sparkline is. `EmptyState` is arguably most valuable there, since an inline
sparkline with no data otherwise renders nothing at all.

`AtomDonut` is the one imperfection: it inherits `Min`/`Max` from the series base and ignores them,
because a slice is a share of a total and has no range to rescale. Splitting a fourth base for two
parameters was the worse trade, so it is documented in the class remarks and the README instead of
pretended away.

## Elements are slots, not registrations

Every piece of chrome is an opt-in child component. The mechanism is a **named `RenderFragment` slot per
element**, and the reason it is not the more obvious flat-children shape is a hard constraint:

**Blazor initializes a child component after its parent's `BuildRenderTree` completes.** A parent can never
learn about its component children during its own render pass. `PadLeft` — the value-axis gutter — has to be
decided *before* the plot is laid out, and `README.md` promises single-pass parity across static SSR,
Server, WebAssembly and prerender. There is no second pass to fix it up in.

A slot is a *parameter*, so `ValueAxis is not null` is readable before the first node is emitted. That is the
whole trick, and it is why the shape is:

```razor
<AtomLineChart Values="@data">
    <ValueAxis><AtomChartValueAxis CssClass="y" /></ValueAxis>
</AtomLineChart>
```

rather than the flatter `<AtomChartValueAxis />` directly inside the chart. Two tags per element is the price
of the SSR guarantee. Three alternatives were considered and rejected:

1. **Flat children + registration** (`OnInitialized` → `Parent.Register(this)` → `StateHasChanged`), which is
   how `AtomTabs` works. Correct for tabs, where the registration list feeds keyboard navigation and is only
   read on interaction. Wrong here: the gutter would be missing on the first render — a visible reflow when
   interactive, and permanently wrong under static SSR.
2. **Flat children + CSS `:has()`** to reserve the space without C# knowing. Workable, and it would give the
   nicer markup, but it needs every `viewBox` to become plot-only, the axes to become absolutely positioned
   overlay `<svg>`s with `overflow: visible`, and all five aspect-ratio pins to change. A much larger rewrite
   for a syntax improvement.
3. **Flat `CssClass`/`Style` parameters on the chart** (`AxisLabelCssClass`, …). No new mechanism at all, but
   it doubles the parameter surface per surface styled and gives the elements nowhere to put their own
   behaviour.

### The governing rule for where a parameter lives

> On the **chart** if the chart must know it before it renders. On the **element** otherwise.

| On the chart | Because |
| --- | --- |
| `ValueAxisWidth` | it *is* `PadLeft` — reserved before any mark is placed |
| `LegendPlacement` | picks which layout area the frame renders the legend into |
| `GridlineCount`, `NiceScale` | feed `TickStep` → `AdjustRange` → `Range`; they move the marks, not just the rules |

`AtomChartSliceLabels.MinPercent` is the instructive counter-example. It drops labels the chart already
computed, which changes no geometry — so the chart hands over *every* slice label with its share attached
(`ChartTextMark.Share`) and the element filters. That is the line: an element may decide not to draw
something, but never where something is drawn.

### The chart precomputes, the element renders

`ChartContext` is cascaded with `IsFixed="false"` and rebuilt every render — the `CardContext` pattern from
`BlazorAtoms.Cards`. The elements only ever *read*, so Blazor's own change detection re-renders them and
there is no notification loop. Every coordinate in it is already in the space the chart placed that slot
into: `AtomLineChart`'s are relative to its translated `<g>`, `AtomBarChart`'s are absolute, and neither
element knows the difference.

One fat context rather than one per element kind, so there is a single `CascadingValue` per chart; unused
lists stay empty. `ChartTextMark` covers value ticks, mark readouts, slice percentages and gauge range
labels, because all four are "text at an (x, y) with an anchor".

`AtomChartValueAxis` is the one element with two markup shapes — SVG `<text>` for a vertical axis, HTML spans
for a horizontal one. It does not choose: `ChartContext.ValueAxisInSvg` does, because the choice is forced by
which axis the labels align to (see "Two label mechanisms" below), and only the chart knows its orientation
before it renders.

## The scoped-CSS boundary, and `::deep`

Scoped CSS stamps the scope id of the component that **declares** the markup. An element rendered into a
chart's slot therefore carries the *element's* id, and the chart's stylesheet cannot reach it. Three
consequences, all load-bearing:

1. **Each element owns its own `.razor.css`.** The label, legend and readout rules that used to live in
   `AtomLineChart.razor.css` and friends moved out, and the class names moved with them
   (`.atom-line-chart-axis-label` → `.atom-chart-value-axis-label`) so one stylesheet serves all charts.
2. **Anything a chart needs to tell an element goes through a custom property**, which inherits down the DOM
   regardless of scope. `--chart-pad-left`/`--chart-pad-right` are what keep the HTML label rows inset by
   exactly the SVG's own padding. `--chart-readout-offset` moved the *other* way: `AtomChartReadout` emits it
   on its own root, which is what lets its `Offset` parameter override the chart's sweep-aware default.
3. **Layout that spans the boundary needs `::deep`.** `AtomBarChart` reorders the axis elements inside its
   plot and flips the whole thing from a column to a row for horizontal bars — layout it owns, on markup it
   does not. `::deep` moves the scope attribute onto the ancestor
   (`.atom-bar-chart-plot ::deep .atom-chart-category-axis`), which is exactly this case. Note the division:
   the chart says *where*, the element says *how it looks*.

Animation rules that have to reach an element key off the SVG's own `data-animate` rather than a chart class
— `svg[data-animate] .atom-chart-value-labels` — so one rule in the element's stylesheet serves both charts.
Keyframe names are written literally there, because a scoped stylesheet renames its own keyframes and a name
resolved through a `var()` would not be rewritten.

## `AtomChartFrame`

One layout shell, used internally by all five charts, holding the heading / body / legend / caption areas and
the `CascadingValue`. Public only because a Razor component cannot be made internal — the Razor compiler
emits `public partial class`, so an `internal` code-behind is a `CS0262`.

It exists rather than the same markup being repeated in five `.razor` files for the scoped-CSS reason above:
five copies of the area divs would need five copies of the rules positioning them, and a layout change would
have to land in all five without drifting. Each chart still owns its own root element (for `ClassAttr` /
`StyleAttr` / the attribute splat) and its own inner plot wrapper, where its chart-specific layout lives.

## Geometry in C#, fixed view units

Every component declares its own `viewBox` in user units and computes mark positions in C#; `Width` and
`Height` only size the CSS box.

The tempting alternative is a unit `viewBox` with `preserveAspectRatio="none"`, so the drawing always
fills the box exactly. It stretches non-uniformly, which turns a point marker into an ellipse and a
rounded bar corner into a lopsided one, and every stroke then needs `vector-effect: non-scaling-stroke`
to keep an even weight. Instead, the stylesheets use `aspect-ratio` matching the `viewBox` — the box fills
without letterboxing *and* scaling stays uniform. Setting `Height` overrides that and can reintroduce
letterboxing; that is the caller's decision.

### Every box is locked to its viewBox's ratio — and must stay that way

Each stylesheet sets `aspect-ratio` to match its own `viewBox`:

| component | viewBox | aspect-ratio |
| --- | --- | --- |
| `AtomSparkline` | 300×40 | `300 / 40` |
| `AtomLineChart` | 320×160 | `2 / 1` |
| `AtomBarChart` | 320×160 | `2 / 1` |
| `AtomDonut` | 100×100 | `1 / 1` |
| `AtomGauge` | 100×100 | `1 / 1` |

Not cosmetic. SVG's default `preserveAspectRatio="meet"` fits the artwork to whichever axis runs out
first, so `width: 100%` plus a **fixed height** draws the graphic narrow and centred inside its own box —
while the HTML category-label row still spans the full box width. The visible symptom is points and bars
sitting under nothing, with the value-axis gutter apparently floating in the middle of the chart.

`AtomBarChart` was written this way from the start, which is why its labels measured 0px off while
`AtomLineChart`'s were wrong the whole time — and why the bug survived a screenshot review: a letterboxed
line chart looks like a small chart, not a broken one.

`AtomSparkline` was 3.75:1, so locking it made a full-width strip ~247px tall. Its viewBox is now 300×40
(7.5:1), which reads as a sparkline at any width.

`The_viewBox_matches_the_aspect_ratio_its_stylesheet_locks` pins all five pairs. It looks like a tautology
and isn't: no layout-free test can catch the mismatch, so the numbers are pinned to force the pair to
change together.

Setting `Height` reintroduces letterboxing whenever the box stops matching the ratio. That is the caller's
call, and it is documented rather than clamped.

### Number formatting

`N()` rounds to three decimals, formats invariant, and collapses negative zero. All three matter:
a locale that writes `0,5` yields coordinates the browser silently discards (there is a culture test),
unrounded doubles bloat the markup, and `-0` appears in every zero-offset dash attribute otherwise.

## `pathLength="100"`

Donut slices, gauge arcs and the line draw-in all set `pathLength="100"` and then work in percentages:
a slice's `stroke-dasharray` length *is* its share, and the draw-in animation is a dashoffset from 100 to
0 with no knowledge of the geometry. No circumference, no π, radius-independent. Lifted from
`AtomProgressRing`, and the reason the CSS can be static while the data is not.

## Razor reserves `<text>`

`<text>` is a Razor control construct, so **an SVG `<text>` element cannot be written as markup in a
`.razor` file** — `RZ1023: "<text>" and "</text>" tags cannot contain attributes`. Every element that draws
SVG text therefore goes through `AtomChartElementBase.TextMarks`, which builds them with the render-tree
builder, each mark wrapped in `OpenRegion(i)` so the sequence numbers inside stay constant and the diff
behaves like ordinary markup. Anything else needing SVG text must do the same.

`<g>` is *not* reserved, which is what lets each of those elements put its own styleable root in markup —
`<g class="@ClassAttr("atom-chart-value-axis")" style="@StyleAttr(null)">` — with the built text inside it.
The text inherits `fill` and `font-size` from the group, so one `Style` on the element restyles every label
it drew.

**This is not optional — it is the only place a builder-emitted element's CSS class can go.** Blazor's CSS
isolation attribute (`b-xxxxxxxxxx`) is stamped onto an element only by the Razor *compiler*, at every
literal HTML/SVG tag it sees written in a `.razor` file. A `<line>` or `<text>` built via
`builder.OpenElement` in C# is invisible to that compiler pass, so it never receives the attribute — and a
scoped rule like `.atom-chart-gridline { stroke: ...; }` compiles to `.atom-chart-gridline[b-xxx] { ... }`,
which then matches nothing. The rule doesn't error; it silently never applies, and the mark renders with no
stroke/fill/dasharray at all. This shipped once (`AtomChartGridlines`, `AtomChartBaseline`,
`AtomChartValueLabels`, `AtomChartSliceLabels`, `AtomChartRangeLabels`, and the SVG half of
`AtomChartValueAxis`) and was invisible to the whole bUnit suite, because bUnit asserts on markup, not on
resolved CSS — the coordinates were all correct, only the paint was missing.

The fix, and the rule for any new builder-emitted mark: **put every visual property on the wrapping `<g>`
(or root `<div>`), never on the per-mark class.** `fill`, `stroke`, `stroke-width`, `stroke-dasharray`,
`font-size`, `font-weight`, `paint-order`, `dominant-baseline` and `pointer-events` are all inherited SVG
properties, so this reaches the children exactly as if they'd matched directly. `opacity` does **not**
inherit — group-level `opacity` still looks identical here because the marks inside never overlap each
other, so per-element and per-group compositing are visually the same. The per-mark class (`atom-chart-
gridline`, `atom-chart-value-label`, …) still belongs in the markup: tests query it for coordinates, it
just carries no styling of its own.

## Nice scaling: the step is chosen, the tick count follows

`NiceScale` picks a 1/2/5 × 10ⁿ step from the requested interval count, snaps the auto bounds outward onto
multiples of it, and then **derives the tick count from the snapped span**. That last part is the whole
design, and it cost two wrong attempts to get to:

1. *Snap the bounds, keep the requested count.* A −13…46 series snaps to −20…60, and dividing 80 by the
   requested 5 gives a tick every 16 — labels at −20, −4, 12, 28. Round bounds, off-step ticks.
2. *Use naive `floor`/`ceil`.* `0.07 / 0.01` is `7.000000000000001`, so `Math.Ceiling` returns 8 and the
   axis gains a whole extra step above the data; `0.3 / 0.1` = `2.9999999999999996` loses one below it.
   Hence `SnapDown`/`SnapUp`, which treat a value within 1e-9 of a multiple as being on it.

So `GridlineCount` is a *target* while `NiceScale` is on, and `GridlineOffsets` is driven by
`ActualIntervals` rather than the parameter — gridlines have to land under the tick labels, and a
one-position disagreement between them reads as a rendering bug rather than a rounding choice. Tests pin
both halves: the count is exact with `NiceScale="false"`, and `Gridlines_land_under_the_tick_labels`
compares the two sets of coordinates.

One more subtlety: the nice step is only used **if it divides the span**. It won't when a bound was given
explicitly, since those are never snapped — and stepping past an explicit `Max` would print a tick label
above the top of the caller's own axis.

## Two label mechanisms, chosen by axis

Category labels and the horizontal value axis are **HTML**; the vertical value axis is **SVG text**. Not
inconsistency — the two axes have different constraints:

- Aligning to fractions of the **width** works in CSS: percentage padding and `space-between` both resolve
  against the containing block's inline size, which is the SVG's width.
- Aligning to fractions of the **height** does not. Percentage padding resolves against *width* even for
  `padding-top`/`padding-bottom`, so there is no CSS expression for "6 of the viewBox's 160 vertical
  units". A first attempt at it put the horizontal bar labels 13px out — worse than the 8px it replaced.

So a vertical axis lives inside the SVG, where it shares the gridlines' coordinate system and is exact by
construction; a horizontal one lives in HTML, where it inherits the page font. The left gutter widens
(`PadLeft`) when the `ValueAxis` slot is filled, and C# emits `--chart-pad-left`/`--chart-pad-right` as
percentages so the HTML category row insets by exactly the same amount.

`AtomChartValueAxis` is therefore the one element that renders two different markup shapes, switched on
`ChartContext.ValueAxisInSvg`. The chart sets it and places the same slot in the corresponding position —
inside the `<svg>` for a vertical axis, in the plot's HTML flow for a horizontal one — so the element never
has to know the orientation, only which shape it was asked for.

## SVG text and rotated groups

Donut slice labels and gauge range labels are rendered **outside** their component's rotated `<g>`, with
absolute angles computed in C#. Text inside a rotated group inherits the rotation, so the numbers would
sit at a tilt that changes with `StartAngle`/`SweepAngle`. Tests assert the labels have no `transform`, and
that neither their own group nor its parent does either — one level deeper than before, since the element
now supplies a `<g>` of its own between the text and the SVG.

## Labels are HTML, not SVG text

Label rows sit below (or beside) the SVG as HTML. SVG text inside a scaled `viewBox` scales with the
graphic, so it ignores the reader's font-size preference and cannot wrap or ellipsis. The cost is that
labels are not part of the SVG if someone extracts it — the right way round for a component whose output
is a live page.

For horizontal bars the same label markup becomes a column via CSS, one row per bar, with equal flex
items lining up with the evenly spaced slots. That is the layout to recommend when labels are long. The
rules that do it live in `AtomBarChart.razor.css` behind `::deep`, because the markup they target belongs
to `AtomChartCategoryAxis` — see "The scoped-CSS boundary" above.

## Bugs the tests caught

Worth recording, because both were invisible without them:

- **`Series` returned null for a chart with no `Values`.** The cache guard was
  `!ReferenceEquals(_cacheSource, Values)` — and on the first read of an unset chart both are null, so
  `ReferenceEquals` is *true*, the fill was skipped, and the next line threw. `<AtomSparkline />` crashed.
  The guard now also checks `_cache is null`, and `A_null_series_behaves_exactly_like_an_empty_one` pins it.
- **Negative zero** reached the markup as `stroke-dashoffset="-0"`; `N()` now collapses it.

`No_coordinate_anywhere_is_NaN_or_Infinity` is the highest-value test in the suite: it renders every chart
against seven awkward series and greps the markup. Bad geometry does not throw — it emits `NaN`
coordinates, and the browser draws nothing at all with a clean console. That failure mode is why the
degenerate cases are handled in `Range`/`Fraction`/`XAt` rather than guarded at the call sites.

## Reduced motion resets the *from* state

Every `prefers-reduced-motion` block sets `animation: none` **and** resets what the keyframes started
from — `stroke-dashoffset: 0`, `transform: none`, `opacity: 1`. Removing only the animation leaves the
element in its start state: a fully dashed-out line, a bar scaled to zero, an invisible area. The chart
would render *empty* rather than un-animated, which is a worse outcome than the motion.
