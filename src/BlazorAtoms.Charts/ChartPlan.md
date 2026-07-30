# Chart elements as opt-in child components

## Context

`BlazorAtoms.Charts` renders every piece of chrome itself, gated by boolean parameters on the chart:
`ShowValueAxis`, `ShowGridlines`, `ShowBaseline`, `ShowValues`, `ShowLegend`, `ShowSliceValues`,
`ShowRange`, `ShowValue`. Each piece's appearance is fixed by the chart's own scoped CSS, so a
consumer can restyle the chart root (`CssClass`/`Style` from `AtomComponentBase`) but cannot touch
the axis labels, the legend rows, or the value readouts. There is also no title, no caption, and no
empty-state anywhere in the package — a chart with `Values="null"` renders a silently blank box.

The fix: every standard chart element becomes its own opt-in child component with its own
`CssClass`/`Style` and its own scoped stylesheet. The boolean parameters are **replaced**, not
supplemented — one way to opt in, no precedence rules to document or pin.

### Why named slots and not flat children

Blazor initializes a child component *after* the parent's `BuildRenderTree` completes, so a parent can
never learn about its component children during its own render pass. `PadLeft` (the value-axis gutter)
has to be decided *before* the plot is drawn, and `README.md` claims single-pass parity across static
SSR / Server / WASM / prerender — there is no second pass to fix it up in.

A named `RenderFragment` slot is a *parameter*, so `slot is not null` is readable before the first node
is emitted. That makes opt-in single-pass-safe with no geometry rewrite, no `:has()` CSS, and no
`StateHasChanged` reflow. The cost is two tags per element, which is accepted.

```razor
<AtomLineChart Values="@data" Labels="@months">
    <Heading><AtomChartHeading Subtitle="FY25">Revenue</AtomChartHeading></Heading>
    <ValueAxis><AtomChartValueAxis CssClass="y-axis" Style="opacity:.5" /></ValueAxis>
    <CategoryAxis><AtomChartCategoryAxis /></CategoryAxis>
    <Gridlines><AtomChartGridlines Dashed="true" /></Gridlines>
    <Legend><AtomChartLegend Columns="2" /></Legend>
</AtomLineChart>
```

## The governing rule

> A parameter lives **on the chart** if the chart must know it before it renders — space reservation,
> layout areas, the tick model. Everything else lives **on the element**.

Applied:

| Stays on the chart | Why |
| --- | --- |
| `GridlineCount`, `NiceScale` | feed `TickStep`/`ActualIntervals`/`AdjustRange`, i.e. `Range` itself — see `AtomCartesianChartBase.cs:118-175` |
| `ValueAxisWidth` (new, default 30) | *is* `PadLeft`; the gutter must be reserved before the plot is laid out |
| `LegendPlacement` (new, enum Below/End) | picks which layout area the legend renders into |
| `Values`, `Labels`, `Min`, `Max`, `Formatter` | data |
| `ShowPoints`, `ShowArea`, `Fill`, `ShowLastPoint`, `ShowNeedle`, `ShowValueArc` | series marks, not chrome — explicitly out of scope |

Everything else — colours, fonts, dash patterns, `MinPercent`, `Columns`, readout `Offset` — is an
element parameter.

## Architecture

### Elements are pure presentation; the chart precomputes all geometry

The chart cascades a DTO rebuilt every render (the `CardContext` pattern —
`src/BlazorAtoms.Cards/CardContext.cs`, `IsFixed="false"`, so children re-render automatically with no
`NotifyChildren` dance). Every element's coordinates are computed by the chart's existing base classes
and handed over ready to draw. Elements own class, style and markup shape; they own no math.

New file `src/BlazorAtoms.Charts/ChartContext.cs`:

