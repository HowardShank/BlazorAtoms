# BlazorAtoms.ActivityIndicators

Self-contained, pure-CSS animated SVG **activity / activity indicators** for Blazor —
**Server or WebAssembly**. No JavaScript, no dependencies, transparent background,
seamless loop. Ships as a Razor Class Library (RCL).

[![.NET](https://github.com/HowardShank/BlazorAtoms/actions/workflows/dotnet.yml/badge.svg)](https://github.com/HowardShank/BlazorAtoms/actions/workflows/dotnet.yml)

Two **form-factors**:
- **Round / compact** — seven interchangeable square indicators with a uniform contract,
  fronted by the `<ActivityIndicator />` wrapper (render a named one, or a random one).
- **Linear / slider** — wide bar/scanner indicators that slide back and forth. Each has its
  own rich, divergent parameter set, so these are **used directly** (not via the wrapper).
  See [Linear / slider indicators](#linear--slider-indicators).

| Component | Visual | Reads as |
|-----------|--------|----------|
| `ActivityFunnel` | Particles pour into a funnel; most fall through, a couple catch on the walls and glow before releasing. | *sifting / filtering / matching* |
| `ActivitySwarm`  | Scattered dots drift inward to form a ring, hold, disperse, and reform while slowly swirling. | *gathering / assembling an answer* |
| `ActivityMagnifier` | A lens orbits a field of items; each item magnifies and glows as the lens passes over it. | *searching / scanning* |
| `ActivityGears` | Two meshing gears turn in opposite directions at coupled speeds. | *working / processing* |
| `ActivityNeural` | Pulses travel left-to-right across a small layered network, firing each node as they arrive. | *thinking / computing* |
| `ActivityHourglass` | Sand drains from the top chamber to the bottom, then the glass flips. | *waiting / time-bound work* |
| `ActivityDna` | Two dot strands rotate as a double helix with base-pair rungs. | *deep analysis* |

All motion is CSS `@keyframes`; theming is done with CSS custom properties. **Every
indicator shares the same `--ind-*` token model and the same parameter set**, so the
theming examples below apply uniformly.

> Looking for internal design rationale (why reflection-based discovery, trim-safety
> internals, CSS-variable fallback chain, etc.)? See `DEVELOPMENT.md` in this folder.

---

## Install

1. Reference the library — NuGet:
   ```xml
   <PackageReference Include="BlazorAtoms.ActivityIndicators" Version="0.1.0" />
   ```
   …or a project reference:
   ```xml
   <ProjectReference Include="..\BlazorAtoms.ActivityIndicators\BlazorAtoms.ActivityIndicators.csproj" />
   ```
2. Ensure your layout references the scoped-CSS bundle — modern templates already include
   `<link rel="stylesheet" href="YourApp.styles.css" />`. An RCL's scoped CSS is bundled
   into the **consuming app's** `{App}.styles.css` automatically; without that link the
   indicators render unstyled/invisible. (This is the most common "it renders but isn't
   styled" cause.)
3. Add the namespace(s) to `_Imports.razor`:
   ```razor
   @using BlazorAtoms.ActivityIndicators
   @using BlazorAtoms.ActivityIndicators.Indicators
   ```

| Type | Namespace |
|---|---|
| `ActivityIndicator` (wrapper) | `BlazorAtoms.ActivityIndicators` |
| The 7 round `Activity*` indicators | `BlazorAtoms.ActivityIndicators.Indicators` |
| Linear indicators (`PulseBar`, `PulseScanner`) | `BlazorAtoms.ActivityIndicators.Indicators` |

---

## The `ActivityIndicator` wrapper

Use this instead of choosing a specific indicator. Set `Name` to render a specific one;
leave it unset to render a **random** indicator (the pick is stable across re-renders).
The set of named indicators is fixed by the package — it's not a registry consumers add to.

```razor
<ActivityIndicator />                          @* random *@
<ActivityIndicator Name="ActivityGears" />         @* specific *@
<ActivityIndicator Name="Gears" Size="64" />   @* "Activity" prefix optional; Size forwarded *@
<ActivityIndicator Name="Swarm" Fill="red" />  @* Fill silently dropped — Swarm has no Fill — no error *@
```

### Wrapper parameters

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `Name` | `string?` | `null` | Indicator to show. `null`/empty → **random**. Matched case-insensitively against the component type name, accepting both `"ActivityGears"` and `"Gears"`. |
| `Size` | `int` | `48` | Rendered width/height in px. Forwarded to every indicator. |
| `Blip` | `string?` | `null` | Sets `--ind-blip`. Forwarded only if the chosen indicator declares it. |
| `Glow` | `string?` | `null` | Sets `--ind-glow`. Forwarded only if the chosen indicator declares it. |
| `Line` | `string?` | `null` | Sets `--ind-line`. Forwarded only if the chosen indicator declares it. |
| `Fill` | `string?` | `null` | Sets `--ind-fill`. Forwarded only if the chosen indicator declares it. |
| `Class` | `string?` | `null` | Extra CSS class(es) on the chosen indicator's root `<svg>`. |
| `OnUnknownName` | `EventCallback<string>` | — | Invoked with the requested `Name` when no indicator matches it, just before falling back to a random indicator. No-op if unbound. |

```razor
@* unknown name -> callback fires with "Nope", then a random indicator renders *@
<ActivityIndicator Name="Nope" OnUnknownName="OnMissing" />

@code {
    private void OnMissing(string requested) => Logger.LogWarning("No indicator {Name}", requested);
}
```

Not every indicator declares every parameter (e.g. `ActivitySwarm` has no `Line`/`Fill`).
Passing a parameter the chosen indicator doesn't declare is silently ignored rather than
throwing — see the `Fill="red"` example above.

---

## Using the indicators directly

You can also use any indicator without the wrapper:

```razor
<ActivityFunnel    Size="48" />
<ActivitySwarm     Size="48" />
<ActivityMagnifier Size="48" />
<ActivityGears     Size="48" />
<ActivityNeural    Size="48" />
<ActivityHourglass Size="48" />
<ActivityDna       Size="48" />
```

Every indicator accepts the same parameter set (string color params map 1:1 to the tokens
below). `ActivitySwarm`, `ActivityNeural`, and `ActivityDna` only declare the params they use
(`Blip`/`Glow`, plus `Line` for neural/dna), so an unsupported one is simply unavailable
on those.

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `Size` | `int` | `48` | Rendered width/height in px. `viewBox` scales cleanly at any size. |
| `Blip` | `string?` | `null` | Sets `--ind-blip`. |
| `Glow` | `string?` | `null` | Sets `--ind-glow`. |
| `Line` | `string?` | `null` | Sets `--ind-line`. |
| `Fill` | `string?` | `null` | Sets `--ind-fill`. |
| `Class` | `string?` | `null` | Extra CSS class(es) on the root `<svg>`. |

---

## Linear / slider indicators

A second **form-factor** of activity indicator: a marker that slides back and forth along a
track (bouncing at each end, trailing a fading comet tail). Same purpose as the round set,
different shape. Unlike the round indicators these are **deliberately configured and used
directly** — they are *not* part of the `<ActivityIndicator />` random/named pool, because
each exposes its own rich, divergent parameter set that a shared picker couldn't carry.

They are **self-contained**: the SVG (including a scoped `<style>` with a unique per-instance
id) is emitted inline, so — unlike the `Activity*` set — they do **not** require the consumer's
`{App}.styles.css` bundle. They use a single `Color` parameter rather than the round set's
`--ind-*` tokens. Trimmed WASM publishes are handled automatically — nothing to configure.

| Component | Form | Reads as |
|-----------|------|----------|
| `PulseBar` | Full-height block sweeping inside a rectangular track, bouncing at each end with a fading tail; stretches to fill its container's width at a fixed px height (like a progress bar). | *working / indeterminate progress* |
| `PulseScanner` | A glowing dot eases left↔right along a track, bounces, and trails a comet tail (optional bright LED core). | *scanning / sweeping* |

### `PulseBar` parameters
| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `Color` | `string` | `"#00ffee"` | Block / tail / track color (any CSS color). |
| `Speed` | `double` | `2.0` | Seconds for one end-to-end sweep (a bounce is two sweeps). |
| `Height` | `double` | `14` | Bar height in px. Width always fills the parent container. |
| `TailLength` | `int` | `12` | Trailing blocks behind the head. `0` = no tail. |
| `TailSpread` | `double` | `0.22` | Fraction of a sweep the tail spans (longer = stretchier). |
| `BlockWidth` | `double` | `0.14` | Moving block width as a fraction (0–1) of the track. |
| `HeadWidth` | `double?` | `null` | Optional head width fraction; `null` = use `BlockWidth`. |
| `CornerRadius` | `double` | `16` | Track/clip corner radius (internal units; `0` = square). |
| `ShowTrack` | `bool` | `true` | Draw the faint full-length track behind the block. |
| `TrackOpacity` | `double` | `0.12` | Opacity of the faint track. |

```razor
<div style="width:100%"><PulseBar /></div>
<PulseBar Color="#ff4400" Speed="1.0" TailLength="8" />
<PulseBar Color="#cc00ff" Height="30" TailLength="14" />
<PulseBar Color="#ffaa00" Height="8"  BlockWidth="0.22" />
```
`PulseBar` is block-level and fills its container's width — wrap it in a sized element.

### `PulseScanner` parameters
| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `Color` | `string` | `"#00ffee"` | Dot / tail color (any CSS color). |
| `Speed` | `double` | `2.4` | Seconds for one left-to-right sweep (a bounce is two sweeps). |
| `Width` | `int` | `640` | Overall width in px. |
| `Height` | `int` | `140` | Overall height in px. |
| `TailLength` | `int` | `10` | Trailing dots behind the head. `0` = no tail. |
| `TailSpread` | `double` | `0.22` | Fraction of a sweep the tail spans (stretchier comet). |
| `HeadRadius` | `double` | `13` | Radius of the bright head dot, in px. |
| `ShowTrack` | `bool` | `true` | Draw the faint baseline track the dot travels along. |
| `ShowCore` | `bool` | `true` | Bright white core in the head dot for an LED look. |

```razor
<PulseScanner />
<PulseScanner Color="#ff4400" Speed="1.4" TailLength="14" />
<PulseScanner Width="800" Height="160" Color="#00ff55" ShowTrack="false" />
```

> Both render a unique per-instance CSS id, so many can share one page safely.

---

## Theme tokens (CSS custom properties)

> The `--ind-*` token model below applies to the **round `Activity*`** set. The linear
> indicators above use a single `Color` parameter instead.

The four tokens are shared, but each component maps them to its own parts. `✅` = the
component uses that token; `—` = it has no element of that kind (setting it is harmless).

| Token | Role (general) | Funnel | Swarm | Magnifier | Gears | Neural | Hourglass | DNA |
|-------|----------------|:------:|:-----:|:---------:|:-----:|:------:|:---------:|:---:|
| `--ind-blip` | Primary moving element | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `--ind-glow` | Highlight / accent | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `--ind-line` | Structural strokes | ✅ | — | ✅ | ✅ | ✅ | ✅ | ✅ |
| `--ind-fill` | Faint body fill | ✅ | — | ✅ | ✅ | — | ✅ | — |

Per-component specifics:

| Component | `--ind-blip` | `--ind-glow` | `--ind-line` | `--ind-fill` |
|-----------|--------------|--------------|--------------|--------------|
| `ActivityFunnel` | falling particles | caught-dot highlight | funnel walls + rim | funnel body |
| `ActivitySwarm` | swarming dots | pulsing core | — | — |
| `ActivityMagnifier` | field items | revealed item + glint | lens rim + handle | glass tint |
| `ActivityGears` | large-gear hub | small-gear hub | teeth + rims | gear bodies |
| `ActivityNeural` | nodes | pulses + node flash | edges | — |
| `ActivityHourglass` | sand | falling stream | frame | glass tint |
| `ActivityDna` | strand dots | front-of-helix highlight | base-pair rungs | — |

Any token left unset uses the component's built-in, scheme-aware default (it auto-adjusts
for light/dark OS themes). Note that `prefers-color-scheme` follows the **OS/browser**
theme, not the local background — a light widget inside a dark-themed app will still get
the dark palette; in that case set the tokens explicitly (see example 5 below). Tip: for
the faint `--ind-fill`, pass a translucent color, e.g. `rgba(124,58,237,.12)`.

There are three equivalent ways to theme — pick whichever fits your call site:
1. **Parameters** (`Blip="#..."`) — per-instance, explicit. Works on `<ActivityIndicator>`
   and on the individual indicators.
2. **A `Class` + your own CSS rule** — reusable named themes.
3. **A CSS variable on any ancestor** — themes every indicator inside a region at once.

---

## Configuration examples

### 1. Per-instance theme via parameters
```razor
<ActivityIndicator Name="Funnel" Size="64"
                   Blip="#8b5cf6" Glow="#4c1d95" Line="#7c3aed" Fill="rgba(124,58,237,.12)" />

<ActivitySwarm Size="64" Blip="#f59e0b" Glow="#92400e" />
```

### 2. Reusable named theme (parameter-free call sites)
```razor
<ActivityIndicator Name="Funnel" Class="activity-brand" />
<ActivityIndicator Name="Swarm"  Class="activity-brand" />
```
```css
/* site.css */
.activity-brand {
  --ind-blip: #0ea5e9;
  --ind-glow: #0369a1;
  --ind-line: #0284c7;
  --ind-fill: rgba(2,132,199,.12);
}
```

### 3. Theme an entire region (tokens inherit into every child indicator)
```razor
<div class="panel-activity">
    <ActivityIndicator Name="Funnel" /> <ActivityIndicator Name="Swarm" />
</div>
```
```css
.panel-activity { --ind-blip:#34d399; --ind-glow:#065f46; }
```

### 4. Single accent via `currentColor`
Map every token to `currentColor`, then drive it with the element's `color` — it inherits
from buttons, links, text, theme classes, etc.
```razor
<ActivitySwarm Size="40" Blip="currentColor" Glow="currentColor" Class="accent" />
```
```css
.accent { color: #e11d48; } /* or inherit from a themed parent */
```

### 5. Force a palette regardless of OS theme
A light card inside a dark-themed app won't auto-darken (OS theme is still dark), so set
the tokens explicitly for guaranteed contrast:
```razor
<div class="light-card">
    <ActivityFunnel Blip="#0e9c9c" Glow="#04545f" Line="#0e9c9c" Fill="rgba(14,143,143,.12)" />
</div>
```

### 6. Size with `em` so it tracks surrounding text
`Size` is px, but you can also let it scale with font size by adding a class:
```razor
<span class="inline-activity"><ActivitySwarm Class="em-size" /></span>
```
```css
.em-size { width: 1.25em; height: 1.25em; } /* overrides the px width/height */
.inline-activity { vertical-align: -0.2em; }
```

---

## Reduced motion

Each component ships a built-in guard — when the user requests reduced motion, all
animations stop and the indicator holds a static, still-legible frame. No code needed:
```razor
<ActivityIndicator Name="Funnel" />   @* automatically freezes under prefers-reduced-motion: reduce *@
```
```css
/* every component ships its own guard, e.g.: */
@media (prefers-reduced-motion: reduce) {
  .drop, .caught { animation: none; }       /* funnel  */
  .swarm, .dot, .core { animation: none; }  /* swarm   */
  .item, .lens { animation: none; }         /* magnifier */
  .gear-a, .gear-b { animation: none; }     /* gears   */
  .node, .pulse { animation: none; }        /* neural  */
  .frame, .sand-top, .sand-bottom, .stream { animation: none; } /* hourglass */
}
```

**Customize the static frame.** Because reduced motion just removes the animation, the
shapes freeze at their authored start position. To show a tidier resting state, override
it from host CSS — e.g. show the swarm as a calm, fully-formed ring:
```razor
<ActivitySwarm Class="rm-tidy" />
```
```css
@media (prefers-reduced-motion: reduce) {
  .rm-tidy .dot  { opacity: 1; transform: none; }
  .rm-tidy .core { opacity: .9; transform: none; }
}
```

> **Test it without changing OS settings:** Chrome/Edge DevTools → Command palette
> (`Ctrl+Shift+P`) → *"Emulate CSS prefers-reduced-motion: reduce"*. Firefox: set
> `ui.prefersReducedMotion = 1` in `about:config`.

---

## Server + WebAssembly

The indicators and the wrapper are render-mode agnostic — pure SVG + scoped CSS, no JS, no
DI — so they work unchanged under `InteractiveServer`, `InteractiveWebAssembly`, and
`InteractiveAuto`. **Nothing for consumers to configure** — Blazor Server never trims
(no-op there), and trimmed/AOT WASM publishes are handled automatically by the package.

---

## Notes & gotchas

- **Scoped-CSS bundle must be linked** — the most common "it renders but isn't styled" cause.
- **`prefers-color-scheme` ≠ local background** — see example 5.
- **Performance** — tiny vector animations driven by the compositor (`transform`/`opacity`);
  rendering dozens on a page is cheap.
