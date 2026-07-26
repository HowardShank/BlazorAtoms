# BlazorAtoms.Progress

Progress indicators for Blazor. Ships **`AtomScrollProgressBar`** — a fixed reading-progress bar
whose width tracks how far down the page the user has scrolled.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Progress\BlazorAtoms.Progress.csproj" />
```
```razor
@using BlazorAtoms.Progress
```

## AtomScrollProgressBar

```razor
<AtomScrollProgressBar />
```

Drop it in once, anywhere on the page (typically near the top of a layout) — it renders as a
`position: fixed` strip and needs no wrapping or `ChildContent`.

### How it animates — and the one real caveat

The primary path is a pure CSS **scroll-driven animation**: a plain `width: 0% → 100%` keyframe
tied directly to page scroll position via `animation-timeline` — the browser interpolates it
natively, no scroll event listener, no per-frame JS. Getting there still needs one small **one-time**
JS call, even on supporting browsers: `atom-progress.js` finds the actual scroll container (walking
up from the bar) and explicitly names a `scroll-timeline` on it, then points the bar's
`animation-timeline` at that name. This isn't optional — the more obvious approach
(`animation-timeline: scroll()` alone, letting the browser auto-resolve the "nearest" scrollable
ancestor) silently breaks for a `position: fixed` bar: verified live that Chrome resolves "nearest"
via the fixed element's reparented containing-block chain (always the viewport), not its DOM
ancestry, so inside an app-shell layout with an inner-scrolling content div (not the whole page),
the bar would bind to the wrong scroller and never move.

**Scroll-driven animations are Chromium-only today** (not yet in Firefox or Safari). On other
browsers, the same `OnAfterRenderAsync` call falls back to a plain `scroll`/`resize` listener on the
same detected scroll container, setting `width` directly from
`scrollTop / (scrollHeight - clientHeight)`.

### Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Color` | `string` | `"#e6175d"` | Color of the bar. |
| `Height` | `string` | `"12px"` | Thickness of the bar. Any CSS length. |
| `Position` | `ScrollProgressPosition` | `Top` | Which edge of the scroll container the bar sticks to (`Top`/`Bottom`) — not the raw viewport edge; see below. |
| `Width` | `string?` | `null` | Width of the track. Any standard CSS length (`"50%"`, `"300px"`, `"20rem"`, ...), resolved against the scroll container, not the viewport. `null` (default) spans the full container width. |
| `Align` | `ScrollProgressAlign` | `Start` | Horizontal alignment of the track within the container when `Width` makes it narrower (`Start`/`Center`/`End`). |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root `<div>`.

### Sizing and positioning against the container, not the viewport

A `position: fixed` element's percentages (`width`, `left`) and `top: 0`/`bottom: 0` normally
resolve against the *viewport* — wrong for an app-shell layout where the actual scroll container
is narrower than (and offset from) the full screen (e.g. next to a sidebar, below a fixed header).
`atom-progress.js` measures the real scroll container's bounding box and:

- sets the track's `left`/`width` in px to match the container horizontally (or, when `Width` is
  set, resolves that length against the container and positions it per `Align`),
- sets `top` (or `bottom`) in px to match the container's own edge, not the viewport's,
- re-syncs on window resize, on the container's own scroll, and whenever `Position`/`Width`/`Align`
  change at runtime.

`Width` accepts any CSS length. Rather than reimplementing unit math for `%`/`px`/`rem`/`vw`/
`calc()`/etc., the module applies it to a hidden probe element placed *inside* the actual scroll
container and reads back its resolved pixel width — the same technique the browser already uses,
so arbitrary units "just work." One CSS nuance worth knowing: a percentage `Width` resolves
against the container's *content box* (excluding its own padding), per standard CSS percentage
resolution — this is correct behavior, not a quirk, but can surprise you if you expected it against
the container's full visual (border-box) width.

## Notes

- **Not fully zero-JS.** Unlike the rest of this repo's effect components, this one genuinely
  needs a small JS touch even on Chromium (to name and bind the scroll-timeline explicitly) and
  full JS on non-Chromium browsers (no declarative-CSS-only way to track scroll position at all
  there). The JS is still self-contained — no `BlazorAtoms.Behaviors` dependency, this package has
  zero BlazorAtoms deps.
- **Accessibility.** Scroll-driven animations are 1:1 with the user's own scroll input, not
  autoplaying motion, so this doesn't gate on `prefers-reduced-motion` the way a timed animation
  would (common guidance treats it the same way native scrollbars aren't considered "motion").
- **Render modes.** JS interop can't run during static SSR/prerender — the bar's native CSS
  keyframes still apply immediately in Chromium regardless; the JS fallback (for other browsers)
  attaches once the component is interactive.
