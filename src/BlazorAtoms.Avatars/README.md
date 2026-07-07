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

## AtomInitialsAvatar

Shows initials on a colored background. Initials come from `Name` (or explicit `Initials`); the
background is picked deterministically from a palette by hashing the name — same name, same color —
unless you set `Background` / a gradient. Shape/size/border/gradient pass through to `AtomAvatar`.

```razor
<AtomInitialsAvatar Name="Ada Lovelace" Size="48" />               @* "AL", auto color *@
<AtomInitialsAvatar Name="Grace Hopper" Shape="Shape.Rounded" Radius="12" />
<AtomInitialsAvatar Initials="+7" Background="#6b7280" />           @* explicit, not truncated *@
```

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `Name` | `string?` | `null` | Initials derived from this (e.g. "Ada Lovelace" → "AL"). |
| `Initials` | `string?` | `null` | Explicit initials; overrides `Name`, not truncated. |
| `MaxInitials` | `int` | `2` | Cap on derived initials. |
| `Background` / `BackgroundGradient*` | `string?` | `null` | Override the auto palette color. |
| `TextColor` | `string?` | `null` | Initials color (default white). |
| `Shape`/`Radius`/`Size`/`BorderColor`/`BorderWidth` | — | — | Pass through to `AtomAvatar`. |

## AtomAvatarGroup

An overlapping row of avatars. Give it `Names` and it renders one `AtomInitialsAvatar` per name,
capping at `Max` with a "+N" overflow chip; or supply `ChildContent` with your own avatars (no
auto overflow).

```razor
<AtomAvatarGroup Names="@team" Max="4" Size="40" />

<AtomAvatarGroup Overlap="16">
    <AtomAvatar Src="/u/ada.jpg" />
    <AtomAvatar Src="/u/grace.jpg" />
    <AtomInitialsAvatar Name="Alan Turing" />
</AtomAvatarGroup>
```

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `Names` | `IReadOnlyList<string>?` | `null` | One initials avatar each + overflow. Null → use `ChildContent`. |
| `Max` | `int` | `0` | Cap before "+N" chip. 0 = show all. |
| `ChildContent` | `RenderFragment?` | `null` | Free-form avatars (no auto overflow). |
| `Size`/`Shape` | — | — | Applied to generated avatars. |
| `Overlap` | `double` | `12` | Overlap in px (`--avg-overlap`). |
| `RingColor`/`RingWidth` | `string`/`double` | `#fff`/`2` | Separating ring around each (`--avg-ring*`). |

## Notes

- The border is a rectangular CSS border clipped by the shape. It follows circle/square/rounded/
  squircle cleanly; on `Hexagon` (clip-path) only the flat edges show a ring.
- `role="img"` with `aria-label` from `Alt` (falls back to "avatar").
- `AtomAvatarGroup` styles its children through `::deep` (they carry a child component's CSS scope),
  applying the overlap margin and the separating ring.
