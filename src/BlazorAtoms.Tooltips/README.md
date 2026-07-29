# BlazorAtoms.Tooltips

Self-contained **tooltip** components for Blazor — **Server or WebAssembly** — in one library.
No dependencies; the only JavaScript is an opt-in cursor-follow module each component loads
itself. Ships as a Razor Class Library (RCL).

[![.NET](https://github.com/HowardShank/BlazorAtoms/actions/workflows/dotnet.yml/badge.svg)](https://github.com/HowardShank/BlazorAtoms/actions/workflows/dotnet.yml)

Three components, same trigger/placement/theming model, different bubble rendering:

- **`AtomTooltip`** — pure-CSS bubble. Rounded rect / pill / ellipse / thought / burst /
  folded-corner via CSS. Lightest; `Burst`/`FoldedCorner` are fill-only (clip-path drops the border).
- **`AtomShapedTooltip`** — bubble outline drawn as an inline **SVG path**, so **border works on
  every shape** (incl. cloud / burst / folded). Color from the same CSS tokens.
- **`AtomPaintedTooltip`** — SVG that also **paints** the bubble: linear-gradient fill, SVG stroke
  border, optional soft shadow — across every shape.

All three anchor to arbitrary trigger content, show on `:hover`/`:focus-within`, share the
`Placement` set (sides + corners + `Cursor`), and theme via `--tip-*` tokens.

---

## Types

| Type | Namespace |
|---|---|
| `AtomTooltip`, `AtomShapedTooltip`, `AtomPaintedTooltip` | `BlazorAtoms.Tooltips` |
| `Placement` (shared) | `BlazorAtoms.Tooltips` |
| `TooltipShape` / `ShapedTooltipShape` / `PaintedTooltipShape` | `BlazorAtoms.Tooltips` |

Each component has its own `Shape` enum, and the members differ: `TooltipShape` (`AtomTooltip`)
includes `Thought`; `ShapedTooltipShape` / `PaintedTooltipShape` include `Cloud` instead.
`Placement` is shared across all three components.

---

## Install

1. Reference the library — NuGet:
   ```xml
   <PackageReference Include="BlazorAtoms.Tooltips" Version="0.1.0" />
   ```
   …or a project reference:
   ```xml
   <ProjectReference Include="..\BlazorAtoms.Tooltips\BlazorAtoms.Tooltips.csproj" />
   ```
2. Ensure your layout references the scoped-CSS bundle — modern templates already include
   `<link rel="stylesheet" href="YourApp.styles.css" />`. An RCL's scoped CSS is bundled
   into the **consuming app's** `{App}.styles.css` automatically; without that link the
   tooltip renders unstyled/invisible.
3. Add the namespace to `_Imports.razor`:
   ```razor
   @using BlazorAtoms.Tooltips
   ```

---

## Basic usage

```razor
<AtomTooltip Text="Saves your changes">
    <button>Save</button>
</AtomTooltip>

<AtomTooltip Text="Top-start placement" Placement="TooltipPlacement.TopStart">
    <span tabindex="0">Hover or Tab to me</span>
</AtomTooltip>

@* Rich content instead of plain text *@
<AtomTooltip Placement="TooltipPlacement.Right">
    <a href="/docs">Docs</a>
    <TooltipContent>
        See the <strong>full reference</strong> for details.
    </TooltipContent>
</AtomTooltip>
```

The trigger (`ChildContent`) is shown as-is; the bubble appears on `:hover` or
`:focus-within` of the trigger. **Give non-interactive triggers (a bare `<span>`, an icon)
a `tabindex="0"`** so keyboard users can reach them — buttons/links already are focusable.

---

## Parameters

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `ChildContent` | `RenderFragment` | — | **Required.** The trigger content. |
| `Text` | `string?` | `null` | Simple bubble content. Ignored if `TooltipContent` is set. |
| `TooltipContent` | `RenderFragment?` | `null` | Rich bubble content; takes priority over `Text`. |
| `Placement` | `TooltipPlacement` | `Top` | Side (`Top`/`Bottom`/`Left`/`Right`, each with `…Start`/`…End`), diagonal corner (`TopLeft`/`TopRight`/`BottomLeft`/`BottomRight`), or `Cursor` (follows the pointer — see below). |
| `Shape` | `TooltipShape` | `Rectangle` | Bubble outline: `Rectangle` (rounded rect, uses `Radius`), `Pill`, `Ellipse`, `Thought`, `Burst`, `FoldedCorner`. See [Shapes](#shapes). |
| `ShowArrow` | `bool` | `true` | Draw the attachment arrow pointing at the trigger. Ignored in `Cursor` mode and on `Burst`/`FoldedCorner` shapes. |
| `Disabled` | `bool` | `false` | Suppresses the bubble entirely; trigger still renders. |
| `Background` | `string?` | `null` | Sets `--tip-bg`. Any CSS color. |
| `TextColor` | `string?` | `null` | Sets `--tip-color`. |
| `BorderColor` | `string?` | `null` | Sets `--tip-border`. |
| `BorderWidth` | `double?` | `null` | Sets `--tip-border-width` (px). |
| `Radius` | `double?` | `null` | Sets `--tip-radius` (px). |
| `ArrowSize` | `double?` | `null` | Sets `--tip-arrow-size` (px). |
| `MaxWidth` | `string?` | `null` | Sets `--tip-max-width` (any CSS length, e.g. `"16rem"`). |
| `Offset` | `double?` | `null` | Sets `--tip-offset` — gap between trigger and bubble (px). In `Cursor` mode, the gap between the pointer and the bubble (default 12). |
| `Class` | `string?` | `null` | Extra CSS class(es) on the root element. |
| `Style` | `string?` | `null` | Extra inline style appended after the built-in theme style. |

---

## Shapes

Set `Shape` to change the bubble outline:

| Shape | Notes |
|-------|-------|
| `Rectangle` *(default)* | Rounded rectangle; corner rounding via `Radius`. Keeps border + arrow. |
| `Pill` | Fully rounded ends (stadium). Keeps border + arrow. |
| `Ellipse` | Elliptical; content is inset + centered. Best for short text. Keeps border + arrow. |
| `Thought` | "Thinking" bubble — rounded body; the arrow becomes a trail of shrinking circles pointing at the trigger (obeys `ShowArrow`). Keeps border. |
| `Burst` | Comic spiky star. **Fill only** — `clip-path` removes the border + arrow. |
| `FoldedCorner` | Dog-ear folded top-right corner. **Fill only** — `clip-path` removes the border + arrow. |

```razor
<AtomTooltip Text="Nice!" Shape="TooltipShape.Pill"><button>Pill</button></AtomTooltip>
<AtomTooltip Text="Hmm…" Shape="TooltipShape.Thought" Placement="TooltipPlacement.Top"><span tabindex="0">Think</span></AtomTooltip>
<AtomTooltip Text="POW!" Shape="TooltipShape.Burst"><button>Burst</button></AtomTooltip>
```

> **Border on `Burst`/`FoldedCorner`:** these use CSS `clip-path`, which clips the border away,
> so `BorderColor`/`BorderWidth` have no visible effect and no arrow is drawn. Use
> `AtomShapedTooltip` or `AtomPaintedTooltip` instead if you need a border on these shapes.

## Theming (CSS custom properties)

Same token model as `BlazorAtoms.ActivityIndicators`: each token has a *public* name you can
set, and a *private*, scheme-aware `-d` default the component falls back to.

| Token | Role | Default (dark) | Default (light) |
|---|---|---|---|
| `--tip-bg` | Bubble background | `#1f2430` | `#ffffff` |
| `--tip-color` | Bubble text | `#f2f4f8` | `#1a1d24` |
| `--tip-border` | Bubble border | `#3a4152` | `#d7dbe3` |
| `--tip-border-width` | Border width | `1px` | |
| `--tip-radius` | Corner radius | `6px` | |
| `--tip-arrow-size` | Arrow size | `8px` | |
| `--tip-max-width` | Bubble max-width | `16rem` | |
| `--tip-offset` | Trigger↔bubble gap | `8px` | |

Three equivalent ways to theme:

```razor
@* 1. Parameters — per instance *@
<AtomTooltip Text="Danger zone" Background="#7f1d1d" TextColor="#fff" BorderColor="#450a0a">
    <button>Delete</button>
</AtomTooltip>

@* 2. Class + your own CSS rule — reusable named theme *@
<AtomTooltip Text="Danger zone" Class="tip-danger">
    <button>Delete</button>
</AtomTooltip>
```
```css
.tip-danger { --tip-bg: #7f1d1d; --tip-color: #fff; --tip-border: #450a0a; }
```

```razor
@* 3. A CSS variable on any ancestor — themes every tooltip inside a region at once *@
<div class="danger-panel">
    <AtomTooltip Text="A"><button>A</button></AtomTooltip>
    <AtomTooltip Text="B"><button>B</button></AtomTooltip>
</div>
```
```css
.danger-panel { --tip-bg: #7f1d1d; --tip-color: #fff; }
```

---

## Positioning behavior

Positioning is pure CSS — deterministic, and works the same in every browser and every
render mode, with no JavaScript involved.

What to expect:
- **No auto-flip** on viewport overflow — the bubble stays on the side you asked for. Pick a
  `Placement` that has room, or leave margin around edge triggers.
- The bubble is clipped by an ancestor with `overflow:hidden`/`clip`, since it lives inside
  the trigger's wrapper. Keep the tooltip out of clipped/scrolling containers, or give that
  container room.

### Corner placements

`TopLeft`, `TopRight`, `BottomLeft`, `BottomRight` place the bubble diagonally off the named
corner of the trigger (e.g. `TopRight` = above-and-right). Same pure-CSS mechanism as the
side placements.

### Cursor mode (`TooltipPlacement.Cursor`)

The bubble follows the mouse pointer while hovering the trigger. This one mode uses a small
JS module under the hood, but it's **invisible to you** — no `<script>` tag, no DI
registration, nothing to wire up.

```razor
<AtomTooltip Text="I follow your cursor" Placement="TooltipPlacement.Cursor">
    <span tabindex="0">Hover over me</span>
</AtomTooltip>
```

Caveats specific to Cursor mode:
- **Needs interactivity** — JS interop can't run during static SSR/prerender, so the bubble
  won't position until the component is interactive (`InteractiveServer`/`WebAssembly`/`Auto`).
- **No arrow** — there's no fixed edge to point from, so `ShowArrow` is ignored.
- **`Offset`** sets the gap between the pointer and the bubble (default 12px).
- Every other placement remains 100% JS-free; the module is only loaded if a Cursor tooltip
  is actually used.

---

## Accessibility

- The bubble has `role="tooltip"` and a stable per-instance `id`; the trigger wrapper has
  a matching `aria-describedby` — wired at render time in C#, no JS needed.
- Shown on `:hover` **and** `:focus-within`, so keyboard users see it too. Give
  non-interactive trigger elements a `tabindex="0"` so they're reachable.
- A short hide-delay lets the pointer travel from the trigger into the bubble itself, so a
  link/button inside `TooltipContent` stays reachable on hover.

---

## Reduced motion

The show/hide fade is a CSS `transition`, not required for functionality — under
`prefers-reduced-motion: reduce` the transition is removed and the bubble still
shows/hides instantly.

---

## Server + WebAssembly

Pure CSS, no JS, no DI — works unchanged under `InteractiveServer`,
`InteractiveWebAssembly`, and `InteractiveAuto`, and even in static SSR (no JS interop to
wait for).

---

## AtomShapedTooltip — SVG outline (border on every shape)

Use `AtomShapedTooltip` when you need a visible border on `Burst`, `FoldedCorner`, or `Cloud`
shapes — `AtomTooltip` can't draw a border on those. Fill and border apply uniformly on every
shape, colored from the same `--tip-bg` / `--tip-border` tokens; positioning, show/hide, and
`Cursor` mode work the same as `AtomTooltip`.

```razor
<AtomShapedTooltip Text="Bordered burst!" Shape="ShapedTooltipShape.Burst"
                   BorderColor="#eab308" BorderWidth="2">
    <button>Hover me</button>
</AtomShapedTooltip>

<AtomShapedTooltip Text="A thought…" Shape="ShapedTooltipShape.Cloud" Width="160px" Height="90px">
    <span tabindex="0">Think</span>
</AtomShapedTooltip>
```

`Shape` is a `ShapedTooltipShape`: `Rectangle` (default; `Radius` in viewBox units 0–50), `Pill`,
`Ellipse`, `Cloud` (thinking-cloud outline + circle trail), `Burst`, `FoldedCorner`. Shares the
`AtomTooltip` parameters plus explicit `Width`/`Height` (any CSS length — useful for `Cloud`/
`Ellipse`, whose outlines need room). `BorderWidth` is a uniform, non-scaling SVG stroke.

Content alignment (shared by `AtomShapedTooltip` and `AtomPaintedTooltip`):

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `TextAlign` | `TooltipTextAlign?` | `null` | Horizontal align of the text/content: `Start` / `Center` / `End`. `null` keeps each shape's default (start; centered for `Cloud`/`Ellipse`). |
| `VerticalAlign` | `TooltipVerticalAlign?` | `null` | Vertical align of the content: `Top` / `Center` / `Bottom`. Only visible when `Height` gives the bubble more room than the content needs. `null` = centered. |

```razor
<AtomShapedTooltip Text="Bottom-right aligned" Width="200px" Height="110px"
                   TextAlign="TooltipTextAlign.End" VerticalAlign="TooltipVerticalAlign.Bottom">
    <button>Hover me</button>
</AtomShapedTooltip>
```

## AtomPaintedTooltip — SVG that also paints

`AtomPaintedTooltip` extends the SVG-outline idea by **painting** the bubble in the SVG: an optional
linear-gradient fill, an SVG stroke border, and an optional soft drop shadow — across every shape.
With no gradient set it falls back to the solid `--tip-bg` token, so it also works as a plain shaped
tooltip.

```razor
<AtomPaintedTooltip Text="Gradient!" Shape="PaintedTooltipShape.Rectangle"
                    GradientFrom="#f97316" GradientTo="#7c3aed" GradientAngle="120"
                    BorderColor="#f8fafc" BorderWidth="1">
    <button>Hover me</button>
</AtomPaintedTooltip>
```

Distinctive parameters (on top of the `AtomShapedTooltip` set):

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `GradientFrom` / `GradientTo` | `string?` | `null` | Set both to paint a linear-gradient fill (overrides `Background`). |
| `GradientAngle` | `double` | `90` | Gradient direction in degrees (0 = left→right, 90 = top→bottom). |
| `Shadow` | `bool` | `true` | Draw a soft drop shadow behind the bubble. |

`Shape` is a `PaintedTooltipShape` (same members as `ShapedTooltipShape`).

---

## Notes & gotchas

- **Scoped-CSS bundle must be linked** — the most common "it renders but isn't styled" cause.
- **No auto-flip** — the bubble stays on the requested side; see "Positioning behavior".
- **Give non-interactive triggers a `tabindex`** or keyboard users can't reveal the tooltip.
- **Per-component `Shape` enums** — `TooltipShape` / `ShapedTooltipShape` / `PaintedTooltipShape`.
  `Placement` is shared across all three.
