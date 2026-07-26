# BlazorAtoms.Transitions

Reusable enter/leave and hover transitions for Blazor. Ships:

- **`AtomTransition`** — a generic wrapper that plays a CSS transition (fade, pop, slide, flip,
  ...) around arbitrary child content whenever `Show` toggles.
- **`AtomHoverEffect`** — a generic wrapper that plays a hover-triggered effect (sparkle, ...)
  around arbitrary child content. Unlike `AtomTransition`, the trigger is plain CSS
  `:hover`/`:active` — no C# state, no toggle parameter.
- **`AtomHoverGlow`** — wraps several children (any elements, not just links) and glows whichever
  one is currently hovered/focused, sliding between them. Pure CSS anchor positioning where
  supported (Chromium today); a JS fallback reproduces the same effect on Firefox/Safari.

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

## AtomHoverGlow

```razor
<AtomHoverGlow>
    <a href="/">Home</a>
    <a href="/about">About</a>
    <a href="/contact">Contact</a>
</AtomHoverGlow>

<AtomHoverGlow GlowColor="#22d3ee" GlowRadius="0.75rem">
    <div class="card">Card 1</div>
    <button>Card 2</button>
</AtomHoverGlow>
```

A soft glow follows whichever **direct child** is currently hovered or contains focus, sliding
between them — the classic nav-menu "active tab" indicator, generalized to wrap any elements
(links, buttons, cards, whatever), not just `<a>` tags.

### How it works — and the one real caveat

The primary path is pure CSS **anchor positioning** (`anchor-name`/`position-anchor`/`anchor()`):
whichever direct child currently matches `:hover`/`:focus-within` claims a shared anchor name via
a plain selector (`.atom-hover-glow > :is(:hover, :focus-within) { anchor-name: ... }`); the glow
indicator's `position-anchor` always points at that name, so the browser resolves "wherever that
is right now" itself — no JS, no enumerating children, works for any number of them.

**Anchor positioning is Chromium-only today** (not yet in Firefox or Safari). On browsers without
it, the component lazy-imports a small fallback JS module (`atom-hover-glow.js`) from
`OnAfterRenderAsync`, detected via `BlazorAtoms.Behaviors.AtomBrowserSupport`'s
`CSS.supports('anchor-name', ...)` check (the same capability-detection helper `AtomTransition`
uses for `@starting-style`). The fallback uses event delegation on the container (`mouseover`/
`focusin`) to find the hovered direct child and positions the indicator with plain
`getBoundingClientRect()` math — same visual result, different mechanism. Supporting browsers
never fetch this module at all.

### Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | The wrapped content — any elements; the glow tracks whichever direct child is hovered/focused. |
| `GlowColor` | `string` | `"#ff1493"` | Color of the glow. |
| `GlowBlur` | `string` | `"32px"` | Blur radius of the glow's outer box-shadow. Any CSS length. |
| `GlowRadius` | `string` | `"0.5rem"` | Corner radius of the glow indicator — tune to roughly match your children's own shape. |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root `<div>`.

## Notes

- **Dependency.** This library carries a real reference to `BlazorAtoms.Behaviors` (for the
  CSS-native/JS-fallback capability check) — the one deliberate exception to the rest of the
  family's "0 BlazorAtoms deps" rule; see `src/LIBRARY-CATALOG.md`.
- **Render modes.** JS interop can't run during static SSR/prerender — the wrapper renders in its
  closed state first and resolves the real capability once interactive.
- **Future components.** `BlazorAtoms.Behaviors.TransitionState` is the reusable engine behind
  `AtomTransition` — a future component (carousel, text animation, image effect) can drive one
  directly without wrapping content in `<AtomTransition>`'s markup.