```csharp
public readonly record struct ChartTextMark(string Text, double X, double Y, string Anchor);
public readonly record struct ChartLine(double X1, double Y1, double X2, double Y2);
public readonly record struct ChartLegendEntry(string? Color, string? Label, double Value, double Share);
public readonly record struct ChartPlot(double PadLeft, double PadTop, double Width, double Height,
                                        double ViewWidth, double ViewHeight);

public sealed class ChartContext
{
    public bool HasData { get; init; }
    public ChartPlot Plot { get; init; }
    public Func<double, string> Format { get; init; } = v => v.ToString();
    public ChartOrientation Orientation { get; init; }

    /// <summary>True when the value axis renders as SVG text in the gutter, false when it renders as
    /// an HTML row (horizontal bars) — see DEVELOPMENT.md "Two label mechanisms, chosen by axis".</summary>
    public bool ValueAxisInSvg { get; init; }

    public IReadOnlyList<ChartTextMark> ValueTicks { get; init; } = [];
    public IReadOnlyList<ChartTextMark> MarkLabels { get; init; } = [];
    public IReadOnlyList<ChartTextMark> SliceLabels { get; init; } = [];
    public IReadOnlyList<ChartTextMark> RangeLabels { get; init; } = [];
    public IReadOnlyList<string?> CategoryLabels { get; init; } = [];
    public IReadOnlyList<ChartLine> Gridlines { get; init; } = [];
    public ChartLine? Baseline { get; init; }
    public IReadOnlyList<ChartLegendEntry> Legend { get; init; } = [];
    public double ReadoutOffset { get; init; }
}
```

One fat context rather than one per element: a single `CascadingValue` per chart, and unused lists stay
empty. `ChartTextMark` deliberately covers value ticks, mark readouts, donut slice labels and gauge
range labels — all four are "text at an (x, y) with an anchor", and the existing per-chart trig stays
exactly where it is.

### `AtomChartElementBase`

New file `src/BlazorAtoms.Charts/AtomChartElementBase.cs` — `AtomComponentBase` plus
`[CascadingParameter] protected ChartContext? Chart { get; set; }`, and the shared SVG-text builder
(Razor reserves `<text>`, so every SVG element component must use `RenderTreeBuilder` — see
`DEVELOPMENT.md` "Razor reserves `<text>`"):

```csharp
protected RenderFragment TextMarks(IReadOnlyList<ChartTextMark> marks, string cssClass) => builder =>
{
    for (var i = 0; i < marks.Count; i++)
    {
        builder.OpenRegion(i);
        builder.OpenElement(0, "text");
        builder.AddAttribute(1, "class", cssClass);
        builder.AddAttribute(2, "x", N(marks[i].X));
        builder.AddAttribute(3, "y", N(marks[i].Y));
        builder.AddAttribute(4, "text-anchor", marks[i].Anchor);
        builder.AddContent(5, marks[i].Text);
        builder.CloseElement();
        builder.CloseRegion();
    }
};
```

Each SVG element wraps those in a `<g>` it can style — `<g>` is *not* a Razor control construct, so the
root goes in markup with `ClassAttr`/`StyleAttr` as everywhere else in the repo:

```razor
@inherits AtomChartElementBase
<g class="@ClassAttr("atom-chart-value-axis")" style="@StyleAttr(null)">
    @TextMarks(Chart?.ValueTicks ?? [], "atom-chart-value-axis-label")
</g>
```

Elements render standalone-safe: no context (used outside a chart) means empty lists, so nothing draws
rather than throwing — same convention as `AtomCardSectionBase` outside an `AtomCard`.

### Scoped CSS moves with the element

Scoped CSS stamps the *declaring* component's scope id, so an element rendered inside a chart slot
carries the element's id, not the chart's. Consequence: each element needs its own `.razor.css`, and the
rules currently in `AtomLineChart.razor.css` / `AtomBarChart.razor.css` / `AtomDonut.razor.css` /
`AtomGauge.razor.css` for label text, legend rows and readouts **move** there. Cross-boundary styling
continues to go through `--chart-*` custom properties, which inherit through the DOM regardless of
scope — the existing `--chart-pad-left` / `--chart-pad-right` / `--chart-readout-offset` mechanism
already works this way and becomes load-bearing.

Class names are renamed to element-owned ones (`.atom-line-chart-axis-label` →
`.atom-chart-value-axis-label`) so one stylesheet serves all charts.

## The 14 elements

**HTML chrome** (slots on `AtomChartBase`, so all five charts get them):

