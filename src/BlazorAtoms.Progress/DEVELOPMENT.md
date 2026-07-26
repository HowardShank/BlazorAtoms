# BlazorAtoms.Progress — Development Notes

Internal architecture notes for maintainers. See `README.md` for consumer-facing usage.

## AtomScrollProgressBar — the first component that isn't zero-JS everywhere

Sourced from a "CSS-only reading progress bar" demo using `animation-timeline: scroll(y)` — a
single `@keyframes width: 0% → 100%` tied directly to scroll position via CSS, no JS at all in the
original. That's real, but scroll-driven animations are Chromium-only right now (no Firefox,
no Safari) — flagged explicitly to the requester before building (per this session's "spec
before implement" process), who chose a JS fallback over shipping CSS-only with silently-inert
non-Chromium browsers.

This makes `AtomScrollProgressBar` the first component in the whole effects family (`AtomTransition`
aside, which already has its own JS-fallback precedent for `@starting-style`) that isn't
zero-JS-everywhere by nature — `AtomHoverGlow` has the same shape (Chromium-only primary path +
JS fallback), so this isn't a new pattern, just a second instance of it.

### Why this package has zero BlazorAtoms dependencies (unlike AtomHoverGlow)

`AtomHoverGlow` (`BlazorAtoms.Transitions`) reuses `BlazorAtoms.Behaviors.AtomBrowserSupport` for
its capability check, since `BlazorAtoms.Transitions` already carries that dependency (the one
documented exception to the 0-deps rule, for `AtomTransition`'s `@starting-style` check).
`BlazorAtoms.Progress` is a brand-new package with no such existing exception, and adding a second
package with a `BlazorAtoms.Behaviors` reference would quietly turn "one deliberate exception" into
a growing pattern. Instead, `atom-progress.js` does its own inline `CSS.supports(...)` check —
three lines, not worth a cross-package dependency to dedupe. Keeps `BlazorAtoms.Progress` at the
same "0 BlazorAtoms deps" standard as every other package except `BlazorAtoms.Transitions`.

### Why JS runs even when the browser natively supports scroll-driven animations

First shipped version tried the obvious pure-CSS approach: `animation-timeline: scroll()` (bare,
default "nearest" scroller) on a `position: fixed` bar, relying entirely on implicit ancestor
auto-detection — no JS at all when supported. Broke immediately in this repo's own demo app (an
app-shell layout: fixed sidebar, `<main>` with `overflow:hidden`, an inner `#content` div that
actually scrolls) — the bar's width stayed permanently at 0%, confirmed live via direct DOM/computed-style
inspection in the browser rather than guessed from reading the CSS.

Root cause, also confirmed live: a `position: fixed` element's "nearest ancestor scroller" is
resolved by Chrome via its *containing-block* chain, which fixed positioning reparents to the
viewport — not via the element's actual DOM/flat-tree ancestry, which is what the spec's "nearest"
keyword is supposed to mean and where the real scroll container (`#content`) actually lives. So the
bare-`scroll()` + `position:fixed` combination silently binds to the wrong (or no) scroller whenever
the real page layout isn't a simple whole-document-scrolls case.

Tried `position: sticky` next (keeps the element in normal flow, no containing-block reparenting) —
fixed the width-tracking for `Position.Top` (confirmed live), but broke `Position.Bottom`: `sticky`
can only pin an element once scrolling would push it toward that edge, and since the bar renders as
the very first element in the content, it never approaches the bottom edge at all — it just sits
in normal flow wherever it naturally falls. Also confirmed live before reverting.

Final approach (what ships): keep the bar `position: fixed` (so `Bottom` genuinely pins to the
viewport edge regardless of scroll position, like `Top` does), and stop depending on implicit
"nearest ancestor" resolution entirely. `atom-progress.js` walks up from the bar once to find the
real scroll container (same walk used by the manual-scroll fallback), then **explicitly names** a
`scroll-timeline` on that container (`scroll-timeline-name` + `scroll-timeline-axis`, set via
`element.style.setProperty`) and points the bar's `animation-timeline` at that same name. Named
timelines are looked up by name, not by DOM/containing-block ancestry, so `position: fixed` no
longer interferes — confirmed live that both `Top` and `Bottom` track scroll correctly this way, in
the exact layout that broke the original approach. This makes the one-time JS call load-bearing
even on Chromium; the only browsers that skip JS *entirely* would be ones with neither
scroll-driven animations nor this component in use, which is to say: none. The manual `scroll`/
`resize`-listener fallback (for browsers without scroll-driven animations at all) reuses the same
scroll-container detection, so there's exactly one "find the real scroller" implementation either
way.

### Why there's no `prefers-reduced-motion` override

Every other effect component in this repo disables its animation under
`prefers-reduced-motion: reduce`, because those animations play *on their own* (loop, hover-driven,
one-shot-triggered) independent of anything the user explicitly did each frame. A scroll-driven
progress bar's width is a direct, 1:1 function of the user's own scroll input — it only ever moves
because the user is actively scrolling, never on a timer or in response to a hover they didn't
intend as "watch this animate." That's the same category as a native scrollbar thumb, which nobody
expects `prefers-reduced-motion` to hide. So this component intentionally has no reduced-motion
gate.
