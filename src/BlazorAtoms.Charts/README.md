# BlazorAtoms.Charts

Five small data visuals for Blazor, drawn as inline SVG. One series of `double`s in, a crisp vector
graphic out.

- **Zero dependencies, zero JavaScript.** Identical under static SSR, Server, WebAssembly and prerender.
- **Every piece of chrome is an opt-in child component** with its own `CssClass`, `Style` and stylesheet.
- **Hover values with no interop.** Every mark carries an SVG `<title>`, so the tooltip is the browser's.
- **Themeable** through `--chart-*` custom properties, defaulting to `currentColor`.
- **`prefers-reduced-motion` aware** — the draw-in animation is skipped, and charts render complete.

## Install

```bash
dotnet add package BlazorAtoms.Charts
```

```razor
@using BlazorAtoms.Charts
```

## The five

```razor
@code { double[] data = [12d, 19, 7, 23, 15, 28]; }

@* bare trend line, for a table cell or beside a number *@
<AtomSparkline Values="@data" />

@* the same data with chrome — each piece opted into by filling its slot *@
<AtomLineChart Values="@data" Labels="@(new[]{"Jan","Feb","Mar","Apr","May","Jun"})"
               ShowArea="true" Smooth="true">
    <ValueAxis><AtomChartValueAxis /></ValueAxis>
    <CategoryAxis><AtomChartCategoryAxis /></CategoryAxis>
    <Gridlines><AtomChartGridlines /></Gridlines>
</AtomLineChart>

@* comparison; zero-based, because a bar's length is its value *@
<AtomBarChart Values="@data" Orientation="ChartOrientation.Horizontal" Radius="3" />

@* parts of a whole *@
<AtomDonut Values="@(new[]{45d, 30, 25})" Labels="@(new[]{"Direct","Search","Social"})">
    <Legend><AtomChartLegend /></Legend>
    <Center><AtomChartCenter><strong>100</strong></AtomChartCenter></Center>
</AtomDonut>

@* one value on a dial *@
<AtomGauge Value="72" Bands="@bands">
    <Readout><AtomChartReadout /></Readout>
    <RangeLabels><AtomChartRangeLabels /></RangeLabels>
</AtomGauge>
@code { GaugeBand[] bands = [new(60, "#59a14f"), new(85, "#edc948"), new(100, "#e15759")]; }
```

## The chrome is opt-in

A chart with no slots filled draws only its marks. Every label, rule, legend and readout is a child
component you place in the matching slot — and because it is a real component it carries the usual
`CssClass` and `Style`, so you can restyle any single piece without reaching for `!important` or
guessing at internal class names:

```razor
<AtomBarChart Values="@data" Labels="@months">
    <Heading><AtomChartHeading Subtitle="FY25">Revenue</AtomChartHeading></Heading>
    <ValueAxis><AtomChartValueAxis Style="fill:#8894a6; font-size:8px" /></ValueAxis>
    <CategoryAxis><AtomChartCategoryAxis CssClass="my-x-labels" /></CategoryAxis>
    <ValueLabels><AtomChartValueLabels Style="fill:#4e79a7; font-weight:600" /></ValueLabels>
    <Caption><AtomChartCaption>Excludes intercompany.</AtomChartCaption></Caption>
</AtomBarChart>
```

| Slot | Element | On |
| --- | --- | --- |
| `Heading` | `AtomChartHeading` (`Subtitle`) | all five |
| `Caption` | `AtomChartCaption` | all five |
| `Legend` | `AtomChartLegend` (`Columns`, `ShowValues`, `ShowPercent`) | all five |
| `EmptyState` | `AtomChartEmptyState` (`Text`) | all five |
| `ValueAxis` | `AtomChartValueAxis` | line, bar |
| `CategoryAxis` | `AtomChartCategoryAxis` (`Wrap`) | line, bar |
| `ValueAxisTitle` / `CategoryAxisTitle` | `AtomChartAxisTitle` | line, bar |
| `ValueLabels` | `AtomChartValueLabels` | line, bar |
| `Gridlines` | `AtomChartGridlines` (`Dashed`) | line, bar |
| `Baseline` | `AtomChartBaseline` | line, bar |
| `Center` | `AtomChartCenter` | donut, gauge |
| `SliceLabels` | `AtomChartSliceLabels` (`MinPercent`) | donut |
| `Readout` | `AtomChartReadout` (`Offset`) | gauge |
| `RangeLabels` | `AtomChartRangeLabels` | gauge |

Two things follow from that shape and are worth knowing up front.

**A `CssClass` from a page with scoped CSS will not match.** Scoped CSS stamps the scope of the component
that *declares* the markup, and the element declares its own root — so the class has to be global, or use
`Style`, or set a `--chart-*` custom property on an ancestor.

**Nothing enforces which element goes in which slot.** Put an `AtomChartValueAxis` in `Heading` and it
emits an SVG group into an HTML div, where it draws nothing. The table above is the pairing.

