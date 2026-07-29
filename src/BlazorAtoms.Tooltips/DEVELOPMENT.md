# BlazorAtoms.Tooltips — Development Notes

Internal design rationale and implementation notes for maintainers of this library. This
file is **not** packed into the NuGet package — see `README.md` for consumer-facing usage
docs.

## Why three components instead of one

All three components share the same trigger/placement/theming model; they differ only in
how the bubble shape is rendered, because each successive technique trades simplicity for
capability:

- **`AtomTooltip`** renders the bubble entirely in CSS (border-radius / CSS shapes /
  `clip-path`). This is the lightest option, but CSS `clip-path` — used for the `Burst` and
  `FoldedCorner` shapes — clips away the border and arrow along with the corners, so those
  two shapes are fill-only in this component. There's no way to keep a border on a
  `clip-path` shape in CSS alone.
- **`AtomShapedTooltip`** exists to fix that: it draws the bubble outline as an inline SVG
  `<path>` instead of a CSS shape. Because the outline is a real path, an SVG `stroke` can
  follow it on every shape (including `Cloud`, `Burst`, `FoldedCorner`), so border rendering
  is uniform. It's heavier than `AtomTooltip` (inline SVG markup) but only pays that cost
  when picked.
- **`AtomPaintedTooltip`** extends `AtomShapedTooltip` one step further: since the bubble is
  already an SVG shape, painting it (linear-gradient fill via SVG `<linearGradient>`, SVG
  stroke, optional soft drop shadow via SVG filter) comes for very little extra complexity
  over drawing a plain outline.

In short: CSS shapes → SVG outline (to regain border support) → SVG paint (to add
gradient/shadow), each component only paying for the capability it adds over the previous
one.

## Package layout

```
BlazorAtoms.Tooltips/
  AtomTooltip.razor / .razor.cs / .razor.css          <- pure-CSS tooltip
  AtomShapedTooltip.razor / .razor.cs / .razor.css     <- SVG-outline (border on every shape)
  AtomPaintedTooltip.razor / .razor.cs / .razor.css    <- SVG paints fill/stroke/shadow
  TooltipPlacement.cs                                          <- shared Placement enum
  TooltipShape.cs / ShapedTooltipShape.cs / PaintedTooltipShape.cs  <- per-component shape enums
  wwwroot/atom-tooltip.js / atom-shaped-tooltip.js / atom-painted-tooltip.js  <- cursor-follow modules
```

## Per-component `Shape` enums

Each component keeps its own `Shape` enum rather than sharing one, because the shape sets
genuinely differ between rendering techniques: `AtomTooltip`'s `TooltipShape` has `Thought`
(a CSS-only shrinking-circle trail effect that doesn't need an SVG outline), while the two
SVG-based components use `Cloud` instead (an SVG path outline). Keeping them separate avoids
a shared enum with members that are invalid/no-ops on some components.

## Positioning implementation

`Placement` is applied via a `data-placement` attribute on the root element and resolved
entirely in CSS: `position:absolute` on the bubble, offset from the trigger's wrapper
(which is `position:relative`) per placement value. This was chosen deliberately over
JS-based positioning (e.g. Popper/Floating UI-style libraries) because it's deterministic
and works identically in every browser and render mode, including static SSR, with zero
JS dependency.

The known trade-off of this "v1" approach is no auto-flip on viewport overflow, and
clipping when the bubble's containing wrapper is inside an `overflow:hidden`/`clip`
ancestor (documented as user-facing caveats in the README).

**Deferred:** a CSS Anchor Positioning based enhancement for auto-flip has been considered,
but Anchor Positioning is still browser-version-sensitive as of this writing, so v1
deliberately sticks with the mechanism that works everywhere. Revisit once browser support
is broad enough to not regress the "works everywhere" guarantee.

## Cursor mode JS interop internals

`TooltipPlacement.Cursor` is the one mode that needs JavaScript, since CSS has no way to read the
live cursor position. The implementation is intentionally self-contained so consumers never
need to wire anything up:

- The component lazy-imports its own JS module (`_content/BlazorAtoms.Tooltips/atom-tooltip.js`,
  and the shaped/painted equivalents) via `IJSObjectReference` the first time a `Cursor`
  tooltip actually renders — not eagerly, so apps that never use `Cursor` mode never pay for
  the module load.
- It attaches a `pointermove` listener to track the cursor while the trigger is hovered.
- It implements `IAsyncDisposable` to detach the listener and dispose the JS module
  reference when the component is torn down, avoiding a leaked interop reference.
- No `<script>` tag and no DI registration are required from the consuming app — the
  self-import pattern means the module reference is entirely owned and cleaned up by the
  component instance.

## Other deferred / future-work notes

- A bordered, crisper version of `Burst`/`FoldedCorner` in `AtomTooltip` itself (rather than
  pointing consumers at `AtomShapedTooltip`/`AtomPaintedTooltip`) was considered, via an SVG
  background instead of a pure CSS `clip-path`. Not implemented — `AtomShapedTooltip`
  already covers this need.
- CSS Anchor Positioning based auto-flip (see "Positioning implementation" above).
