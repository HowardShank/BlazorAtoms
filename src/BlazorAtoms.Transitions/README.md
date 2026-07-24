# BlazorAtoms.Transitions

Reusable enter/leave and hover transitions for Blazor. Ships:

- **`AtomTransition`** — a generic wrapper that plays a CSS transition (fade, pop, slide, flip,
  ...) around arbitrary child content whenever `Show` toggles.
- **`AtomHoverEffect`** — a generic wrapper that plays a hover-triggered effect (sparkle, ...)
  around arbitrary child content. Unlike `AtomTransition`, the trigger is plain CSS
  `:hover`/`:active` — no C# state, no toggle parameter.

Unlike an overlay component (e.g. `AtomDrawer`), the wrapped element stays mounted permanently —
visibility is a pure CSS class toggle. On browsers that support `@starting-style`, the very first
appearance animates with zero JS; elsewhere a small JS fallback (via `BlazorAtoms.Behaviors`)
replicates the same effect. Every toggle *after* the first render animates in any browser with no
JS involved either way.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Transitions\BlazorAtoms.Transitions.csproj" />
```
```razor
@using BlazorAtoms.Transitions
```

## AtomTransition

```razor
<button @onclick="() => _show = !_show">Toggle</button>

<AtomTransition Show="_show" Effect="AtomTransitionEffect.SlideUp">
    <div class="card">Some content that fades/slides in and out.</div>
</AtomTransition>

@code {
    private bool _show;
}
```

### Effects

`Fade`, `Pop`, `FadeScale`, `SlideUp`, `SlideDown`, `SlideLeft`, `SlideRight`, `ShiftBlur`,
`FlipY20`, `FlipYNeg20`, `FlipX20`, `FlipXNeg20`, `BounceUp`, `BounceDown`, `BounceLeft`,
`BounceRight`, `GrowLeft`, `GrowRight`, `GrowTop`, `GrowBottom`.

### Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Show` | `bool` | `false` | Toggles the shown/hidden state. |
| `Effect` | `AtomTransitionEffect` | `Fade` | Which enter/exit animation to play. |
| `Duration` | `int` | `240` | Milliseconds, → `--atom-transition-duration`. |
| `OnEntered` | `EventCallback` | — | Fires after entering (best-effort, not tied to `transitionend`). |
| `OnExited` | `EventCallback` | — | Fires after exiting (same caveat). |
| `ChildContent` | `RenderFragment?` | `null` | Content to show/hide. |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root `<div>`.

## AtomHoverEffect

```razor
<AtomHoverEffect Href="/somewhere">
    <span>Click!</span>
</AtomHoverEffect>

<AtomHoverEffect GlowColor="#e879f9" SparkleCount="8" ScaleAmount="1.15">
    <img src="icon.png" alt="" />
</AtomHoverEffect>
```

A generic hover-effect wrapper — matches `AtomTransition`'s "wraps arbitrary `ChildContent`" shape,
but the trigger is plain CSS `:hover`/`:active` rather than a `Show` boolean, so there's no C#
state behind it at all. Renders a real `<a href>` when `Href` is set; otherwise a focusable
(`tabindex="0"`) non-link element with the same hover effect. `Effect` picks the treatment —
`Sparkle` (the only member so far) scales the content up slightly, adds a colored glow, and pops
`SparkleCount` SVG sparkles in at scattered positions around it.

This can't reuse `AtomTextSparkle`'s (`BlazorAtoms.Typography`) layered 3D text-shadow/glare-sweep
trick — that only works because it clips a gradient to text glyphs. For arbitrary content, the
hover treatment is a `filter: drop-shadow(...)` glow plus a `transform: scale(...)` instead.

Sparkle positions are placed by a pure function of index, not `System.Random` — a time-seeded
random would scatter sparkles differently between server-rendered and first interactive markup,
causing a visible jump on hydration.

### Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | The content the effect wraps — any element. |
| `Effect` | `HoverEffect` | `Sparkle` | Which hover treatment to play. |
| `Href` | `string?` | `null` | Optional link target — renders `<a href>` when set, a focusable non-link otherwise. |
| `GlowColor` | `string` | `"#eab308"` | Color of the glow and sparkle SVGs. |
| `SparkleCount` | `int` | `5` | How many sparkle SVGs scatter around the content. Ignored for effects that don't use sparkles. |
| `ScaleAmount` | `double` | `1.05` | How much the content scales up on hover. |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root element.

## Notes

- **Dependency.** This library carries a real reference to `BlazorAtoms.Behaviors` (for the
  CSS-native/JS-fallback capability check) — the one deliberate exception to the rest of the
  family's "0 BlazorAtoms deps" rule; see `src/LIBRARY-CATALOG.md`.
- **Render modes.** JS interop can't run during static SSR/prerender — the wrapper renders in its
  closed state first and resolves the real capability once interactive.
- **Future components.** `BlazorAtoms.Behaviors.TransitionState` is the reusable engine behind
  `AtomTransition` — a future component (carousel, text animation, image effect) can drive one
  directly without wrapping content in `<AtomTransition>`'s markup.