### What stays on the chart, and why

> A parameter lives **on the chart** if the chart must know it before it renders. Everything else lives
> **on the element**.

That is not a style preference. Blazor initializes a child component *after* its parent's render pass
finishes, so a chart can never read a child's parameters while laying itself out — and the value-axis
gutter has to be reserved before the first mark is placed. Filling a *slot* is readable in time, because a
slot is a parameter; a child's `Width` is not.

So `ValueAxisWidth` (the gutter), `LegendPlacement` (which layout area), and `GridlineCount`/`NiceScale`
(the tick model, which moves the marks and not just the rules) are chart parameters. Colours, fonts, dash
patterns, `MinPercent`, `Columns` and the readout `Offset` are element parameters.

## Shared parameters

On all five:

| Parameter | Type | Default | |
| --- | --- | --- | --- |
| `Width` / `Height` | `string?` | `100%` / from ratio | CSS lengths for the box — see below |
| `SeriesColor` | `string?` | `currentColor` | the data marks |
| `Animate` | `bool` | `true` | draw-in on first render |
| `Duration` | `string?` | `700ms` | draw-in length |
| `AriaLabel` | `string?` | generated | see [Accessibility](#accessibility) |
| `Visible` | `bool` | `true` | `false` = `display:none` |
| `LegendPlacement` | `ChartLegendPlacement?` | per chart | `End` on `AtomDonut`, `Below` elsewhere |

On the four that plot a series (everything but `AtomGauge`):

| Parameter | Type | |
| --- | --- | --- |
| `Values` | `IEnumerable<double>?` | the data; null or empty draws no marks |
| `Labels` | `IEnumerable<string>?` | positional, any length — see below |
| `Formatter` | `Func<double, string>?` | formats titles and readouts |
| `Min` / `Max` | `double?` | plotted range; defaults to the data's own |

On the two with axes (`AtomLineChart`, `AtomBarChart`): `GridlineCount` (`4`), `NiceScale` (`true`),
`ValueAxisWidth` (`30`), `AxisColor` — plus the seven element slots listed above.

### The value axis

Fill the `ValueAxis` slot to label the value (Y) axis — every gridline plus both bounds. `Labels` is the
*category* axis, and `CategoryAxis` is its own slot, so the two are independent:

```razor
<AtomBarChart Values="@data" Labels="@months">
    <ValueAxis><AtomChartValueAxis /></ValueAxis>
    <CategoryAxis><AtomChartCategoryAxis /></CategoryAxis>
    <Gridlines><AtomChartGridlines /></Gridlines>
</AtomBarChart>
```

`NiceScale` (on by default) rounds the auto-derived range outward to a 1/2/5 × 10ⁿ step so the ticks are
whole numbers — a 0–28 series plots to 0–30 and ticks 0/10/20/30. **The consequence: `GridlineCount`
becomes a target rather than a promise**, because the tick count has to follow from the step. Fixing the
count instead would put ticks between step multiples (a −13…46 series would label −20, −4, 12, 28).

Turn `NiceScale` off for exactly `GridlineCount` lines, at whatever values divide the raw range. An
explicit `Min`/`Max` is never rounded either way — and when it doesn't divide by the nice step, the axis
falls back to even division so no tick can print above your own `Max`.

Filling the slot widens the plot's left gutter by `ValueAxisWidth` (30 view units by default, about four
digits at the label's own size). A `Formatter` emitting currency or thousands separators will overflow
that; widen it rather than accepting the clipping.

### Per component

**`AtomSparkline`** — `Fill`, `ShowLastPoint` (`true`), `Smooth`, `StrokeWidth`, `AreaColor`,
`AreaOpacity`. No plot-chrome slots: a sparkline with gridlines isn't a sparkline. It keeps the four
page-furniture slots, since a heading is not a mark on the plot.

**`AtomLineChart`** — `ShowPoints` (`true`), `ShowArea`, `Smooth`, `StrokeWidth`, `AreaColor`,
`AreaOpacity`.

**`AtomBarChart`** — `Orientation`, `BarGap` (`0.25`), `Radius`.

**`AtomDonut`** — `Thickness` (`18`), `StartAngle`, `PadAngle` (`0.5`), `Palette`, `TrackColor`.

**`AtomGauge`** — `Value`, `Min` (`0`), `Max` (`100`), `SweepAngle` (`240`), `Thickness` (`12`), `Bands`,
`ShowNeedle` (`true`), `ShowValueArc`, `Formatter`, `TrackColor`, `NeedleColor`.

### Labels without hovering

`<title>` tooltips need a pointer, and **touch devices have no hover** — so on a phone an unlabelled donut
is a ring of colours with no key. `AtomChartLegend` fixes that with an HTML list (swatch, label, value,
percentage) that inherits the page font and wraps:

```razor
<AtomDonut Values="@data" Labels="@sources">
    <Legend><AtomChartLegend /></Legend>
    <SliceLabels><AtomChartSliceLabels /></SliceLabels>
</AtomDonut>
```

It works on all four series charts, not just the donut. `ShowPercent` defaults to *auto*: percentages
appear only where the values sum to something, so a donut shows them and a line chart doesn't rather than
printing a column of `0%`.

`AtomChartSliceLabels` additionally prints percentages on the ring. Slices thinner than `MinPercent` are
skipped — a threshold rather than collision detection, because measuring rendered text is precisely what
SVG can't do without JavaScript. Skipped slices keep their tooltip and their legend row.

For the gauge, `AtomChartRangeLabels` prints `Min` and `Max` at the ends of the arc so the dial states its
own scale, and `AtomChartReadout` prints the value. The readout sits just below centre on a partial dial
(`Offset` to override): the needle pivots at the centre and its hub is drawn there, so a centred readout
renders underneath it. `AtomChartCenter` is the separate slot for arbitrary content in the hole, so a
label and the value can now both be shown.

### Empty results look empty, not broken

An empty or null series draws a correctly sized, entirely blank box. Fill `EmptyState` and it says so:

```razor
<AtomLineChart Values="@rows">
    <EmptyState><AtomChartEmptyState Text="No sales in this period" /></EmptyState>
</AtomLineChart>
```

It renders *over* the plot area rather than in place of it, so nothing shifts when the data arrives. Each
chart decides what "empty" means for it: no values for the series charts, no drawable total for
`AtomDonut` (all-zero or all-negative), and no range at all for `AtomGauge` (`Min` ≥ `Max`).

## Sizing

Set `Width`; the height follows from each chart's own aspect ratio — 7.5:1 for `AtomSparkline`, 2:1 for
`AtomLineChart` and `AtomBarChart`, square for `AtomDonut` and `AtomGauge`.

`Height` is available but it is the one parameter that can make a chart look wrong. SVG scales uniformly,
so once the box stops matching that ratio the graphic is drawn to fit the shorter axis and centred, with
empty space either side — while the HTML label row still spans the full width, leaving the labels no
longer under the marks they name. Prefer sizing by `Width`, or by the container:

```razor
<div style="width: 24rem">
    <AtomLineChart Values="@data" Labels="@months" />
</div>
```

## Things that would otherwise surprise you

**Bars are zero-based; lines are not.** A bar encodes its value as a length, so the axis must start at
zero — otherwise the smallest bar is always zero-height. A line chart's message is the *shape* of the
change, which zero-basing can flatten. Set `Min` explicitly to override either.

**Awkward data is handled, not thrown on.** Empty, single-point and dead-flat series are ordinary query
results: an empty series draws no marks, a single point plots at the middle, and a flat series plots at a
constant height instead of dividing by zero. Negative values work on bars and lines; on a donut they are
dropped, since a negative share of a whole is meaningless.

**`Labels` is advisory.** Read positionally, never length-checked. Shorter, longer or absent is fine — a
mark with no label falls back to its formatted value in the tooltip.

**`AtomDonut` ignores `Min`/`Max`.** A slice's size is its share of the total, so there is no range to
rescale. They are inherited along with the data; this is the one place in the library where an inherited
parameter does nothing, and it is stated rather than hidden. Use `AtomGauge` for a value within a range.

**`AtomGauge` is not `AtomMeter`.** `BlazorAtoms.Progress`'s `AtomMeter` implements the HTML `<meter>`
semantics with `role="meter"`. A gauge is a graphic — arbitrary bands, a needle, a sweep angle,
`role="img"`. Likewise `AtomDonut` is not `AtomProgressRing`: many slices versus one fraction of a task.

## Theming

The parameters write `--chart-*` properties, so a rule on an ancestor themes every chart beneath it:

```css
.dashboard {
    --chart-series-color: #4e79a7;
    --chart-axis-color: #8894a6;
    --chart-duration: 400ms;
}
```

Available: `--chart-width`, `--chart-height`, `--chart-series-color`, `--chart-axis-color`,
`--chart-area-color`, `--chart-area-opacity`, `--chart-stroke-width`, `--chart-track-color`,
`--chart-needle-color`, `--chart-duration`.

Custom properties are also how a chart reaches *inside* an element, since scoped CSS cannot cross that
boundary: `--chart-pad-left`/`--chart-pad-right` are what keep the HTML label rows inset by exactly the
SVG's own padding, and `--chart-readout-offset` positions the gauge readout. Those are written by the
components and are documented in `DEVELOPMENT.md` rather than being part of the theming surface.

## Accessibility

Each chart is `role="img"` with an `aria-label`. Unnamed, it generates one describing the data — e.g.
*"bar chart of 6 values from 7 to 28"* — because a collection of `<rect>`s conveys nothing on its own. Set
`AriaLabel` to say something better. Individual marks carry `<title>`, which assistive tech reads and the
browser shows on hover.

For a chart whose exact numbers matter, put a table nearby — a graphic can be described but not
tabulated, and no `aria-label` substitutes for the values.
