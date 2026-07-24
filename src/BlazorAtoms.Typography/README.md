# BlazorAtoms.Typography

Text primitives for Blazor. Ships:

- **`AtomTextCycle`** — a zero-JS flip-cascade word rotator: cycles through a list of
  words/phrases in an infinite loop, one at a time, sliding (4 directions) or spinning in place
  fast-to-slow before landing upright (2 directions).
- **`AtomTextScramble`** — a zero-JS one-shot entrance animation for a single word, splitting it
  into characters that fly/drop/spin in with a staggered delay (7 effects). Not a cycling
  component like `AtomTextCycle` — deliberately single-word/specialized.

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

## AtomTextScramble

```razor
<AtomTextScramble Word="AWESOME" Effect="TextScrambleEffect.RevolveScale" />
```

A single word, split into one `<span>` per character, each flying/dropping/spinning into place with
a per-character stagger delay. Not a cycling component — pass a new `Word` when you have one, and
it replays automatically. **This is not `AtomTextCycle`**: no word list, no infinite loop — one
word, one animation, optionally repeatable.

### How it animates

Unlike `AtomTextCycle`, the `@keyframes` percentage breakpoints here are fixed (they don't depend
on word length — only the per-character *delay multiplier* does), so all 7 effects ship as static
scoped CSS — no per-instance `<style>` generation needed. Each character gets
`animation-delay: calc(var(--atom-text-scramble-stagger) * i)` for its index `i`.

### Replaying

The animation plays automatically on first render and again whenever `Word` changes — no trigger
is required for the common case. To replay the *same* word on demand (e.g. a "Repeat Animation"
button, matching the classic demo this component is based on), grab a component reference and call
`Replay()`:

```razor
<AtomTextScramble @ref="_scramble" Word="AWESOME" />
<button @onclick="() => _scramble.Replay()">Repeat Animation</button>

@code {
    private AtomTextScramble _scramble = default!;
}
```

Internally, both the automatic replay-on-change and `Replay()` work the same way: bumping an
internal counter used as the root `<span>`'s `@key`, forcing Blazor to tear down and rebuild the
character spans (rather than diff/patch them) — which is what restarts the CSS animation, with no
JS class-toggle trick needed.

### Effect

| Effect | Motion |
|---|---|
| `RevolveScale` *(default)* | Flies in from the upper-left, rotating and shrinking from an oversized start. |
| `BallDrop` | Drops in from the upper-right like a bouncing ball. |
| `SideSlide` | Slides in from the left, overshoots, settles with a color flash. |
| `RevolveDrop` | Spins down from above, unrolling into place. |
| `DropVanish` | Like `RevolveDrop`, but flings off to the upper-left mid-flight before settling. |
| `Twister` | Twists in from a rotated, offset start position. |
| `LeftRight` | Slides in from the left, overshoots past center with a color change, then settles. |

### Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Word` | `string` | *required* | The word/short phrase to animate in, one character at a time. Empty renders nothing. |
| `Effect` | `TextScrambleEffect` | `RevolveScale` | Which entrance animation each character plays — see above. |
| `StaggerDelay` | `string` | `"0.05s"` | Delay added per character index. Any CSS time. |
| `AnimationDuration` | `string` | `"0.5s"` | How long each character's own animation takes. Any CSS time. |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root `<span>`, and the public `Replay()` method described above.

## Notes

- **Zero JS.** Pure CSS `animation`/`@keyframes` — no `IJSObjectReference`, works identically in
  every render mode including static SSR. `AtomTextCycle`'s keyframes are generated server/
  client-side in C# (per-instance, sized to word count); `AtomTextScramble`'s are static (word
  length only affects the stagger multiplier, not the keyframe shape).
- **Accessibility.** Both components respect `prefers-reduced-motion: reduce` (animation disabled,
  final/first state shown immediately). `AtomTextCycle`'s duplicate wrap-around row is
  `aria-hidden`.
