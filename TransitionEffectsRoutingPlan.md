# Scaling past AtomTransition: routing 35+ future CSS effects

## Context

`BlazorAtoms.Transitions`'s `AtomTransition` ships with 20 effects (Fade/Pop/Slide×4/FlipY20 etc/
Bounce×4/Grow×4) — a generic wrapper that plays a CSS enter/exit animation around arbitrary child
content on a boolean `Show` toggle, backed by `BlazorAtoms.Behaviors.TransitionState`
(`@starting-style` CSS-native, JS `nextFrame()` fallback).

The idea list at `src/BlazorAtoms.Layout/transition-ideas.md` (10,000+ lines, too large to fully
process) has 35+ more CSS effects to add. Concern: `AtomTransition` becoming unmanageable — too
much responsibility in one component/enum/CSS file.

A 15-item sample from that list was reviewed against `AtomTransition`'s actual contract (generic
child wrapper + boolean `Show` + enter/exit classes) and against `src/LIBRARY-CATALOG.md`'s
existing roadmap:

- **Genuinely fits `AtomTransition`** (toggle-driven, generic wrapper): "flip/spin/etc. animations
  on show/hide/default" — just new enum members + CSS rules, same pattern as today's 20.
- **Everything else in the sample does NOT fit** — it's stateless hover/focus CSS (hover text
  effects, glowing dots, storm button), always-on loop animations (sparkly text, glowing loader
  ring, gradient/rainbow backgrounds, kinetic loading panel), one-shot click effects (fizzy
  buttons, click ripple, 3d button press), or — critically — **whole UI controls that don't exist
  yet** (toggle switch, CSS dropdown menu) which are already on the roadmap as their own
  components: `BlazorAtoms.Inputs.AtomSwitch`, `BlazorAtoms.Buttons.AtomButton` (Tier B, planned),
  `BlazorAtoms.Overlays.AtomDropdown` (Tier C, planned). Confirmed via `Glob` that no
  `AtomButton`/`AtomSwitch`/`AtomDropdown`/loader components exist yet in `src/*/Atom*.razor`.

The real risk isn't "too many entries in one enum" — it's conflating a **generic content wrapper**
with **skins/variants of specific, already-planned components**. Forcing a button's press effect
or a switch's morph animation through `AtomTransition`'s `Show`-boolean contract would be the wrong
shape (no boolean toggle exists for "user is hovering" or "animation loops forever"), and would
make `AtomTransition`'s CSS/enum balloon with effects nothing else about it applies to.

## Approach: route by behavioral contract, not visual theme

For each future effect, ask: **does it need a boolean Show/Hide wrapped around arbitrary child
content?**

- **Yes** → it belongs in `AtomTransition`. Add an `AtomTransitionEffect` member + a
  `.atom-transition-<name>` / `.atom-transition-<name>.atom-transition-shown` /
  `@starting-style` triplet in `AtomTransition.razor.css`, following the exact pattern already
  used for e.g. `FlipY20` (see `src/BlazorAtoms.Transitions/AtomTransition.razor.css:178-196`) and
  a bUnit case added to `tests/BlazorAtoms.Transitions.Tests/AtomTransitionTests.cs`'s
  `Effect_emits_class` theory. This family stays purely "enter/exit around content," so it can
  keep growing without changing shape — cost is one CSS rule-set + one enum member + one test row
  per effect, not new components.

- **No** → route it to the specific component it's a variant/skin of, per
  `LIBRARY-CATALOG.md`'s existing Tier A/B/C plan, as a `Variant`/`Effect`-style `[Parameter]` enum
  **on that component**, with the CSS scoped in *that component's* `.razor.css` — not
  `BlazorAtoms.Transitions`:
  - Text hover/sparkle/glow effects → `BlazorAtoms.Typography` (`AtomText`, planned) — a
    `TextEffect` enum param.
  - Button effects (fizzy, 3d press, gradient/rainbow border, storm, beveled, shape-change on
    click, click-ripple) → `BlazorAtoms.Buttons` (`AtomButton`, planned Tier B) — a
    `ButtonEffect`/`Variant` enum param; these are hover/`:active`/one-shot-on-click CSS, no
    `TransitionState` needed at all for most (stateless `:hover`/`:active` pseudo-classes; only
    click-ripple needs a tiny bit of C# to key a `key`-per-click CSS restart).
  - Loader effects (kinetic panel, glowing ring) → new members on
    `BlazorAtoms.ActivityIndicators` (already shipped, same "always-animating, no state" family as
    its existing `AtomActivityGears`/`AtomPulseBar`) or `BlazorAtoms.Progress` (planned) —
    whichever matches determinate vs indeterminate.
  - Toggle switch (with morph/"hole" animation) → `BlazorAtoms.Inputs.AtomSwitch` (planned Tier
    B) — the animation is that component's own enter/exit-on-value-change concern; can reuse
    `BlazorAtoms.Behaviors.TransitionState` directly (no markup wrapper needed — the README already
    calls this out as the intended reuse path) rather than wrapping in `<AtomTransition>`.
  - CSS dropdown menu (open/close + layout options) → `BlazorAtoms.Overlays.AtomDropdown` (planned
    Tier C) — a real positioned-overlay component (like `AtomDrawer`, not like `AtomTransition`);
    "layout/formatting" options become its own params, open/close can use `AtomTransition` or
    `TransitionState` internally for the actual reveal animation.
  - "Pure CSS drawer graphic" (decorative illustration, not `AtomDrawer`) — lowest priority, purely
    ornamental; defer until it's clear which package (if any) it belongs to — flag for the user to
    re-scope rather than guessing a home.
  - Image/overlay filter effects (zoom, ken-burns, grayscale-on-hover) — confirmed: these warrant a
    **dedicated image-aware component** (new package, e.g. `BlazorAtoms.Media` or a subfolder under
    an existing visual-content family), not `AtomTransition`'s generic `<div>` wrapper — only
    decide the package name once the actual effect list for images is in hand.

## Practical next step for triage

Don't try to pre-sort all 35+ up front from names alone. When ready to implement a batch: for each
effect, name (a) the trigger (toggle / hover-focus / loop / one-shot-click) and (b) the element
type it's themed around (generic content / button / text / switch / dropdown / image / loader).
That's the only information needed to route it using the table above — most items will land on an
already-planned Tier A/B/C component instead of needing a new home invented on the spot.

## Critical files
- `src/BlazorAtoms.Transitions/AtomTransitionEffect.cs`, `AtomTransition.razor.css`,
  `tests/BlazorAtoms.Transitions.Tests/AtomTransitionTests.cs` — where genuine toggle/enter-exit
  additions go (pattern to copy: existing `FlipY20` family).
- `src/LIBRARY-CATALOG.md` — the authoritative map of which package owns which future component;
  update its Tier B/C rows as effects get assigned homes so the routing decision isn't re-litigated
  each time.
- `src/BlazorAtoms.Layout/transition-ideas.md` — the raw idea dump this plan routes from; process
  it in small batches (grep/read a section at a time), never all at once.
- New packages only get created when a batch of routed effects actually needs one that doesn't
  exist yet (e.g. the image-effects component) — don't scaffold packages speculatively.

## Verification
No code changes in this plan — it's a routing/design decision. Verification is: when the first
real batch of effects is implemented under this scheme, confirm (1) `AtomTransition`'s enum/CSS
only grew for genuine toggle-driven additions, (2) every non-toggle effect landed as a param on its
themed component per `LIBRARY-CATALOG.md`, not bolted onto `BlazorAtoms.Transitions`, and (3)
`dotnet test` still passes for whichever project(s) changed.
