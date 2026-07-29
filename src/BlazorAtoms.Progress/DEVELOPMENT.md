# BlazorAtoms.Progress — Development Notes

Internal architecture notes for maintainers. See `README.md` for consumer-facing usage.

## Two bases, not one

`AtomProgressBase` holds what all four determinate indicators share: label/readout, the
`Variant`/`Size`/`Effect` axes emitted as `data-*`, the `--progress-*` token surface, and `Kebab`.
`AtomProgressValueBase` adds `Value`/`Min`/`Max`/`Formatter` and the clamped `Fraction`.

The split exists for `AtomProgressSteps`, which has no continuous value — inheriting the value base
would give it `Min`/`Max` parameters that mean nothing and can't be honored. Same rule that keeps the
hover-reveal cards as five components instead of one effect enum: don't put a parameter on a type
where it is silently invalid. The alternative (one base, and steps documents that `Value` means
"completed count") trades a compile-time guarantee for a doc comment.

`AtomScrollProgressBar` shares neither base. It has no `Value` at all — its position comes from the
scroll container — and it predates them.

## Null `Value` = indeterminate

`Value` is `double?` and null *is* the indeterminate state, rather than a non-nullable `Value` plus an
`Indeterminate` bool. Two params would admit `Value="40" Indeterminate="true"`, where the value is
silently ignored and the component has to pick a winner. One nullable param makes that
unrepresentable. Precedent in the repo: `AtomRating.Value` is `double?` so null is a real "unrated",
distinct from 0.

Consequences the components implement uniformly:

- `aria-valuenow` is **omitted** when null. Not an oversight — a progressbar with no `valuenow` is
  precisely how ARIA spells indeterminate. **`AtomMeter` is the exception**: see below.
- No readout renders even with `ShowValue`, so the element is absent rather than empty.
- `data-indeterminate="true"` on the root, so the state is styleable without a second class.
- The clamped value is what gets announced, so the visual and the announcement can't disagree.
- `Max <= Min` returns `Fraction` 0 rather than dividing by zero.

## Indeterminate vs. `Effect`: two animations, one `animation` property

`ProgressEffect` decorates a known amount; the indeterminate animation says the amount is unknown.
They are deliberately independent parameters — but CSS gives an element one `animation` property, so
where a looping effect and the indeterminate keyframe would land on the same element, an explicit
`[data-indeterminate][data-effect="…"]` rule re-asserts the indeterminate animation. It wins because
"unknown" is the more load-bearing of the two messages.

Under `prefers-reduced-motion: reduce` the indeterminate animation can't simply be dropped: a frozen
35%-wide bar (or a frozen quarter-arc ring) reads as *stalled progress*, which is worse than no
indicator. Both fall back to a dimmed full track and let the accessible busy state carry the meaning.

## AtomProgressRing — `pathLength` instead of 2πr

`pathLength="100"` re-bases the circle's own length onto a 0–100 scale, so `stroke-dasharray="100"`
plus `stroke-dashoffset: 100 - percent` draws that percentage at any radius, with no π and nothing to
recompute when `Diameter` changes. A test asserts the offset is identical at 32 px and 400 px — the
property that would break if anyone "optimized" this into circumference math.

The `viewBox` is `0 0 {Diameter} {Diameter}` so 1 user unit == 1 px, which is what lets `stroke-width`
be a plain px number.

**Why the stroke width resolves in C# here but not on the bar.** The radius depends on it — measured
to the stroke's centerline, `r = Diameter/2 - Thickness/2`, so the outer edge lands on the box rather
than half outside it. CSS *can* set `r` (SVG2 geometry properties) but not portably, so `Thickness`
falls back to a per-`Size` constant in C# instead of in a `[data-size]` block, and is clamped to
`Diameter/2` so it can't swallow the hole. `--progress-thickness` is still emitted for the effect
rules.

The indeterminate spin uses `transform-box: fill-box` + a 50% origin so one keyframe works at every
diameter. It overrides the inline `transform` attribute, which is why `StartAngle` has no effect while
indeterminate — documented rather than worked around, since a rotating arc has no meaningful start.

`Stripes`/`Gradient` can't be a `background-image` on a stroke without a per-instance `<pattern>` or
`<linearGradient>`, and those need ids — which would differ between the prerender and interactive
passes and cause a hydration mismatch. They degrade to stroke-only equivalents (a dash pattern, a
lighter stroke) instead of minting ids.

## AtomProgressSteps — connectors as `::before`, and the button/span switch

The connector line is each item's own `::before`, not a rendered element: one less node per step, and
`:first-child` suppresses the leading one. It belongs to the step it leads *into*, so a
complete-or-active step fills the line behind it and the filled run always ends at the current marker.

Vertical spacing is one token (`--progress-step-gap`) driving both the item's `padding-bottom` and the
connector's own length, so the two can't drift apart. An earlier draft sized the connector from
`--progress-marker-size` and left a gap under any non-default marker size.

`OnStepClick` changes the *markup*: supplied, markers are real `<button>`s (focusable,
Enter/Space-activatable, with an `aria-label`, because the visible caption is a sibling the button
doesn't contain and it would otherwise announce a bare number); not supplied, they are `aria-hidden`
`<span>`s so a non-navigable tracker adds nothing to the tab order. A `<div @onclick>` would have been
neither.

The marker's inner content is one `RenderFragment` local declared at the top of the `.razor`, rendered
into either branch — Razor resolves locals in source order, so it has to precede its use.

## AtomMeter — reimplementing `<meter>`'s semantics, not its chrome

Native `<meter>` draws its bar through vendor pseudo-elements that differ per browser
(`::-webkit-meter-optimum-value` vs Firefox's own set), can't be themed consistently, and can carry
neither the `--progress-*` surface nor the effect keyframes. So the element is not used; the
*semantics* are — `role="meter"` plus the spec's own three-way `low`/`high`/`optimum` classification,
reimplemented in `Level` with one test case per branch.

The third band only exists when `Optimum` lies outside `Low`..`High`; with an optimum between them the
spec defines no sub-suboptimum, and a test pins that. No `Optimum` at all → no `data-level`, because
with no stated ideal there is nothing to judge against.

`data-level` beats `[data-variant]` in the CSS: once a caller has said what good looks like, the
value's quality is more informative than a decorative scheme. An inline `FillColor` still beats both.

`Segments` renders as one `repeating-linear-gradient` rather than N tick elements, so segment count has
no DOM cost.

### A null `Value` is the one place the meter breaks the family's indeterminate contract

The bar and the ring treat "no value" as a first-class state: they animate, and they omit
`aria-valuenow`, which is exactly how ARIA defines an indeterminate `progressbar`. Neither move
transfers to a meter:

- **No animation.** There is nothing to sweep *toward* — a moving meter would imply a measurement in
  progress, which is not what a meter models. `data-indeterminate` instead hatches the empty track, so
  "no reading taken" is visually distinct from a real zero. That attribute exists on all three
  components but is the *only* thing it drives here.
- **`role="meter"` is dropped, along with every `aria-value*` attribute.** ARIA lists `aria-valuenow`
  as **required** for `role="meter"` — there is no indeterminate meter in the spec. Emitting the role
  without the value ships invalid markup; emitting `aria-valuenow="{Min}"` ships a fabricated 0%
  reading that assistive tech cannot distinguish from real data. Dropping the role means an
  unmeasured meter announces nothing, which is the honest option and costs only that.

This was shipped wrong first: the original markup kept the role and merely left `aria-valuenow` empty,
with a code comment that *stated* the requirement it was violating. Tests now pin both directions —
role absent with no value, full semantics restored with one.

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
