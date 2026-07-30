# BlazorAtoms.Skeletons

Loading placeholders for Blazor — the grey shapes you show while data is on its way, sized to match the
content that will replace them so the page doesn't jump.

- **Zero dependencies, zero JavaScript.** Pure CSS animation, so it behaves identically under static
  SSR, Server, WebAssembly and prerender. No `<script>` tag, no `builder.Services.Add…()`.
- **One painted primitive.** `AtomSkeletonBlock` draws; the other three are presets that render it.
- **Respects `prefers-reduced-motion`** — the animation stops on a flat colour, with no layout change.

## Install

```bash
dotnet add package BlazorAtoms.Skeletons
```

```razor
@using BlazorAtoms.Skeletons
```

## The four components

```razor
@* the primitive — any rectangle *@
<AtomSkeletonBlock Width="8rem" Height="2rem" Radius="6px" />

@* a paragraph: N lines, last one short *@
<AtomSkeletonText Lines="4" />

@* a portrait *@
<AtomSkeletonAvatar Size="56px" Shape="SkeletonAvatarShape.Rounded" />

@* media band + avatar + lines *@
<AtomSkeletonCard Lines="3" MediaHeight="160px" />
```

The usual shape of it — swap the skeleton for the real thing when the data lands:

```razor
@if (posts is null)
{
    <AtomSkeletonCard AriaLabel="Loading posts" />
}
else
{
    <PostCard Post="posts[0]" />
}
```

## Shared parameters

Every component has these.

| Parameter | Type | Default | |
| --- | --- | --- | --- |
| `Animation` | `SkeletonAnimation` | `Shimmer` | `Shimmer`, `Pulse` or `None` |
| `BaseColor` | `string?` | — | resting colour, any CSS colour |
| `HighlightColor` | `string?` | — | the sweeping band; `Shimmer` only |
| `Duration` | `string?` | `1.4s` | one cycle, e.g. `"900ms"` |
| `AriaLabel` | `string?` | — | see [Accessibility](#accessibility) |
| `Visible` | `bool` | `true` | `false` = `display:none`, stays in the DOM |
| `CssClass` / `Style` | `string?` | — | appended after the component's own |

## Per-component parameters

**`AtomSkeletonBlock`** — `Width` (`100%`), `Height` (`1rem`), `Radius` (`4px`).

**`AtomSkeletonText`** — `Lines` (`3`), `LineHeight` (`0.8rem`), `LineRadius`, `Gap` (`0.55rem`),
`LastLineWidth` (`60%`), `Width` (`100%`).

`Lines="0"` renders an empty container rather than throwing, so binding it to a computed count needs no
guard. Line widths are deterministic — full width except the last — so the prerender and interactive
passes agree and nothing shifts on hydration.

**`AtomSkeletonAvatar`** — `Size` (`40px`), `Shape` (`Circle`, `Square`, `Rounded`).

No `Radius`, and no separate `Width`/`Height`: `Shape` owns the corners, and one `Size` keeps a "circle"
from rendering as an ellipse. For an arbitrary radius, use `AtomSkeletonBlock` — that's what it's for.

**`AtomSkeletonCard`** — `ShowMedia` (`true`), `MediaHeight` (`120px`), `ShowAvatar` (`true`),
`AvatarSize` (`40px`), `Lines` (`3`), `LineGap`, `Gap` (`0.75rem`), `Padding` (`0`), `Width` (`100%`).

## Theming

Set the parameters, or set the `--skeleton-*` custom properties yourself — they're what the parameters
write, so a rule on an ancestor themes every skeleton beneath it:

```css
.dark-surface {
    --skeleton-base-color: #23262b;
    --skeleton-highlight-color: #2f343b;
    --skeleton-duration: 1.1s;
}
```

Available: `--skeleton-base-color`, `--skeleton-highlight-color`, `--skeleton-duration`,
`--skeleton-width`, `--skeleton-height`, `--skeleton-radius`, `--skeleton-gap`, `--skeleton-padding`.

## Accessibility

A skeleton is decorative — it tells a screen-reader user nothing they can act on, and the page is
usually announcing its own loading state already. So by default the root is `aria-hidden="true"` and
contributes nothing to the accessibility tree.

Give it an `AriaLabel` and it becomes a polite live region instead:

```razor
<AtomSkeletonText AriaLabel="Loading comments" />
```

```html
<div class="atom-skeleton-text" role="status" aria-live="polite" aria-label="Loading comments">
```

Opt-in rather than default because six skeletons on a page would otherwise announce six times, and
compete with whatever the page itself is saying. On a preset, the label goes on the preset only — the
blocks it renders stay hidden, so one card is one announcement, not five.

## Reduced motion

Under `prefers-reduced-motion: reduce` the animation is removed **and** the shimmer's gradient is
cleared, so the shape holds a flat `BaseColor`. Clearing the gradient matters: leaving it painted would
freeze a bright band part-way across the shape, which reads as a rendering bug rather than a
placeholder. `Animation="SkeletonAnimation.None"` is a design choice you make; this is the automatic
fallback.
