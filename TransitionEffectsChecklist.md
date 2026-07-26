# Transition-effects routing checklist

Tracking doc for `TransitionEffectsRoutingPlan.md`. Check off each package/component once it's
scaffolded and receiving its routed effects. Not all of these need to happen at once — build in
the order effects actually get triaged from `src/BlazorAtoms.Layout/transition-ideas.md`.

## Already shipped — extend in place

- [ ] `BlazorAtoms.Transitions` / `AtomTransition` — add genuine toggle-driven enter/exit effects
      only (e.g. more flip/spin/slide variants). Pattern: `AtomTransitionEffect.cs` +
      `AtomTransition.razor.css` + a row in `AtomTransitionTests.cs`'s `Effect_emits_class` theory.
- [ ] `BlazorAtoms.ActivityIndicators` — add loop/ambient loader effects here (glowing ring,
      kinetic panel) if indeterminate; matches existing `AtomActivityGears`/`AtomPulseBar` family.

## New components — planned in `src/LIBRARY-CATALOG.md`, not yet built

- [ ] `BlazorAtoms.Buttons` → `AtomButton` — routes: fizzy, 3d press, gradient/rainbow border,
      storm button, beveled, shape-change-on-click, click-ripple. Mostly stateless
      `:hover`/`:active` CSS; only ripple needs a little C#.
- [ ] `BlazorAtoms.Inputs` → `AtomSwitch` — routes: toggle switch with morph/"hole" animation.
      Reuse `BlazorAtoms.Behaviors.TransitionState` directly (no `<AtomTransition>` wrapper needed).
- [ ] `BlazorAtoms.Overlays` → `AtomDropdown` — routes: CSS dropdown menu (open/close + layout
      options). Real positioned overlay, same shape as `AtomDrawer`; can use `AtomTransition`/
      `TransitionState` internally for the reveal animation.
- [x] `BlazorAtoms.Typography` → **`AtomTextCycle`** *(shipped)* — routes: "simple text animation"
      (vertical word-flip loop). Zero-JS, per-instance-generated `@keyframes` sized to word count.
      Playground: `/playground/textcycle`. Still open in this library: `AtomText` for hover text
      effects, sparkly shiny text, and other text-specific hover/loop animations.
- [x] `BlazorAtoms.Typography` → **`AtomTextScramble`** *(shipped)* — routes: "pure CSS text
      animation" (per-character fly/drop/spin-in demo, 7 effects). Different trigger contract from
      `AtomTextCycle`: single word, one-shot (auto-plays on mount/word-change), optional manual
      replay via a public `Replay()` method — not a Words-list infinite loop, and not a
      toggle-driven `Show` like `AtomTransition` either. Zero-JS: static scoped CSS (keyframe %
      breakpoints don't depend on runtime data here, only the per-character stagger multiplier
      does), `@key`-based remount replaces the demo's jQuery class-toggle restart trick.
      Playground: `/playground/textscramble`.
- [x] `BlazorAtoms.Typography` → **`AtomTextLava`** *(shipped)* — routes: "molten lava effect that
      text rises from." Single word (matches `AtomTextScramble`'s specialized shape), default
      trigger is loop (bubbles up/down forever), `Loop="false"` opts into a one-shot rise-and-hold.
      Scope includes the lava background visual, not just the text motion. Zero-JS: one
      `@keyframes` block reused for both trigger modes via `animation-direction`/
      `iteration-count`/`fill-mode` alone (no second keyframe generator, no per-instance CSS).
      Playground: `/playground/textlava`.
- [x] `BlazorAtoms.Typography` → **`AtomTextSparkle`** *(shipped)* — routes: "sparkly shiny text"
      (hover-triggered layered 3D text-shadow + glare sweep + SVG sparkles). Trigger is hover-focus,
      element is generic text/link — the first Typography component whose trigger is pure CSS
      `:hover`/`:active` with no C# state at all (cheaper even than `AtomTextScramble`'s
      `@key`-remount). Colorization via `Color`/`ShadowColor`/`GlareColor`; sparkle scatter count
      via `SparkleCount`, placed by a pure function of index (not `System.Random`, to avoid a
      hydration-mismatch jump between server-rendered and interactive markup). `Href` optional —
      renders a real `<a>` when set, a focusable non-link otherwise.
      Playground: `/playground/textsparkle`.
