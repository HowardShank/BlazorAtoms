# BlazorAtoms.ActivityIndicators — development notes

Internal design rationale and implementation details for maintainers of this library.
For public, consumer-facing usage documentation (install, parameters, theming, examples),
see `README.md` in this same folder — this document does not repeat that content.

---

## Package layout

```
BlazorAtoms.ActivityIndicators/
  ActivityIndicator.razor / .razor.cs    <- the wrapper (this is what you usually use)
  ILLink.Descriptors.xml                 <- WASM trim-safety (see "Trim-safety under WASM")
  Indicators/
    ActivityFunnel.razor      ActivityFunnel.razor.css
    ActivitySwarm.razor       ActivitySwarm.razor.css
    ActivityMagnifier.razor   ActivityMagnifier.razor.css
    ActivityGears.razor       ActivityGears.razor.css
    ActivityNeural.razor      ActivityNeural.razor.css
    ActivityHourglass.razor   ActivityHourglass.razor.css
    ActivityDna.razor         ActivityDna.razor.css
    PulseBar.razor                                 <- linear (slider) form-factor, used directly
    PulseScanner.razor                             <- linear (slider) form-factor, used directly
```

Each round `.razor` holds the SVG geometry (inlined so host CSS can reach it); each
`.razor.css` is the component's **scoped** stylesheet holding the animation keyframes, the
variable wiring, and the built-in default palette. The linear `Pulse*` indicators have no
`.razor.css` — they are self-contained, emitting their own scoped `<style>` inline.

---

## How wrapper discovery works

**Discovery is by convention** — the wrapper reflects over its own assembly for
non-abstract `ComponentBase` types in the `…ActivityIndicators.Indicators` namespace whose
name starts with `Activity`, cached once per process. The naming convention IS the
registry: a library maintainer can **add or remove an `Activity*.razor`** in `Indicators/`
and the wrapper picks it up on the next build with **no code change**. (Downstream
consumers don't register their own — the indicator set is what the package provides.)

This is why the linear `Pulse*` indicators are excluded from the wrapper's pool by design —
they don't match the `Activity*` naming convention the reflection filter looks for, since
each has its own rich, divergent parameter set that a shared picker couldn't carry.

**Parameter forwarding is filtered** — the indicators don't all declare the same params
(`ActivitySwarm` has no `Line`/`Fill`; `ActivityNeural` has no `Fill`). The wrapper inspects
the chosen indicator's declared `[Parameter]` members and forwards only the ones that
actually exist there, so passing an unsupported one is silently ignored rather than
throwing.

---

## Why the SVG is inlined (not `<img>`)

CSS variables and `currentColor` only reach an SVG when its markup is part of the DOM.
These components inline the SVG directly in the `.razor`, so the host page can theme them.
(An `<img src="*.svg">` is sandboxed — external CSS cannot get in — which is why that route
is **not** themeable.) This is the reason consumer theming via CSS custom properties works
at all, and why it must stay this way if the components are ever refactored.

---

## The token → fallback CSS-variable chain

Every colored part reads its color from a public token, falling back to a private,
scheme-aware default:

```css
.drop { fill: var(--ind-blip, var(--ind-blip-d)); }
/*                ^public        ^private default  */
```

- **`--ind-blip`** — the *public* knob. Consumers set it; it is never declared by the
  component, so it inherits freely from any ancestor.
- **`--ind-blip-d`** — the *private* default, declared by the component on the `svg`
  element and swapped by a `prefers-color-scheme` media query.

So: if the public token is set, it wins. If not, the default is used — and that default
auto-darkens on light OS themes.

### Built-in light/dark defaults
```css
svg { --ind-blip-d:#6fe3e3; /* ...dark-scheme palette... */ }
@media (prefers-color-scheme: light) {
  svg { --ind-blip-d:#0e9c9c; /* ...darker palette for light backgrounds... */ }
}
```

Every one of the four tokens (`--ind-blip`, `--ind-glow`, `--ind-line`, `--ind-fill`)
follows this same public/private pair pattern. Keep the pattern consistent if a new token
or component is added.

---

## Trim-safety under WASM (`ILLink.Descriptors.xml`)

The indicators and the wrapper are render-mode agnostic — pure SVG + scoped CSS, no JS, no
DI — so functionally they work unchanged under `InteractiveServer`, `InteractiveWebAssembly`,
and `InteractiveAuto`.

The one WASM-specific concern is **trimming**: the wrapper discovers indicators by
reflection, which the IL linker can't see statically, so under a trimmed/AOT WASM publish
the `Activity*` types would otherwise be stripped as "unreferenced." The package ships an
embedded `ILLink.Descriptors.xml` that roots the `…Indicators` namespace, so discovery
survives trimming and the indicator set stays expandable without needing consumers to
maintain their own linker config. Blazor Server never trims, so the descriptor is a no-op
there; trimmed WASM consumers get it automatically via the package reference.

---

## Maintainer implementation notes (editing component source)

- **`transform-box: fill-box`** — used throughout so `transform-origin: center` resolves to
  each element's *own* box (not the SVG viewport); required for in-place scaling/pivoting.
  Don't remove it when touching the scoped `.razor.css` files.
- **Culture safety** — components with fractional coordinates or delays (`ActivitySwarm`,
  `ActivityMagnifier`, `ActivityDna`, and the neural/hourglass delays) store those values as
  **strings** so they render verbatim. A `double` like `82.5` would emit `82,5` under a
  comma-decimal server culture and break the SVG. Keep them as strings if you edit the
  geometry/timing.
