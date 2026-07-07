# BlazorAtoms.StaticBadges

A **static** badge for Blazor — the small label/count that shows **when it has a value** — in a rich
variety of shapes. Four are pure-CSS boxes (**Pill / Circle / Square / Rounded**); six are drawn as
inline SVG so fill *and* border work on every one (**Star / Hexagon / Diamond / Shield / Burst /
Ribbon**). Wrap it around an icon/button and it overlays a corner; use it alone and it renders inline.
Takes any `object` value (type-aware formatting, or your own `Formatter`). No animation, no JS. Server
or WebAssembly.

> Want motion (pop / bounce / spin / pulse / ping)? See **BlazorAtoms.AnimatedBadges**.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.StaticBadges\BlazorAtoms.StaticBadges.csproj" />
```
```razor
@using BlazorAtoms.StaticBadges
```
Link `{App}.styles.css` (scoped-CSS bundle), as with any RCL.

## Usage

```razor
@* Overlay a host: red count on a bell, shows only when Count > 0 *@
<AtomStaticBadge Value="Count" Max="99" Variant="Variant.Danger">
    <button>🔔</button>
</AtomStaticBadge>

@* A star "sale" badge, inline *@
<AtomStaticBadge Value="5" Shape="Shape.Star" Variant="Variant.Warning" Size="40" />

@* A ribbon label *@
<AtomStaticBadge Value="NEW" Shape="Shape.Ribbon" Background="#7c3aed" />

@* Presence dot *@
<AtomStaticBadge Value="true" Dot="true" Variant="Variant.Success">
    <span>Status</span>
</AtomStaticBadge>
```

## The "show" gate

The badge renders **only when the value is present**:
- `null` / empty string → hidden.
- numeric `0` → hidden unless `ShowZero="true"`.
- `false` → hidden; `true` → shown (as a dot when `Dot`).
- numeric over `Max` → shows `"{Max}+"` (e.g. `Max="99"` → `99+`).

## Value → display string

1. `Formatter` (`Func<object?,string>`) if set — full override.
2. Otherwise type-aware: numbers (honor `Max`, invariant culture), `DateTime`→short date,
   `enum`→`[Description]` or name, `bool`→presence only, else `ToString()`.

## Shapes

| Shape | Kind | Notes |
|-------|------|-------|
| `Pill` (default) | CSS | Stadium; grows with text. |
| `Circle` | CSS | Equal width/height. |
| `Square` | CSS | Sharp corners. |
| `Rounded` | CSS | Corner radius via `Radius`. |
| `Star` | SVG | Five-point star. |
| `Hexagon` | SVG | Flat-top hexagon. |
| `Diamond` | SVG | Rhombus. |
| `Shield` | SVG | Crest. |
| `Burst` | SVG | 12-point starburst / seal. |
| `Ribbon` | SVG | Horizontal banner with notched ends; wider by default. |

SVG shapes are square by default (set `Width`/`Height` to override); `Ribbon` defaults wider. Fill is
`Background`, stroke is `BorderColor` + `BorderWidth`.

## Parameters

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `ChildContent` | `RenderFragment?` | `null` | Host element to overlay. Null → inline. |
| `Value` | `object?` | `null` | The value; gates the badge and is converted to text. |
| `Formatter` | `Func<object?,string>?` | `null` | Overrides type-aware conversion. |
| `Max` | `int` | `0` | Numeric cap → `"{Max}+"`. 0 = no cap. |
| `ShowZero` | `bool` | `false` | Show a numeric `0` instead of hiding. |
| `Dot` | `bool` | `false` | Textless presence dot (always a small circle). |
| `Placement` | `Placement` | `TopEnd` | Corner when overlaying: `TopEnd`/`TopStart`/`BottomEnd`/`BottomStart`/`TopCenter`/`BottomCenter`. |
| `Shape` | `Shape` | `Pill` | See the shapes table. |
| `Variant` | `Variant` | `Default` | `Default`/`Info`/`Success`/`Warning`/`Danger` color preset. |
| `Background`/`TextColor`/`BorderColor` | `string?` | `null` | Override variant colors (`--sb-bg`/`-color`/`-border`). Background is the SVG fill. |
| `BorderWidth` | `double?` | `null` | px (SVG stroke width for SVG shapes). |
| `Size` | `double?` | `null` | px; drives height/min-width/font-size. |
| `Width` | `string?` | `null` | Explicit width (any CSS length). |
| `Height` | `string?` | `null` | Explicit height (any CSS length). |
| `Radius` | `double?` | `null` | px, for `Shape.Rounded`. |
| `Offset` | `double?` | `null` | px nudge outward from the host corner (default straddles the corner). |
| `MaxWidth` | `string?` | `null` | CSS length; long text truncates with ellipsis. |
| `AriaLabel` | `string?` | `null` | Accessible label; defaults to the display text. |

## Accessibility

The badge is `role="status"`. `AriaLabel` overrides the announced text (give dots a meaningful label).

## Notes

- SVG shapes draw with `preserveAspectRatio="none"`, so a non-square `Width`/`Height` stretches the
  shape. Keep the box square (the default) for crisp stars/hexagons; widen only `Ribbon`.
- Border/stroke uses `vector-effect="non-scaling-stroke"`, so `BorderWidth` stays uniform regardless
  of shape size.