| Component | Slot | Replaces | Element params |
| --- | --- | --- | --- |
| `AtomChartHeading` | `Heading` | — (new) | `Subtitle`, `ChildContent` |
| `AtomChartCaption` | `Caption` | — (new) | `ChildContent` |
| `AtomChartLegend` | `Legend` | donut `ShowLegend` | `Columns`, `ShowValues`, `ShowPercent` |
| `AtomChartEmptyState` | `EmptyState` | — (new) | `Text`, `ChildContent` |

Slot named `Heading`, not `Title`: `<Title>` in consumer markup sits one casing away from SVG's own
`<title>` and reads as a bug.

**Cartesian** (slots on `AtomCartesianChartBase`):

| Component | Slot | Replaces | Element params |
| --- | --- | --- | --- |
| `AtomChartValueAxis` | `ValueAxis` | `ShowValueAxis` | presentation only |
| `AtomChartCategoryAxis` | `CategoryAxis` | implicit `HasLabels` | `Rotate` |
| `AtomChartAxisTitle` | `ValueAxisTitle`, `CategoryAxisTitle` | — (new) | `ChildContent` |
| `AtomChartValueLabels` | `ValueLabels` | `ShowValues` | presentation only |
| `AtomChartGridlines` | `Gridlines` | `ShowGridlines` | `Dashed` |
| `AtomChartBaseline` | `Baseline` | `ShowBaseline` | presentation only |

`AtomChartAxisTitle` is HTML in its own grid area (vertical via `writing-mode: vertical-rl`), never SVG
— so it costs no viewBox space and needs no reservation param.

**Per-chart specials:**

| Component | Slot | On | Replaces | Element params |
| --- | --- | --- | --- | --- |
| `AtomChartCenter` | `Center` | donut, gauge | `CenterContent` | `ChildContent` |
| `AtomChartSliceLabels` | `SliceLabels` | donut | `ShowSliceValues` | `MinPercent` |
| `AtomChartReadout` | `Readout` | gauge | `ShowValue` | `Offset` |
| `AtomChartRangeLabels` | `RangeLabels` | gauge | `ShowRange` | presentation only |

Splitting `Center` from `Readout` removes an existing wart: `AtomGauge.razor:58-67` uses
`CenterContent is not null` to suppress the readout, so today the two cannot coexist. As separate slots
they can.

## Batches

**1 — Foundation + HTML chrome.** `ChartContext.cs`, `AtomChartElementBase.cs`, the four HTML elements
with stylesheets, and the four slots + `CascadingValue` + `LegendPlacement` on `AtomChartBase`. Root
markup of all five charts becomes a layout grid with areas for heading / axis-title / plot / legend /
caption. `AtomSparkline` gets the slots but no plot chrome — its "no chrome by definition" contract
holds, and `Sparkline_offers_no_chrome_parameters` must be re-checked against the new surface.

**2 — Cartesian elements.** Six components + stylesheets; `AtomLineChart` / `AtomBarChart` rewired to
place slots in the right coordinate space (`ValueAxis` inside the translated `<g>` for line and vertical
bar, in the HTML row for horizontal bar — the chart knows `IsVertical` pre-render, so it places the same
slot in either spot and sets `ValueAxisInSvg`). Delete `ShowValueAxis`, `ShowGridlines`, `ShowBaseline`,
`ShowValues`; add `ValueAxisWidth`. `PadLeft`/`PadTop` now gate on `ValueAxis is not null` instead of
`ShowValueAxis` — including the ascender-clearance widening at `AtomLineChart.razor.cs:54` and
`AtomBarChart.razor.cs:64`.

**3 — Donut + gauge specials.** Four components; `Slices` → `ChartLegendEntry` projection for the
context; delete `ShowLegend`, `ShowSliceValues`, `SliceLabelMinPercent`, `ShowRange`, `ShowValue`,
`ReadoutOffset`, `CenterContent`. `EffectiveReadoutOffset`'s sweep-dependent default
(`AtomGauge.razor.cs`) moves into the context so `AtomChartReadout.Offset` can override it.

