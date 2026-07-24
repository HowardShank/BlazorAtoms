# BlazorAtoms.Typography

Text primitives for Blazor. Ships **`AtomTextCycle`** — a zero-JS flip-cascade word rotator:
cycles through a list of words/phrases in an infinite loop, one at a time, sliding (4 directions)
or spinning in place fast-to-slow before landing upright (2 directions).

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Typography\BlazorAtoms.Typography.csproj" />
```
```razor
@using BlazorAtoms.Typography
```

## AtomTextCycle

```razor
Make
<AtomTextCycle Words="@(new[] { "wOrK", "lifeStyle", "Everything" })" />
AweSoMe!
```

Renders inline — wrap it in your own surrounding text/markup rather than passing a prefix/suffix
parameter.

### How it animates

The component doesn't ship a fixed `@starting-style`/`@keyframes` — the percentage breakpoints of
the loop depend on how many words you pass, so a small `@keyframes atom-text-cycle-{v|h|spin}-n{N}`
block is generated per instance (by motion kind and word count) and emitted inline. Every effect
appends a duplicate of the first word as an extra row/column so the loop's wrap-around (100% → 0%)
is an instant, invisible snap instead of a visible jump.

### Effect

```razor
<AtomTextCycle Words="@words" Effect="TextCycleEffect.SlideLeftToRight" ItemWidth="10rem" />
<AtomTextCycle Words="@words" Effect="TextCycleEffect.SpinClockwise" SpinTurns="3" Easing="ease-out" />
```

`Effect` picks both the motion kind and its direction:

- `SlideBottomToTop` *(default)* / `SlideTopToBottom` — vertical slide, sized by `ItemHeight`.
- `SlideRightToLeft` / `SlideLeftToRight` — horizontal slide, sized by `ItemWidth`.
- `SpinClockwise` / `SpinCounterClockwise` — the word sits in a normal horizontal line, then spins
  in place (`SpinTurns` full rotations) as it transitions, landing upright exactly as the next word
  arrives. Sized by `ItemHeight`, same as vertical slide (`ItemWidth` isn't used by Spin). Pair
  with `Easing="ease-out"` for a classic fast-then-slowing-to-a-stop feel.

Each direction's pair reuses the exact same generated keyframes via CSS
`animation-direction: reverse` — reversing time reverses both the spatial/rotational direction and
the word-visit order, which is why it's the cheapest correct implementation rather than a second
keyframe generator.

### Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Words` | `IReadOnlyList<string>` | *required* | Words/phrases to cycle through. 1 word renders statically; 0 renders nothing. |
| `Duration` | `int` | `5000` | Milliseconds for one full loop through every word. |
| `SlideRatio` | `double` | `0.12` | Fraction (0–1) of each word's time slot spent transitioning to the next; the rest is a hold. |
| `Easing` | `string` | `"ease-in-out"` | CSS easing for the transition phase. |
| `Effect` | `TextCycleEffect` | `SlideBottomToTop` | Motion kind + direction — see above. |
| `SpinTurns` | `int` | `2` | Full rotations Spin makes per transition. Ignored for Slide. |
| `ItemHeight` | `string` | `"3.5rem"` | Row height for the vertical slide and for Spin — must fit the tallest word at your font size. |
| `ItemWidth` | `string` | `"8rem"` | Column width for the horizontal slide — must fit the widest word. Ignored for vertical slide and Spin. |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root `<span>`.

## Notes

- **Zero JS.** Pure CSS `animation`/`@keyframes`, generated server/client-side in C# — no
  `IJSObjectReference`, works identically in every render mode including static SSR.
- **Accessibility.** Respects `prefers-reduced-motion: reduce` (animation disabled, first word
  shown). The duplicate wrap-around row is `aria-hidden`.
