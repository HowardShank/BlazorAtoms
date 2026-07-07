# BlazorAtoms.AnimatedBadges

An animated badge for Blazor — the small label/count that **pops in when it has a value**. Wrap it
around an icon/button and it overlays a corner (notification-count style); use it alone and it
renders inline. Takes any `object` value (type-aware formatting, or your own `Formatter`), and can
**Pop / Bounce / Spin / Pulse / Ping**. Pure CSS, no JS. Server or WebAssembly. All motion respects
`prefers-reduced-motion`.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.AnimatedBadges\BlazorAtoms.AnimatedBadges.csproj" />
```
```razor
@using BlazorAtoms.AnimatedBadges
```
Link `{App}.styles.css` (scoped-CSS bundle), as with any RCL.

## Usage

```razor
@* Overlay a host: red count on a bell, pops in only when Count > 0 *@
<AtomAnimatedBadge Value="Count" Max="99" Variant="Variant.Danger"
                   Animation="BadgeAnimation.Bounce" Trigger="AnimationTrigger.OnChange">
    <button>🔔</button>
</AtomAnimatedBadge>

@* Presence dot with a ping ring *@
<AtomAnimatedBadge Value="true" Dot="true" Variant="Variant.Success"
                   Animation="BadgeAnimation.Ping" Trigger="AnimationTrigger.Loop">
    <span>Status</span>
</AtomAnimatedBadge>

@* Inline (no host) — any object, custom formatter *@
<AtomAnimatedBadge Value="order" Formatter="@(o => ((Order)o!).Total.ToString(\"C\"))" />
```

## The "popup" gate

The badge renders **only when the value is present**:
- `null` / empty string → hidden.
- numeric `0` → hidden unless `ShowZero="true"`.
- `false` → hidden; `true` → shown (as a dot when `Dot`).
- numeric over `Max` → shows `"{Max}+"` (e.g. `Max="99"` → `99+`).

## Value → display string

1. `Formatter` (`Func<object?,string>`) if set — full override.
2. Otherwise type-aware: numbers (honor `Max`, invariant culture), `DateTime`→short date,
   `enum`→`[Description]` or name, `bool`→presence only, else `ToString()`.

## Parameters

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `ChildContent` | `RenderFragment?` | `null` | Host element to overlay. Null → inline. |
| `Value` | `object?` | `null` | The value; gates the popup and is converted to text. |
| `Formatter` | `Func<object?,string>?` | `null` | Overrides type-aware conversion. |
| `Max` | `int` | `0` | Numeric cap → `"{Max}+"`. 0 = no cap. |
| `ShowZero` | `bool` | `false` | Show a numeric `0` instead of hiding. |
| `Dot` | `bool` | `false` | Textless presence dot. |
| `Placement` | `Placement` | `TopEnd` | Corner when overlaying: `TopEnd`/`TopStart`/`BottomEnd`/`BottomStart`/`TopCenter`/`BottomCenter`. |
| `Shape` | `Shape` | `Pill` | `Pill`/`Circle`/`Square`/`Rounded`. |
| `Variant` | `Variant` | `Default` | `Default`/`Info`/`Success`/`Warning`/`Danger` color preset. |
| `Animation` | `BadgeAnimation` | `Pop` | `None`/`Pop`/`Bounce`/`Spin`/`Pulse`/`Ping`. |
| `Trigger` | `AnimationTrigger` | `Appear` | `Appear`/`Loop`/`OnChange`/`Hover`. |
| `Background`/`TextColor`/`BorderColor` | `string?` | `null` | Override variant colors (`--badge-bg`/`-color`/`-border`). |
| `BorderWidth` | `double?` | `null` | px. |
| `Size` | `double?` | `null` | px; drives height/min-width/font-size. |
| `Width` | `string?` | `null` | Explicit width (any CSS length); overrides size-driven min-width. |
| `Height` | `string?` | `null` | Explicit height (any CSS length); overrides size-driven height. |
| `Radius` | `double?` | `null` | px, for `Shape.Rounded`. |
| `Duration` | `double?` | `null` | Animation duration in seconds; overrides the per-animation default. |
| `Delay` | `double?` | `null` | Animation start delay in seconds. |
| `Offset` | `double?` | `null` | px nudge outward from the host corner (default straddles the corner). |
| `MaxWidth` | `string?` | `null` | CSS length; long text truncates with ellipsis. |
| `AriaLabel` | `string?` | `null` | Accessible label; defaults to the display text. |

## Accessibility

The badge is `role="status"` + `aria-live="polite"`, so screen readers announce count/value
changes. `AriaLabel` overrides the announced text (give dots a meaningful label).

## Notes

- **Entrance only (v1):** the badge animates *in* (`Pop`/`Appear`) and, with `OnChange`, replays when
  the value changes (it remounts via `@key`). It **hides immediately** — CSS can't animate an element
  that unmounts. An exit animation (kept mounted + state class) is a future enhancement.
- `prefers-reduced-motion: reduce` disables all badge motion; the badge still shows.
