# BlazorAtoms.PaintedTooltips

A Blazor tooltip whose inline SVG both **shapes and paints** the bubble — linear-gradient fill,
SVG stroke border, and an optional soft shadow — across every shape (rectangle, pill, ellipse,
cloud, burst, folded corner). With no gradient set it behaves like a solid shaped tooltip
(`--tip-bg`), so it's a superset of `BlazorAtoms.ShapedTooltips`.

Server or WebAssembly. Pure CSS for positioning + show/hide; only `Placement.Cursor` uses a
tiny self-loaded JS module.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.PaintedTooltips\BlazorAtoms.PaintedTooltips.csproj" />
```
```razor
@using BlazorAtoms.PaintedTooltips
```

## Usage

```razor
<AtomPaintedTooltip Text="Sunset" GradientFrom="#f97316" GradientTo="#7c3aed" GradientAngle="120">
    <button>Gradient</button>
</AtomPaintedTooltip>

<AtomPaintedTooltip Text="POW!" Shape="Shape.Burst"
                    GradientFrom="#fde047" GradientTo="#ef4444" BorderColor="#7f1d1d">
    <span tabindex="0">Burst</span>
</AtomPaintedTooltip>
```

## Parameters

Superset of `BlazorAtoms.ShapedTooltips` (`Text`/`TooltipContent`, `Placement` incl. corners +
`Cursor`, `Shape`, `Background`, `TextColor`, `BorderColor`, `BorderWidth`, `Radius`, `ArrowSize`,
`MaxWidth`, `Offset`, `ShowArrow`, `Disabled`, `Class`/`Style`), plus:

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `GradientFrom` | `string?` | `null` | Gradient start color. Set with `GradientTo` to paint a linear-gradient fill (overrides `Background`). |
| `GradientTo` | `string?` | `null` | Gradient end color. |
| `GradientAngle` | `double` | `90` | Gradient direction in degrees (0 = left→right, 90 = top→bottom). |
| `Shadow` | `bool` | `true` | Soft drop shadow behind the bubble. |
| `Width` | `string?` | `null` | Explicit bubble width (any CSS length). Null = fit content. |
| `Height` | `string?` | `null` | Explicit bubble height (any CSS length). Null = fit content. |

> **`Cloud`/`Ellipse` sizing:** their outlines don't reach the box corners, so give them an
> explicit `Width`/`Height` (e.g. `Width="160px" Height="90px"`) for a rounder look. They also
> get extra interior padding automatically; content is centered.

Notes:
- Gradient is a per-instance SVG `<linearGradient>` in the bubble's `<svg>`; the path fill points
  at it. No gradient set → solid `--tip-bg` fill (plain shaped tooltip).
- The border (SVG stroke) uses `BorderColor`/`--tip-border` (solid). Gradient strokes aren't
  exposed yet.
- The arrow is a solid CSS square; when a gradient is set it's tinted to `GradientFrom` so it
  doesn't clash. `Burst`/`FoldedCorner` show no arrow; `Cloud` shows the circle trail.
- SVG uses `viewBox="0 0 100 100"` + `preserveAspectRatio="none"` + `non-scaling-stroke`, so the
  border width stays uniform; `Burst`/`Cloud` outlines stretch slightly on very non-square bubbles.
