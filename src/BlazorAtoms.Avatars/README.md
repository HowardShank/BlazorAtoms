# BlazorAtoms.Avatars

An avatar for Blazor — a **head/shoulders silhouette** (solid or gradient fill) or an **image**,
cropped to a selectable shape (**circle / square / rounded / squircle / hexagon**) with a corner
radius, a **color or gradient background** (with angle), and an optional border ring. Pure CSS +
inline SVG, no JS. Server or WebAssembly.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Avatars\BlazorAtoms.Avatars.csproj" />
```
```razor
@using BlazorAtoms.Avatars
```
Link `{App}.styles.css` (scoped-CSS bundle), as with any RCL.

## Usage

```razor
@* Default placeholder silhouette *@
<AtomAvatar Size="48" />

@* Image, cropped to a hexagon *@
<AtomAvatar Src="/users/ada.jpg" Alt="Ada Lovelace" Shape="Shape.Hexagon" Size="64" />

@* Gradient background + gradient silhouette *@
<AtomAvatar Size="80"
            BackgroundGradientFrom="#0ea5e9" BackgroundGradientTo="#7c3aed" BackgroundGradientAngle="135"
            FigureGradientFrom="#ffffff" FigureGradientTo="#e0e7ff" />

@* Rounded with a ring *@
<AtomAvatar Src="/users/ada.jpg" Shape="Shape.Rounded" Radius="16"
            BorderColor="#22d3ee" BorderWidth="3" Size="72" />
```

## How it works

- **Image vs silhouette:** set `Src` to show an image (cropped to the shape via `object-fit: cover`
  + the container's crop); leave it null for the built-in head/shoulders silhouette.
- **Crop:** circle/square/rounded/squircle use `border-radius` + `overflow: hidden`; hexagon uses
  `clip-path`.
- **Background:** solid `Background`, or a gradient when both `BackgroundGradientFrom`/`To` are set
  (`BackgroundGradientAngle` in CSS degrees, 0 = up). Shows behind a transparent image and around a
  silhouette.
- **Silhouette fill:** solid `FigureColor`, or a gradient when both `FigureGradientFrom`/`To` are set
  (`FigureGradientAngle` rotates about the center; per-instance SVG `<linearGradient>`).

## Parameters

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `Src` | `string?` | `null` | Image URL. Null → silhouette. |
| `Alt` | `string?` | `null` | Image alt / accessible label. |
| `Shape` | `Shape` | `Circle` | `Circle`/`Square`/`Rounded`/`Squircle`/`Hexagon`. |
| `Radius` | `double?` | `null` | px, for `Shape.Rounded`. |
| `Size` | `double?` | `null` | px; width = height (default 3rem). |
| `Background` | `string?` | `null` | Solid background (default `#e5e7eb`). |
| `BackgroundGradientFrom`/`To` | `string?` | `null` | Both set → gradient background. |
| `BackgroundGradientAngle` | `double` | `135` | CSS degrees (0 = up). |
| `FigureColor` | `string?` | `null` | Solid silhouette fill (default `#9ca3af`). |
| `FigureGradientFrom`/`To` | `string?` | `null` | Both set → gradient silhouette. |
| `FigureGradientAngle` | `double` | `135` | SVG rotation about the center. |
| `BorderColor` | `string?` | `null` | Ring color; null → no border. |
| `BorderWidth` | `double?` | `null` | px (default 1 when `BorderColor` set). |

## Notes

- The border is a rectangular CSS border clipped by the shape. It follows circle/square/rounded/
  squircle cleanly; on `Hexagon` (clip-path) only the flat edges show a ring.
- `role="img"` with `aria-label` from `Alt` (falls back to "avatar").
