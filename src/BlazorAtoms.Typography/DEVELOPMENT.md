# BlazorAtoms.Typography — Development Notes

Internal architecture notes for maintainers. See `README.md` for consumer-facing usage.

## AtomTextCycle — per-instance generated `@keyframes`

The loop needs `N` roughly-equal hold-then-transition segments, one per word — the exact
percentage breakpoints (`100/N`, `200/N`, ...) depend on `Words.Count`, which is instance data. A
static scoped `.razor.css` can't express that (its rules are fixed at build time), so
`AtomTextCycle.razor.cs`'s `BuildKeyframesCss(n, slideRatio, effect, spinTurns)` builds the
`@keyframes atom-text-cycle-{v|h|spin}-n{N}` text in `OnParametersSet` and the `.razor` file emits
it as a plain inline `<style>` element — the same "component brings its own small stylesheet"
pattern `AtomHighlight.razor` already uses for its `<link>`. Two instances with the same motion
kind, word count, and (for Spin) `SpinTurns` share an identical, harmless duplicate `<style>`
block; nothing keys or dedupes across instances since the generated CSS is pure and
side-effect-free either way. The keyframe name is motion-kind-qualified (`-v-`/`-h-`/`-spin-`)
specifically so instances of different kinds with the same word count don't emit two rules under
the *same* name — the browser would silently keep only the last one parsed, breaking whichever
instance lost.

### `Effect` — reverse via `animation-direction`, not a second keyframe generator

`TextCycleEffect` has 6 values but only 3 real motion kinds (vertical slide, horizontal slide,
spin — and spin is really just vertical slide plus a `rotate()` riding along, see below). Rather
than generating a second set of keyframes for the "opposite" direction on each kind,
`SlideTopToBottom`/`SlideLeftToRight`/`SpinCounterClockwise` just play the *same* generated
keyframes with `animation-direction: reverse` (see `IsReversed` in `AtomTextCycle.razor.cs`).
Reversing playback time reverses the spatial/rotational direction (a segment that moves from `-i`
to `-(i+1)` forward now visibly moves from `-(i+1)` to `-i`) and, as a side effect, the order words
are visited in. That's an accepted trade-off — "reverse the whole loop" is far simpler and more
robust than threading a second duplicate-row/rotation scheme through the generator, and the
direction is what the parameter promises, not a guaranteed word-visit order. The
duplicate-first-word wrap-around trick still works symmetrically under reverse playback: the
100%↔0% boundary still shows the same content on both sides of the loop point either way.

### Why every effect needs a duplicate first-word row

The track only ever moves in one direction (`translateY`/`translateX` grows more negative). After
showing the last word, the animation's `100%` keyframe must jump back to `0%` to loop — normally a
visible snap. Rendering `Words.Count + 1` rows/columns, where the last is a duplicate of
`Words[0]`, means the frame right before the wrap (the `Nth` one, the duplicate) is pixel-identical
to the frame right after it (the `0th`, the real first word) — so the "jump" is imperceptible. The
duplicate item is `aria-hidden` since it's a pure visual convenience, not new content.

### Why the keyframes don't depend on `ItemHeight`/`ItemWidth`

Each step's `transform` reads `calc(var(--atom-text-cycle-item-height|width) * -{i})` rather than a
literal length — the CSS custom property is set on the track's inline `style` from the
`ItemHeight`/`ItemWidth` parameter. This means the generated `@keyframes` block only needs to
change when `Words.Count` (or, for Spin, `SpinTurns`) changes, not when the item size does, so
instances with different sizes but the same word count still safely share one rule.

## Spin's design history: three wrong shapes before the right one

Worth recording in full — each attempt was plausible, each was visually wrong in a specific way,
and the eventual fix came directly from the requester describing (and pasting a minimal repro of)
what "spinning" actually meant to them.

1. **`rotateX` (pivoting around the horizontal axis), a 3D drum.** Reads as a flip-clock digit
   tipping up/down — foreshortening vertically as it turns — visually indistinguishable from a
   vertical slide/fade, not a "propeller spinning" look.
2. **`rotateY` 3D drum (pivoting around the vertical axis, plus `translateZ` + `perspective`,
   items facing the viewer via `backface-visibility: hidden`).** A real 3D turn, but with the
   component's typical `Perspective`/`ItemWidth` ratio the depth cue was too subtle — it read as a
   plain horizontal slide with a slight squish, not a visible "turn."
3. **Plain 2D `rotate()` pinwheel — every word a "blade" radiating from a shared hub, all visible
   at once, spinning as one rigid assembly.** This matched "propeller" as a literal object (a real
   propeller does show all its blades together), but the actual ask — confirmed once the requester
   pasted a minimal repro (a single `<div>` with `animation: spin 1s linear infinite; rotate(0deg)
   → rotate(360deg)`) — was much simpler: **one word, sitting in a normal horizontal reading line,
   spinning in place and landing upright**, not multiple words permanently visible at different
   angles around a hub.
4. **Current: Spin is vertical slide plus a `rotate()` riding on the same transform.** No hub, no
   radius, no per-item static transform, no dedicated axis CSS at all — `AxisClass` returns
   `"atom-text-cycle-axis-v"` for Spin too, and it needs the same duplicate-first-word row vertical
   slide does (a bare `rotate()` alone would loop seamlessly at 360°, but this design's seam is
   `translateY`-driven like vertical slide, so it needs the same trick). The only difference from
   plain vertical slide is that `BuildKeyframesCss`'s `ValueAt(step)` appends
   `rotate({-step * 360 * spinTurns}deg)` to the same `transform` as the `translateY(...)` — always
   a multiple of `360 * SpinTurns` (so every *hold* is visually upright, `Easing="ease-out"` gives
   the fast-then-slowing-to-a-stop feel), while each *step-to-step* delta is a genuine
   `360 * SpinTurns`-degree turn, giving the requested "spins fast, slows, lands in the correct
   reading position" motion riding along on the already-proven, single-word-visible-at-a-time
   reveal mechanism.

Net effect on `AtomTextCycle.razor.css`: Spin needs **no CSS of its own** — attempts 2 and 3's
`transform-style: preserve-3d` / `perspective` / `backface-visibility` / `position: absolute` /
custom `overflow: visible` rules are all gone. It's just the vertical-axis rules, unchanged.
