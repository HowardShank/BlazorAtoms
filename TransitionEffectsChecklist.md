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
- [ ] `BlazorAtoms.Progress` — alternative home for loader effects if determinate rather than
      indeterminate (decide per-effect against `ActivityIndicators` above).

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
