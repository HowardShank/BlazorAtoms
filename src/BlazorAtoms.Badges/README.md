# BlazorAtoms.Badges

Badge, chip, tag and pill components for Blazor — one library, four components:

- **`AtomBadge`** — a label / count in a rich variety of shapes: pill, circle, square, rounded, plus
  SVG-drawn **star, hexagon, diamond, shield, starburst, ribbon**. Motion is **opt-in** via
  `Animation` (default `None`): **Pop / Bounce / Spin / Pulse / Ping**, disabled under
  `prefers-reduced-motion`.
- **`AtomChip`** — an **interactive** chip: leading icon/avatar slot, label, optional remove (×)
  button; keyboard-operable (click / select) when given an `OnClick` handler.
- **`AtomTag`** — a display-oriented **categorization label** (GitHub-style), rounded rectangle,
  optional icon + remove button. No interaction.
- **`AtomPill`** — a **status pill**: fully-rounded, soft-tinted, with a leading status dot.

The chip family (`AtomChip` / `AtomTag` / `AtomPill`) shares a color `Variant` and a Solid / Soft /
Outline `Appearance`; they form an interactivity gradient — Chip (interactive) → Tag (label) →
Pill (status).

Badges overlay a host element at a corner (`ChildContent` given) or render inline, are fully styleable
via `--badge-*` tokens, and accept any object `Value` with type-aware formatting
(numeric `Max` → `"99+"`, `ShowZero`, enum `[Description]`, `DateTime`, `bool`/`Dot`, or a
`Formatter` override). Pure CSS + inline SVG, no JS. Server or WebAssembly.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Badges\BlazorAtoms.Badges.csproj" />
```
```razor
@using BlazorAtoms.Badges
```
Link `{App}.styles.css` (scoped-CSS bundle), as with any RCL.

## AtomBadge

```razor
@* Count overlaying an icon, capped at 99 (no motion — the default) *@
<AtomBadge Value="Count" Max="99" Variant="Variant.Danger">
    <span class="bell">🔔</span>
</AtomBadge>

@* SVG shapes — star, ribbon *@
<AtomBadge Value="5" Shape="Shape.Star" Variant="Variant.Warning" Size="40" />
<AtomBadge Value="NEW" Shape="Shape.Ribbon" Background="#7c3aed" />

@* Pops in on the corner of a bell, re-bounces when the count changes *@
<AtomBadge Value="Count" Max="99" Variant="Variant.Danger"
           Animation="BadgeAnimation.Bounce" Trigger="AnimationTrigger.OnChange">
    <span>🔔</span>
</AtomBadge>

@* Ping-ring presence dot *@
<AtomBadge Value="true" Dot="true" Variant="Variant.Success"
           Animation="BadgeAnimation.Ping" Trigger="AnimationTrigger.Loop">
    <Avatar />
</AtomBadge>

@* Custom formatter *@
<AtomBadge Value="order" Formatter="@(o => ((Order)o!).Total.ToString(\"C\"))" />
```

`Shape`: `Pill` (default) / `Circle` / `Square` / `Rounded` are pure-CSS boxes; `Star` / `Hexagon` /
`Diamond` / `Shield` / `Burst` / `Ribbon` are drawn as inline SVG paths so fill **and** border apply
to every shape.

`Animation` (default `None`): `Pop` (entrance) / `Bounce` / `Spin` / `Pulse` / `Ping` (expanding
ring). `Trigger`: `Appear` / `Loop` / `OnChange` (replays when `Value` changes) / `Hover`. All motion
is disabled under `@media (prefers-reduced-motion: reduce)`; the badge still shows. Only the entrance
animates — hiding is immediate (CSS can't animate an unmounting node). Animations transform the whole
badge, SVG or box; for `Ping` on an SVG shape the ring is dropped (a rounded rect can't trace a star)
and the shape pulses instead.

## AtomChip / AtomTag / AtomPill

```razor
@* Interactive filter chip — toggles Selected, keyboard-operable, removable *@
<AtomChip Text="Blazor" Variant="Variant.Info" Selected="@on" OnClick="() => on = !on"
          Removable="true" OnRemove="Remove">
    <Icon><span>★</span></Icon>
</AtomChip>

@* Categorization tags (GitHub-style labels) *@
<AtomTag Text="bug" Variant="Variant.Danger" />
<AtomTag Text="wontfix" Appearance="Appearance.Outline" Removable="true" OnRemove="Remove" />

@* Status pills — soft tint + leading dot *@
<AtomPill Text="Active"  Variant="Variant.Success" />
<AtomPill Text="Pending" Variant="Variant.Warning" />
<AtomPill Text="Failed"  Variant="Variant.Danger" Appearance="Appearance.Solid" />
```

| | `AtomChip` | `AtomTag` | `AtomPill` |
|---|---|---|---|
| Role | interactive | display label | status |
| Default shape | stadium (pill) | rounded rect | stadium (pill) |
| Default `Appearance` | `Soft` | `Solid` | `Soft` |
| Leading `Icon` slot | ✓ | ✓ | ✓ (replaces the dot) |
| Remove (×) button | ✓ | ✓ | — |
| `OnClick`/`Selected`/`Disabled` | ✓ | — | — |
| Leading status `Dot` | — | — | ✓ (default on) |

`Appearance`: `Solid` (accent fill + contrasting text) / `Soft` (low-opacity accent tint + accent
text) / `Outline` (transparent + accent border/text). `Variant` picks the accent color; explicit
`Background` / `TextColor` / `BorderColor` still override. `Size` (px) drives height and font;
`Height` (px) overrides just the box height (vertical size) independent of `Size`; `AtomChip`/`AtomTag`
also take a `Radius`. `AtomPill` additionally takes `DotColor` (status-dot color override). `AtomChip`
becomes a `role="button"` (Enter/Space, `aria-pressed`) only when `OnClick` has a handler — otherwise
it is a static label.

All three take font-styling overrides: `FontFamily`, `FontSize` (px, overrides the `Size`-derived
size), `FontWeight`, `FontStyle`, `LetterSpacing`, `TextTransform`. Each maps to a `--<name>-font-*`
CSS token; null keeps the component default.

## Shared parameters

| Parameter | Type | Notes |
|-----------|------|-------|
| `Value` | `object?` | Anything; empty/null → nothing renders (the "popup" gate). |
| `Formatter` | `Func<object?,string>?` | Full override of the display string. |
| `Max` | `int?` | Numeric overflow → `"{Max}+"`. |
| `ShowZero` | `bool` | Show a numeric `0` (hidden by default). |
| `Dot` | `bool` | Textless presence dot. |
| `Variant` | `Variant` | `Default`/`Info`/`Success`/`Warning`/`Danger` color preset (overridable). |
| `Background`/`TextColor`/`BorderColor`/`BorderWidth` | `string?`/`double?` | Explicit color overrides. |
| `Shape` | `Shape` | Outline shape (see per-component notes). |
| `Size` | `double?` | px; drives font/min-size. |
| `Placement` | `Placement` | Corner when overlaying: `TopEnd` (default) / `TopStart` / `BottomEnd` / `BottomStart` / `TopCenter` / `BottomCenter`. |
| `AriaLabel` | `string?` | Accessible label (falls back to the display string). `role="status"`. |

## Notes

- Overlay mode wraps `ChildContent` in a `position:relative` host and places the badge absolutely by
  `data-placement`; inline mode (no child) renders in normal flow.
- `AtomBadge` is a `role="status"` `aria-live="polite"` region so count changes are announced.
