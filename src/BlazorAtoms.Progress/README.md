# BlazorAtoms.Progress

Progress indicators for Blazor.

| Component | What it is | Own params |
|---|---|---|
| **`AtomProgressBar`** | Horizontal bar with an optional buffered band | `ValuePosition`, `Buffer`, `Width`, `Radius` |
| **`AtomProgressRing`** | Circular SVG arc with an optional centered readout | `Diameter`, `Cap`, `StartAngle`, `CenterContent` |
| **`AtomProgressSteps`** | Discrete step tracker (wizard / checkout) | `Steps`, `Current`, `Orientation`, `Marker`, `StatusFor`, `StepTemplate`, `OnStepClick` |
| **`AtomMeter`** | Scalar gauge with quality bands (`role="meter"`) | `Low`, `High`, `Optimum`, `Segments`, `ShowScale`, `Width`, `Radius` |
| **`AtomScrollProgressBar`** | Fixed reading-progress bar driven by scroll position | `Position`, `Align`, `Width`, `Color`, `Height`, `ScrollContainer` |

The first four share a base (see [Shared parameters](#shared-parameters)) and are **zero-JS**.
`AtomScrollProgressBar` is the odd one out on both counts — it has no `Value` at all, and it does need
a little JS; it is documented separately [below](#atomscrollprogressbar).

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Progress\BlazorAtoms.Progress.csproj" />
```
```razor
@using BlazorAtoms.Progress
```

## Quick start

```razor
<AtomProgressBar Value="62" Label="Uploading" ShowValue="true" />

@* No Value at all → indeterminate. It sweeps, and announces a busy state. *@
<AtomProgressBar Label="Working" />

<AtomProgressRing Value="72" ShowValue="true" Diameter="120" Cap="ProgressRingCap.Round" />

<AtomProgressSteps Steps="@(new[] { "Cart", "Address", "Payment" })"
                   Current="1"
                   Marker="ProgressStepMarker.Check" />

<AtomMeter Value="88" Low="60" High="85" Optimum="0"
           Label="Disk used" ShowValue="true"
           Formatter="@(v => $"{v} GB")" />
```

## Null `Value` means indeterminate

`Value` is `double?`, and **null is the indeterminate state** — one parameter rather than a value plus
a separate `Indeterminate` flag, so `Value="40"`-but-ignore-it is not expressible. An indeterminate
indicator:

- plays its own "amount unknown" animation (the bar sweeps, the ring spins — but **not the meter**,
  which has nothing to sweep toward; see [below](#a-null-value-on-a-meter)),
- **omits `aria-valuenow`**, which is exactly how ARIA spells "indeterminate progressbar",
- shows no readout even with `ShowValue="true"` — there is no number to show,
- sets `data-indeterminate="true"` on the root, so the state is styleable.

Out-of-range values are clamped for drawing, and the *clamped* value is what gets announced, so the
visual and the announcement never disagree. A collapsed scale (`Max <= Min`) reports 0 rather than
dividing by zero.

## Shared parameters

Every indicator except `AtomScrollProgressBar` inherits these. `Value`/`Min`/`Max`/`Formatter` come
from `AtomProgressValueBase` and are absent on `AtomProgressSteps`, which has no continuous value.

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Value` | `double?` | `null` | Null = indeterminate. Not on `AtomProgressSteps`. |
| `Min` / `Max` | `double` | `0` / `100` | The scale. Not on `AtomProgressSteps`. |
| `Formatter` | `Func<double,string>?` | `null` | Formats the readout; default is a whole percent. Not on `AtomProgressSteps`. |
| `Label` | `string?` | `null` | Caption. Also the fallback accessible name. |
| `ShowValue` | `bool` | `false` | Renders the readout. |
| `AriaLabel` | `string?` | `null` | Falls back to `Label`, then a per-component default — never unnamed. |
| `Visible` | `bool` | `true` | `false` hides via `display:none`, staying in the DOM. |
| `Variant` | `ProgressVariant` | `Primary` | `Default`/`Primary`/`Info`/`Success`/`Warning`/`Danger` → `data-variant`. |
| `Size` | `ProgressSize` | `Medium` | `Small`/`Medium`/`Large` → `data-size`. |
| `Effect` | `ProgressEffect` | `None` | Opt-in CSS motion → `data-effect` (no attribute when `None`). |
| `Thickness` | `double?` | `null` | px → `--progress-thickness`. Overrides the `Size` default. |
| `TrackColor` | `string?` | `null` | → `--progress-track-color`. |
| `FillColor` | `string?` | `null` | → `--progress-fill-color`. Overrides the `Variant` accent. |
| `TextColor` | `string?` | `null` | → `--progress-text-color`. |
| `FontSize` | `double?` | `null` | px → `--progress-font-size`. |
| `Duration` | `double?` | `null` | seconds → `--progress-duration`. Drives both the value transition and any effect keyframe. |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root element.

`Radius` is **not** on the shared base — it is a parameter on `AtomProgressBar` and `AtomMeter` only,
because they are the only two with a rectangular track to round. On the ring and the step markers it
would be a parameter that does nothing.

**Theming priority**, lowest to highest: each component's CSS defaults block →
`[data-variant]`/`[data-size]` rules → a consumer stylesheet targeting the root class → the
`--progress-*` parameters above (inline) → your `Style` (appended last, so it wins).

### Tokens with no parameter behind them

The rest of the `--progress-*` surface has no matching parameter — set it through `Style` (or your own
stylesheet) when the presets aren't enough. Each is a normal custom property on the component root, so
it also cascades if you set it on an ancestor.

| Token | Used by | Default | What it is |
|---|---|---|---|
| `--progress-accent` | all | `#64748b` | What `Variant` actually sets. Override to add a scheme without touching `FillColor`. |
| `--progress-gap` | bar, meter, steps | `.5rem` | Space between the track, the label, and the readout. |
| `--progress-marker-size` | steps | `2rem` | Step marker diameter (`Size` presets it). |
| `--progress-step-gap` | steps (vertical) | `1.25rem` | Connector run between two markers. |
| `--progress-tick-color` / `--progress-tick-width` | meter | `currentColor` 45% / `2px` | `Segments` tick appearance. |
| `--progress-level-optimum` / `-suboptimum` / `-sub-suboptimum` | meter | green / amber / red | The quality-band fills. |

### `ProgressEffect`

Pure CSS, keyed off `data-effect` — no C# state, identical in every render mode. `None` (default),
`Stripes`, `StripesAnimated`, `Shimmer`, `Glow`, `Pulse`, `Gradient`. All are `prefers-reduced-motion`
guarded. Adding one is an enum member plus a CSS block.

Effects are independent of the indeterminate state, which plays its own keyframe regardless — "we
don't know the amount" is a different concern from "decorate the amount we do know." Where the two
would collide on the same element (only one `animation` per element), the indeterminate animation
wins. On `AtomProgressSteps` the fill-texture members are inert: there is no filled bar to texture.

## AtomProgressBar

```razor
<AtomProgressBar Value="30" Buffer="70" ValuePosition="ProgressValuePosition.Outside" Width="20rem" />
```

`Buffer` draws a dimmer second band behind the fill on the same scale — the "downloaded but not yet
played" span. It is dropped while indeterminate, where there is no meaningful scale position for it.

`ValuePosition` is `Inside` (default, right-aligned in the fill), `Outside` (after the track), or
`Above` (on the label's row). `Inside` puts a floor on the track height so a `Small` bar doesn't clip
the text.

`role="progressbar"` lives on the **track**, not the root: the root also holds the label and readout,
and a progressbar must not contain extra text content of its own.

## AtomProgressRing

```razor
<AtomProgressRing Value="72" Diameter="120" Thickness="10" Cap="ProgressRingCap.Round">
    <CenterContent><strong>4.2</strong> GB free</CenterContent>
</AtomProgressRing>
```

`CenterContent` beats the `ShowValue` readout, so you can put an icon or a "3 of 7" in the middle
instead.

**The arc math has no π in it.** `pathLength="100"` on the circle re-bases its own length onto a 0–100
scale, so `stroke-dasharray="100"` plus a `stroke-dashoffset` of `100 - percent` draws exactly that
percentage at *any* radius — no `2πr` to recompute when the diameter changes.

Two consequences worth knowing:

- **Stroke width resolves in C#, not CSS.** The radius depends on it (the ring must sit inside its
  box or the stroke clips at the edges), and CSS geometry properties like `r` aren't portable yet. So
  `Thickness` falls back to a per-`Size` constant (6/8/12 px) in C#, and is clamped so it can never
  swallow the hole. The `--progress-thickness` token is still emitted for effect CSS to read.
- **`StartAngle` is ignored while indeterminate.** The spin keyframe owns the element's `transform`,
  which overrides the inline attribute the angle is written to.

`Stripes`/`Gradient` degrade to the closest stroke-only equivalent here: an SVG stroke can't take a
`background-image` without a per-instance `<pattern>`/`<linearGradient>` def, which would mean minting
ids that differ between the prerender and interactive passes.

## AtomProgressSteps

```razor
<AtomProgressSteps Steps="_steps" Current="@_step"
                   Marker="ProgressStepMarker.Check"
                   Orientation="ProgressStepsOrientation.Vertical"
                   StatusFor="StatusOf"
                   OnStepClick="GoToStep" />
```

Progress here is `Current` — a zero-based index into `Steps` — not a `Value`. Steps before it are
`Complete`, the one at it is `Active`, later ones are `Pending`. `Current` past the end marks
everything complete (the "finished" state); a negative `Current` marks everything pending.

`ProgressStepStatus.Error` is **never** inferred from `Current`, because "the user is on step 3" says
nothing about whether step 2 failed. Only `StatusFor` can produce it.

`Marker` is `Number` (default), `Dot`, `Check` (number until complete, then a tick), or `None` (an
empty circle for you to style). An `Error` step always draws a cross, whatever the marker style.

**`OnStepClick` changes the markup, not just the handler.** Supplied, every marker is a real
`<button>` — focusable, Enter/Space-activatable, and given an `aria-label` (the visible caption is a
sibling it doesn't contain, so the button would otherwise announce only its number). Not supplied, the
markers are inert `aria-hidden` `<span>`s, so a non-navigable tracker adds nothing to the tab order.

`ShowValue` renders a clamped position counter ("2 of 3"), never "4 of 3".

## AtomMeter

```razor
<AtomMeter Value="88" Low="60" High="85" Optimum="0"
           Segments="5" ShowScale="true"
           Formatter="@(v => $"{v} GB")" />
```

A meter is a measurement that simply *is* what it is — disk used, fuel, a score, password strength —
as opposed to a task advancing toward completion. It renders `role="meter"`, not `progressbar`.

**Why not the native `<meter>` element:** its bar is drawn by the UA through vendor pseudo-elements
that differ per browser (`::-webkit-meter-optimum-value` vs Firefox's own set) and can't be themed
consistently, let alone carry this library's `--progress-*` surface or effect keyframes. The
*semantics* are the native ones — the ARIA role, and the low/high/optimum model below.

`data-level` classifies the value using the HTML `<meter>` spec's own three-way rule, and it wins over
`Variant` (once you've stated an `Optimum`, the value's quality is more informative than a decorative
scheme). An explicit `FillColor` still beats both, being inline.

| `Optimum` sits… | Optimum band | Suboptimum | Sub-suboptimum |
|---|---|---|---|
| below `Low` (small is good) | ≤ `Low` | `Low`..`High` | > `High` |
| above `High` (large is good) | ≥ `High` | `Low`..`High` | < `Low` |
| between them (middle is good) | `Low`..`High` | outside it | never occurs |

No `Optimum` → no `data-level` at all: with no stated ideal there is nothing to judge against. Unset
`Low`/`High` collapse to the ends of the scale, as in the native element.

`Segments` draws its ticks as **one** `repeating-linear-gradient` overlay, so a 20-segment meter still
costs a single node. `ShowScale` adds a ruler with `Min`/`Max` at the ends and any `Low`/`High` marks
at their real positions; it shows raw numbers (or your `Formatter`'s output), never percentages — a
percentage of itself would be meaningless on a ruler.

### A null `Value` on a meter

**It does not animate** — unlike the bar and the ring, there is no indeterminate motion here, because a
meter has nothing to sweep *toward*. The track renders empty, hatched (via `data-indeterminate`) so an
unmeasured meter is distinguishable at a glance from a genuine zero.

It also **drops `role="meter"` and every `aria-value*` attribute**. ARIA requires `aria-valuenow` on
`role="meter"`; there is no indeterminate meter in the spec, unlike `progressbar` where *omitting*
valuenow is precisely how indeterminate is spelled. An empty track that announces nothing beats a
meter that is invalid — and beats inventing an `aria-valuenow` of `Min`, which would read to assistive
tech as a real 0% measurement. Supply a `Value` and the full semantics come back.

## AtomScrollProgressBar

```razor
<AtomScrollProgressBar />
```

Drop it in once, anywhere on the page (typically near the top of a layout) — it renders as a
`position: fixed` strip and needs no wrapping or `ChildContent`.

### How it animates — and the one real caveat

The primary path is a pure CSS **scroll-driven animation**: a plain `width: 0% → 100%` keyframe
tied directly to page scroll position via `animation-timeline` — the browser interpolates it
natively, no scroll event listener, no per-frame JS. Getting there still needs one small **one-time**
JS call, even on supporting browsers: `atom-progress.js` finds the actual scroll container (walking
up from the bar) and explicitly names a `scroll-timeline` on it, then points the bar's
`animation-timeline` at that name. This isn't optional — the more obvious approach
(`animation-timeline: scroll()` alone, letting the browser auto-resolve the "nearest" scrollable
ancestor) silently breaks for a `position: fixed` bar: verified live that Chrome resolves "nearest"
via the fixed element's reparented containing-block chain (always the viewport), not its DOM
ancestry, so inside an app-shell layout with an inner-scrolling content div (not the whole page),
the bar would bind to the wrong scroller and never move.

**Scroll-driven animations are Chromium-only today** (not yet in Firefox or Safari). On other
browsers, the same `OnAfterRenderAsync` call falls back to a plain `scroll`/`resize` listener on the
same detected scroll container, setting `width` directly from
`scrollTop / (scrollHeight - clientHeight)`.

The same listener path is used, on any browser, when the bar isn't a DOM descendant of the container
it tracks — see [Which element does it track?](#which-element-does-it-track) — because a named
scroll-timeline isn't visible from outside the declaring element's subtree.

### Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Color` | `string` | `"#e6175d"` | Color of the bar. |
| `Height` | `string` | `"12px"` | Thickness of the bar. Any CSS length. |
| `Position` | `ScrollProgressPosition` | `Top` | Which edge of the scroll container the bar sticks to (`Top`/`Bottom`) — not the raw viewport edge; see below. |
| `Width` | `string?` | `null` | Width of the track. Any standard CSS length (`"50%"`, `"300px"`, `"20rem"`, ...), resolved against the scroll container, not the viewport. `null` (default) spans the full container width. |
| `Align` | `ScrollProgressAlign` | `Start` | Horizontal alignment of the track within the container when `Width` makes it narrower (`Start`/`Center`/`End`). |
| `ScrollContainer` | `string?` | `null` | CSS selector naming the scrollable element to track. `null` = walk up from the bar for the nearest ancestor that is actually scrolling. See below. |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root `<div>`.

### Which element does it track?

Resolution order:

1. **`ScrollContainer`**, if set — `document.querySelector`, so the bar can live anywhere in the
   markup rather than inside the thing it measures. A selector that matches nothing falls through
   to step 2 rather than failing silently. Same parameter name and semantics as
   `AtomScrollTo.ScrollContainer` in `BlazorAtoms.Navigation`.

   One mechanical consequence: a CSS named scroll-timeline is only visible to descendants of the
   element declaring it, so when the bar sits *outside* the container it tracks, the native
   scroll-driven animation can't be used and that bar transparently uses the scroll-listener path
   instead (the same one non-Chromium browsers use). Behaviour is identical; it's per bar, so one
   page can have a nested-container bar on the listener path and a page-level bar running natively.
2. Otherwise, walk up from the bar for the nearest ancestor with `overflow-y: auto|scroll` **that
   is currently overflowing**, stopping at the document.

That "currently overflowing" test is deliberate — an `overflow: auto` box that never scrolls isn't
this bar's scroller — but it makes the answer depend on *when* you ask. So resolution is repeated
rather than once-and-done, driven by two signals that both re-resolve and rebind when the answer
changes:

- a **`ResizeObserver`** on the page and the current container — catches the container or the page
  actually changing size (a window resize, a sidebar opening);
- a **capture-phase `scroll` listener** on `document` — catches a container that has *become*
  scrollable since the last resolution. This one is necessary because `ResizeObserver` reports a
  change to an element's own box, never to its `scrollHeight`: in an app-shell layout, a
  viewport-bounded content div never resizes when content grows inside it, so the
  not-overflowing → overflowing transition is invisible to the observer. The first scroll of the
  real container is the signal instead. (Scroll events don't bubble, but capture phase on
  `document` still sees them from any element.)

Both funnel into one `requestAnimationFrame`-debounced check, so a burst of layout changes or a
stream of scroll events costs at most one re-resolve per frame.

Belt and braces: the bar also isn't painted until a container has been measured (below), so even a
resolution that starts out wrong is never visible.

Set `ScrollContainer` explicitly when you already know the scroller — it skips the heuristic
entirely, and it's the clearest way to give several bars on one page different targets.

### It stays hidden until it has been measured

Until the module reports a successful measure, the track carries
`atom-scroll-progress-track-pending` (`visibility: hidden`). The pre-JS `width: 100%` on a
`position: fixed` track spans the whole **viewport**, so painting it before measurement shows a
full-width bar in the wrong place.

If JS never runs — static SSR/prerender, a failed module import, scripting disabled — the bar stays
hidden rather than appearing misplaced. Nothing is lost by that: the fill is advanced either by a
scroll-timeline or by the fallback scroll listener, so without JS it could never move anyway.

Several bars can share one container: the named `scroll-timeline` is created once per container and
reference-counted, so instances don't overwrite each other's timeline.

### Sizing and positioning against the container, not the viewport

A `position: fixed` element's percentages (`width`, `left`) and `top: 0`/`bottom: 0` normally
resolve against the *viewport* — wrong for an app-shell layout where the actual scroll container
is narrower than (and offset from) the full screen (e.g. next to a sidebar, below a fixed header).
`atom-progress.js` measures the real scroll container's bounding box and:

- sets the track's `left`/`width` in px to match the container horizontally (or, when `Width` is
  set, resolves that length against the container and positions it per `Align`),
- sets `top` (or `bottom`) in px to match the container's own edge, not the viewport's,
- re-syncs on window resize, on the container's own scroll, and whenever `Position`/`Width`/`Align`
  change at runtime.

`Width` accepts any CSS length. Rather than reimplementing unit math for `%`/`px`/`rem`/`vw`/
`calc()`/etc., the module applies it to a hidden probe element placed *inside* the actual scroll
container and reads back its resolved pixel width — the same technique the browser already uses,
so arbitrary units "just work." One CSS nuance worth knowing: a percentage `Width` resolves
against the container's *content box* (excluding its own padding), per standard CSS percentage
resolution — this is correct behavior, not a quirk, but can surprise you if you expected it against
the container's full visual (border-box) width.

## Notes

- **Not fully zero-JS.** Unlike the rest of this repo's effect components, this one genuinely
  needs a small JS touch even on Chromium (to name and bind the scroll-timeline explicitly) and
  full JS on non-Chromium browsers (no declarative-CSS-only way to track scroll position at all
  there). The JS is still self-contained — no `BlazorAtoms.Behaviors` dependency, this package has
  zero BlazorAtoms deps.
- **Accessibility.** Scroll-driven animations are 1:1 with the user's own scroll input, not
  autoplaying motion, so this doesn't gate on `prefers-reduced-motion` the way a timed animation
  would (common guidance treats it the same way native scrollbars aren't considered "motion").
- **Render modes.** JS interop can't run during static SSR/prerender — the bar's native CSS
  keyframes still apply immediately in Chromium regardless; the JS fallback (for other browsers)
  attaches once the component is interactive.
