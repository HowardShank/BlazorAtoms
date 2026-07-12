# BlazorAtoms.Ratings

Star/heart/… **rating** for Blazor, drawn as pure inline SVG — no JavaScript, no dependencies,
works in Server or WebAssembly and every render mode.

One component, `AtomRating`, does both jobs:

- **Display** (`ReadOnly="true"`) — fills icons to *any* fraction of the value, e.g. `4.3` of `5`.
- **Input** (default) — hover preview, click to set, full keyboard control, snaps to a configurable
  `Step` (whole stars, half stars, or finer).

The value is `double?`: **`null` is the "unrated" state** (every icon empty) and is distinct from a
real `0`. Two-way bind with `@bind-Value`.

## Install

```
dotnet add package BlazorAtoms.Ratings
```

No setup, no DI registration, no `<script>` tag.

## Usage

```razor
@using BlazorAtoms.Ratings

@* Interactive, half-star input, two-way bound *@
<AtomRating @bind-Value="score" />

@* Read-only display with a fractional value, a count, and hearts *@
<AtomRating Value="4.3" ReadOnly="true" Icon="RatingIcon.Heart"
            Color="#e0245e" ShowValue="true" Count="1204" />

@* Whole-star, clearable input with a custom icon path *@
<AtomRating @bind-Value="score" Step="1" Clearable="true"
            IconPath="M12 2l3 7h7l-5.5 4 2 7L12 16l-6.5 4 2-7L2 9h7z" />

@code {
    double? score;
}
```

## Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Value` (`@bind-Value`) | `double?` | `null` | Current rating; `null` = unrated. |
| `Max` | `int` | `5` | Number of icons. |
| `Step` | `double` | `0.5` | Input granularity (1 = whole, 0.5 = half, 0.25 = quarter). Display still fills to any fraction. |
| `ReadOnly` | `bool` | `false` | Display only — no hover/click/keyboard. |
| `Disabled` | `bool` | `false` | Dim and block interaction. |
| `Clearable` | `bool` | `false` | Click the current value again (or press Delete/0) to reset to `null`. |
| `Icon` | `RatingIcon` | `Star` | Built-in shape: Star, Heart, Circle, Square, Diamond, Gem, Emerald, Marquise, Teardrop, Apple, Cherry, Lemon, Grape, Strawberry, Banana, Triangle, Thumb, Bolt. |
| `IconPath` | `string?` | — | Custom filled-icon SVG path (overrides `Icon`). |
| `EmptyIcon` | `RatingIcon?` | — | Distinct shape for the empty portion. |
| `EmptyIconPath` | `string?` | — | Custom empty-icon SVG path. |
| `IconViewBox` | `string` | `0 0 24 24` | View box for custom paths. |
| `Rotation` | `double?` | — | Rotate every glyph N degrees (clockwise, about center). Visual only. |
| `Color` | `string?` | amber | Filled color → `--rating-color`. |
| `EmptyColor` | `string?` | tint of text | Empty/track color → `--rating-empty`. |
| `HoverColor` | `string?` | filled | Preview color → `--rating-hover`. |
| `Size` | `double?` | `24` | Icon px (w = h) → `--rating-size`. |
| `Gap` | `double?` | `6` | Gap between icons/labels px → `--rating-gap`. |
| `ShowValue` | `bool` | `false` | Show the numeric value beside the icons. |
| `ValueFormat` | `string` | `0.#` | Value label format. |
| `UnratedText` | `string` | `Unrated` | Value label when `null`. |
| `Count` | `int?` | — | Optional review/vote count shown after the icons. |
| `CountFormat` | `string` | `N0` | `Count` format. |
| `AriaLabel` | `string?` | auto | Accessible label; auto-generated from value + `Max` when null. |

Plus the shared escape hatch on every Atom component: `CssClass`, `Style`, and arbitrary splatted
attributes (`title`, `data-*`, `id`, ARIA, …).

## Keyboard (input mode)

| Key | Action |
|---|---|
| → / ↑ | Increase by `Step` |
| ← / ↓ | Decrease by `Step` (clears to `null` past the bottom when `Clearable`) |
| Home | Set to `Step` (the minimum) |
| End | Set to `Max` |
| Delete / Backspace / 0 | Clear to `null` when `Clearable` |

## How the fractional fill works

Each icon is two stacked copies of the same SVG glyph: an empty one underneath and a full-color one
on top, clipped by a wrapper whose width is the fraction of the value in that position. No SVG clip
ids, no masks, no JS — just `overflow: hidden` on a percentage-width box. That makes `4.3` render as
four full icons and one 30%-filled icon.
