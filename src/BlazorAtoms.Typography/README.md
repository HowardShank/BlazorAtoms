# BlazorAtoms.Typography

Text primitives for Blazor. Ships:

- **`AtomTextCycle`** — a zero-JS flip-cascade word rotator: cycles through a list of
  words/phrases in an infinite loop, one at a time, sliding (4 directions) or spinning in place
  fast-to-slow before landing upright (2 directions).
- **`AtomTextScramble`** — a zero-JS one-shot entrance animation for a single word, splitting it
  into characters that fly/drop/spin in with a staggered delay (7 effects). Not a cycling
  component like `AtomTextCycle` — deliberately single-word/specialized.
- **`AtomTextLava`** — a zero-JS single word rising up out of an animated molten-lava-gradient
  background. `Loop` (default on) makes it bubble up and down forever; off, it rises once and holds.
- **`AtomTextSparkle`** — a zero-JS hover effect: layered 3D text-shadow, a colorized glare sweep,
  and SVG sparkles that pop in around the text. Pure CSS `:hover`/`:active` — the only Typography
  component whose trigger needs no C# state at all.

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

## AtomTextLava

```razor
<AtomTextLava Word="MOLTEN" />
<AtomTextLava Word="STEADY" Loop="false" />
```

A single word rising up out of an animated molten-lava-gradient background. The lava background
always loops — it's ambient, not tied to the word's own state. `Loop` (default `true`) controls
only whether the word itself keeps bubbling up and down forever, or rises once and holds.

Both trigger modes reuse the exact same `@keyframes` — `Loop` just flips
`animation-iteration-count`/`animation-direction`/`animation-fill-mode` on the word:

- `Loop="true"` *(default)*: `iteration-count: infinite; direction: alternate` — plays forward
  (rise to rest) then backward (sink back below) forever.
- `Loop="false"`: `iteration-count: 1; direction: normal; fill-mode: forwards` — rises once from
  below and holds at rest.

### Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Word` | `string` | *required* | The word/short phrase rising out of the lava. Empty renders nothing. |
| `Loop` | `bool` | `true` | Bubble up/down forever, or rise once and hold. |
| `RiseDistance` | `string` | `"1.5rem"` | How far below rest the word starts (and, when looping, sinks back to). Any CSS length. |
| `Duration` | `string` | `"1.2s"` | How long one rise (or rise/sink half-cycle) takes. Any CSS time. |
| `GlowColor` | `string` | `"#ff5500"` | Color of the heat-glow text-shadow. |
| `BgColorHot` | `string` | `"#ff6a00"` | Color of the hotter of the two radial-gradient lava blobs. |
| `BgColorCool` | `string` | `"#ff2d00"` | Color of the cooler of the two radial-gradient lava blobs. |
| `BgColorBaseDark` | `string` | `"#3a0a00"` | Darker end (top) of the base linear-gradient behind the blobs. |
| `BgColorBaseLight` | `string` | `"#1a0500"` | Lighter end (bottom) of the base linear-gradient behind the blobs. |

Plus the public `Replay()` method — same `@key`-remount trick `AtomTextScramble` uses — to rerun
the rise from its initial state on demand, regardless of `Loop`.

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root `<span>`.

## AtomTextSparkle

```razor
<AtomTextSparkle Text="Click!" Href="/somewhere" />
<AtomTextSparkle Text="Sparkly Shiny Text" SparkleCount="8" Color="#e879f9" GlareColor="#fff" />
```

A hover effect, not a toggle/loop/one-shot one — the only Typography component whose trigger is
pure CSS `:hover`/`:active` with no C# state behind it at all. Renders a real `<a href>` when
`Href` is set; otherwise a focusable (`tabindex="0"`) non-link element with the identical hover
effect. On hover: a layered 3D text-shadow lifts the text, a colorized glare sweep animates across
a clipped-text overlay, and `SparkleCount` SVG sparkles pop in at scattered positions around it.

Sparkle positions are placed by a pure function of index (`x = i*53 % 100`, etc.), not
`System.Random` — a time-seeded random would scatter sparkles differently between the
server-rendered markup and the first interactive re-render, causing a visible jump on hydration; a
deterministic function of the index can't.

### Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Text` | `string` | *required* | The text to display. Empty renders nothing. |
| `Href` | `string?` | `null` | Optional link target — renders `<a href>` when set, a focusable non-link otherwise. |
| `Color` | `string` | `"#eab308"` | Fill color of the glare-sweep text layer. |
| `ShadowColor` | `string` | `"#a16207"` | Color of the layered 3D text-shadow. |
| `GlareColor` | `string` | `"hsl(0 0% 100% / 0.75)"` | Color of the glare sweep and the sparkle SVGs. |
| `SparkleCount` | `int` | `5` | How many sparkle SVGs scatter around the text. |
| `FontSize` | `string` | `"1.5rem"` | Text size — sparkle size and shadow depth scale off this (`em`-based). |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root element.

## Notes

- **Zero JS.** Pure CSS `animation`/`@keyframes` — no `IJSObjectReference`, works identically in
  every render mode including static SSR. `AtomTextCycle`'s keyframes are generated server/
  client-side in C# (per-instance, sized to word count); `AtomTextScramble`'s, `AtomTextLava`'s,
  and `AtomTextSparkle`'s are static (their keyframe shapes never depend on instance data).
  `AtomTextSparkle`'s trigger is plain CSS `:hover`/`:active` — it needs no C# state at all, unlike
  the others' click/loop-driven triggers.
- **Accessibility.** All four components respect `prefers-reduced-motion: reduce` (animation
  disabled, final/first state shown immediately). `AtomTextCycle`'s duplicate wrap-around row is
  `aria-hidden`; `AtomTextSparkle` without `Href` still gets `tabindex="0"` so keyboard users can
  focus and trigger `:focus`-adjacent styling if you add it via `CssClass`.
