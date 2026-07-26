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
| `Position` | `ScrollProgressPosition` | `Top` | Which edge of the viewport the bar sticks to (`Top`/`Bottom`). |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root `<div>`.

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
