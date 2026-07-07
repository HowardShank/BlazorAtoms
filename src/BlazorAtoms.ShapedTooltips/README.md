# BlazorAtoms.ShapedTooltips

A Blazor tooltip whose bubble **outline is drawn with inline SVG** — so `border` and the arrow
work on **every** shape, including burst, folded-corner, and cloud (the CSS-only
`BlazorAtoms.Tooltips` can't border its clip-path shapes). **Color still lives in CSS tokens**
(`--tip-bg` → SVG `fill`, `--tip-border` → SVG `stroke`), so the theming model is unchanged.

Server or WebAssembly. Pure CSS for positioning + show/hide; only `Placement.Cursor` uses a
tiny self-loaded JS module.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.ShapedTooltips\BlazorAtoms.ShapedTooltips.csproj" />
```
```razor
@using BlazorAtoms.ShapedTooltips
```
Ensure your layout links `{App}.styles.css` (scoped-CSS bundle) — as with any RCL.

## Usage

```razor
<AtomShapedTooltip Text="Bordered burst!" Shape="Shape.Burst" BorderColor="#eab308" Background="#7f1d1d">
    <button>POW</button>
</AtomShapedTooltip>

<AtomShapedTooltip Text="A thought…" Shape="Shape.Cloud" Placement="Placement.Top">
    <span tabindex="0">Hmm</span>
</AtomShapedTooltip>
```

## Parameters

Same set as `BlazorAtoms.Tooltips` (`Text`/`TooltipContent`, `Placement` incl. corners + `Cursor`,
`Background`/`TextColor`/`BorderColor`/`BorderWidth`, `ArrowSize`, `MaxWidth`, `Offset`,
`ShowArrow`, `Disabled`, `Class`/`Style`), plus:

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `Shape` | `Shape` | `Rectangle` | `Rectangle`/`Pill`/`Ellipse`/`Cloud`/`Burst`/`FoldedCorner` — all drawn as SVG. |
| `Radius` | `double` | `12` | Corner rounding for `Rectangle`, in **viewBox units** (0–50), not px. |
| `Width` | `string?` | `null` | Explicit bubble width (any CSS length). Null = fit content. |
| `Height` | `string?` | `null` | Explicit bubble height (any CSS length). Null = fit content. |

> **`Cloud`/`Ellipse` sizing:** their outlines don't reach the box corners, so tight text can
> touch the edge. They get extra interior padding automatically; for a rounder look give them an
> explicit `Width`/`Height` (e.g. `Width="160px" Height="90px"`). Content is centered.

Notes:
- The SVG uses `viewBox="0 0 100 100"` with `preserveAspectRatio="none"` (stretches to the
  bubble) and `vector-effect: non-scaling-stroke` so the border stays a uniform px width. On
  very non-square bubbles the `Burst`/`Cloud` outlines stretch slightly.
- `BorderColor`/`BorderWidth` now apply to **all** shapes (SVG stroke).
- `Burst`/`FoldedCorner` integrate no separate arrow; `Cloud` shows the circle trail; the other
  shapes use the arrow. `Cursor` mode never shows an arrow.