**4 — Tests.** `tests/BlazorAtoms.Charts.Tests/` — `ChartComponentTests.cs` and `AxisAndLabelTests.cs`
both assert on the deleted bools and on the old class names, so every such case is rewritten to render
the element instead. Nice-scale coverage survives untouched (`GridlineCount` and `NiceScale` stay on the
chart). New cases: slot absent renders nothing; slot present reserves the gutter; `CssClass`/`Style`
reach each element's root; every element renders standalone with no context;
`The_viewBox_matches_the_aspect_ratio_its_stylesheet_locks` still passes (no viewBox changes in this
design). Also re-check the hierarchy reflection tests (`Gauge_takes_a_single_Value_and_no_series`,
`Donut_has_no_gridline_parameters`) against the new slot surface.

**5 — Playground + docs.** `samples/Demos.Shared/Playgrounds/ChartsPlaygroundView.razor` — per repo
convention every parameter is wired and the page emits a valid copy/paste snippet, which now means
per-element toggles plus per-element `CssClass`/`Style` inputs, and snippet emission that nests element
tags inside their slots. `README.md` and `DEVELOPMENT.md` rewritten: the governing rule above, why slots
rather than flat children (the one-pass constraint), and the scoped-CSS boundary. Update
`src/LIBRARY-CATALOG.md`.

## Critical files

- `src/BlazorAtoms.Charts/AtomChartBase.cs` — HTML-chrome slots, `CascadingValue`, `LegendPlacement`
- `src/BlazorAtoms.Charts/AtomCartesianChartBase.cs` — cartesian slots; `GridlineCount`/`NiceScale` and
  the whole tick model stay put
- `src/BlazorAtoms.Charts/AtomSeriesChartBase.cs` — untouched; its geometry helpers feed the context
- `src/BlazorAtoms.Charts/Atom{Sparkline,LineChart,BarChart,Donut,Gauge}.razor{,.cs,.css}` — slot
  placement, context construction, bool removal, CSS moved out
- New: `ChartContext.cs`, `AtomChartElementBase.cs`, 14 × `AtomChart*.razor{,.cs,.css}`
- `tests/BlazorAtoms.Charts.Tests/{ChartComponentTests,AxisAndLabelTests}.cs`
- `samples/Demos.Shared/Playgrounds/ChartsPlaygroundView.razor`
- `src/BlazorAtoms.Charts/{README,DEVELOPMENT}.md`, `src/LIBRARY-CATALOG.md`

Patterns to follow rather than reinvent: `src/BlazorAtoms.Cards/CardContext.cs` +
`AtomCardSectionBase.cs` (rebuilt-per-render DTO cascade, nullable child params, `??` precedence,
children work standalone) and `src/BlazorAtoms.Shared/{AtomComponentBase,StyleVars}.cs`
(`ClassAttr`/`StyleAttr`, `--chart-*` token emission). Deliberately *not* the `AtomTabs` self-cascade +
`Register`/`NotifyChildren` pattern — that needs a second render pass, which is the thing this design
exists to avoid.

## Known consequences to state in the docs, not hide

- **Breaking.** Every existing chart usage loses its chrome until slots are added, including the X label
  row, which currently appears automatically whenever `Labels` is set.
- **`ValueAxisWidth` is on the chart, not on the axis element** — a gutter width the element owned could
  not reach `PadLeft` in time. Same reason `LegendPlacement` is a chart parameter.
- **An element in the wrong slot renders harmlessly wrong** (e.g. `AtomChartValueAxis` in `Heading`
  emits an SVG `<g>` into an HTML div, so nothing appears). Nothing enforces slot/element pairing;
  document the pairs.

## Verification

1. `dotnet build BlazorAtoms.sln` — both TFMs (net9.0, net10.0), 0 warnings. Note: full-solution builds
   fail with MSB3027 while `BlazorWebAppSvrDemo.exe` is running; build the affected projects
   individually if so.
2. `dotnet test BlazorAtoms.sln` — all 52 assemblies green.
3. Browser check of the Charts playground — **requires express permission per message before any
   run/serve/preview**, so ask rather than assume. What to look for, since these are failure modes this
   package has actually shipped: X labels still aligned under their marks (letterboxing regression), the
   topmost Y tick not clipped at the top-left (ascender clearance), the gutter not floating mid-chart,
   and each element's `CssClass`/`Style` visibly landing.
4. Grep the rendered markup for `NaN` — `No_coordinate_anywhere_is_NaN_or_Infinity` is the
   highest-value test in the suite, and bad geometry fails silently rather than throwing.