- [x] `BlazorAtoms.Transitions` → **`AtomHoverEffect`** *(shipped)* — generic-wrapper sibling of
      `AtomTransition` (same "arbitrary `ChildContent`" shape) but hover-triggered rather than
      `Show`-toggled: pure CSS `:hover`/`:active`, zero C# state. Added after `AtomTextSparkle`
      turned out to be the wrong shape for the actual ask — the requester wanted a wrapper around
      *any* element, not a fixed `Text` string, so `AtomTextSparkle` (text-specific, kept as-is)
      and `AtomHoverEffect` (generic, new) both ship. `HoverEffect` enum (`Sparkle` first member)
      is the extensible family — future hover effects add enum members here, same pattern as
      `AtomTransitionEffect`. Playground: `/playground/hovereffect`.
- [x] `BlazorAtoms.Transitions` → **`AtomHoverGlow`** *(shipped)* — routes: "nav active-tab glow
      that follows the hovered item," generalized from an `<a>`-only demo to wrap *any* direct
      children (per explicit ask). Trigger is hover-focus, but structurally different from
      `AtomHoverEffect`/`AtomTextSparkle`: tracks a *group* of children and glows whichever one is
      active, not a single wrapped item. Primary path is pure CSS anchor positioning
      (`anchor-name`/`position-anchor`/`anchor()`) — **Chromium-only today, no Firefox/Safari** —
      with an explicit, user-approved JS fallback (`atom-hover-glow.js`, event delegation +
      `getBoundingClientRect()`) for browsers without it, detected via
      `BlazorAtoms.Behaviors.AtomBrowserSupport`. The one hover-effect component in this family
      that isn't zero-JS on every browser. Playground: `/playground/hoverglow`.
- [x] `BlazorAtoms.Progress` → **`AtomScrollProgressBar`** *(shipped)* — routes: "scroll reading
      progress bar." New trigger category for this family: scroll-driven/continuous, not
      toggle/hover/loop/click. First member of the previously-empty `BlazorAtoms.Progress` package.
      Primary path is a pure CSS scroll-driven animation (`animation-timeline: scroll()`) —
      **Chromium-only today** — with a user-approved JS fallback (`atom-progress.js`, scroll/resize
      listener) for other browsers. Unlike `AtomHoverGlow`'s fallback, this one does its own inline
      `CSS.supports` check rather than referencing `BlazorAtoms.Behaviors.AtomBrowserSupport` — a
      second package with that dependency would turn "one deliberate exception" into a pattern, and
      a single-use inline check is only 3 lines. Playground: `/playground/scrollprogress`. Still
      open in this library: AtomProgressBar (determinate `Value`), AtomProgressRing,
      AtomProgressSteps, AtomMeter — also open: alternative home for loader effects if determinate
      rather than indeterminate (decide per-effect against `ActivityIndicators` above).

## Needs a new package (not yet in the catalog)

- [ ] Image/overlay effects (zoom, ken-burns, grayscale-on-hover, etc.) — dedicated image-aware
      component confirmed needed (not `AtomTransition`'s generic wrapper). Name TBD (e.g.
      `BlazorAtoms.Media`) — decide once the actual image-effect list is triaged.

## Deferred / needs re-scoping with user

- [ ] "Pure CSS drawer graphic" (decorative illustration, not `AtomDrawer`) — no clear package fit
      yet; lowest priority, purely ornamental.

## Process reminder

`transition-ideas.md` is 10,000+ lines — triage in small batches, not all at once. For each effect
before implementing: name (a) its trigger — toggle / hover-focus / loop / one-shot-click — and (b)
the element type it's themed around. That's enough to route it via
`TransitionEffectsRoutingPlan.md`'s table without re-deriving the decision each time.
