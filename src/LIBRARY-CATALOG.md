# BlazorAtoms — Library Catalog & Roadmap

A living planning doc for how the BlazorAtoms family of component libraries is organized.
Each library ships as its own NuGet package, standalone, with ~0 third-party dependencies,
usable in any Blazor render mode.

> **Design philosophy — lightweight, drop-in, no lock-in.**
> Every BlazorAtoms library is a small, self-contained "atom": add just the one you need and it
> works. No shared runtime dependency, no umbrella package, no design-system framework to buy
> into, no global setup/theme provider, no `builder.Services.Add…()` registration, and no
> JS bundle to wire up. Each component is plain Blazor + CSS/SVG that reads its inputs from
> `[Parameter]`s and styles from CSS variables, so it **intermixes freely with any application** —
> a greenfield app, a legacy page, or alongside a heavy component suite (MudBlazor, Radzen,
> Fluent, Telerik) without conflict. Take one component, take one library, or take several; you
> never inherit a large framework just to use a spinner.
>
> **One deliberate exception:** `BlazorAtoms.Transitions` carries a real `ProjectReference` to
> `BlazorAtoms.Behaviors`, for the runtime CSS-capability check its `@starting-style`/JS-fallback
> hybrid needs. This is a genuine shared *capability* package (not a grab-bag "Core"), scoped and
> acknowledged rather than silently breaking the 0-deps rule — every other library still stands
> alone.

## Naming convention

- **Library** = package id = assembly = root namespace = `BlazorAtoms.<Area>`.
  - `<Area>` is PascalCase: **plural** for countable families (`Inputs`, `Cards`, `Charts`),
    a mass noun otherwise (`Layout`, `Progress`, `Navigation`).
- **Public component** = `Atom<Name>` for both the type and the tag, e.g. `AtomActivityGears` → `<AtomActivityGears />`.
- **Component groups** within a library = subfolder + matching sub-namespace (e.g. `Indicators/`).
- **Dependencies**: framework only (`Microsoft.AspNetCore.Components.Web`); no third-party runtime packages.
  Shared code lives in `BlazorAtoms.Shared` and is compiled in via `build/Shared.props` (no package dependency).

Libraries are tiered below by how well they fit the ethos: standalone, ~0-dependency,
self-contained SVG/CSS, JavaScript-free.

## JavaScript policy

JavaScript is allowed only where a browser primitive genuinely requires it (focus-trap,
scroll-lock, smart positioning, clipboard, Resize/IntersectionObserver) — and it must be
**invisible to the consumer**:

- The library ships its own small JS module as a static web asset
  (`wwwroot/atom-<area>.js` → `_content/BlazorAtoms.<Area>/atom-<area>.js`) and **lazy-imports it
  itself** via an `IJSObjectReference` on first use. No `<script>` tag, no DI registration, no setup.
- Each JS-using library carries its own module — no shared JS package (that would be a dependency).
- Logic and state stay in C#; JS does only the primitive the platform can't do declaratively.
- Prefer native features first: `<details>/<summary>`, the HTML **Popover API**, CSS **anchor
  positioning**, and `<dialog>` remove most classic JS needs.
- Caveat: JS interop can't run during static SSR / prerender — JS components render markup first and
  enhance once interactive. JS-free libraries behave identically in every render mode.

## Graphics policy

Most every graphic the components draw is **inline SVG** — vector, crisp at any zoom/DPI, themeable via
CSS custom properties and `currentColor`, animatable in CSS. This covers charts, gauges, sparklines,
rating icons, QR/barcodes, skeleton shapes, and icons.

- **No/Limited raster for our own chrome** — want crisp clear SVG, not PNG/JPG UI assets, whenever possible.
- **Avoid `<canvas>`** (it is raster *and* needs JS); use SVG unless there is a hard performance reason.
- The only raster we own is the 128×128 NuGet package icon (packaging metadata, never shipped into
  apps). **User content** (an avatar photo, an image inside a card) is raster by nature — we render
  it inside SVG/CSS chrome but don't control its quality; our frame is always vector.

---

## Tier A — strong fit (pure SVG/CSS, JS-free). Build these first.

| Library | Components (`Atom*`) |
|---|---|
| `BlazorAtoms.ActivityIndicators` *(shipped)* | AtomActivityGears, AtomActivityDna, AtomActivityFunnel, AtomActivityHourglass, AtomActivityMagnifier, AtomActivityNeural, AtomActivitySwarm, AtomPulseBar, AtomPulseScanner |
| `BlazorAtoms.Progress` *(in progress)* | AtomScrollProgressBar *(shipped)* — fixed reading-progress bar tracking page scroll position; CSS scroll-driven animation (`animation-timeline: scroll()`) natively, Chromium-only, with a small self-contained JS fallback elsewhere (no `BlazorAtoms.Behaviors` dependency — inline capability check instead, to avoid growing that "one exception" beyond `BlazorAtoms.Transitions`). Still to build: AtomProgressBar (determinate `Value`), AtomProgressRing, AtomProgressSteps, AtomMeter |
| `BlazorAtoms.Skeletons` | AtomSkeletonText, AtomSkeletonBlock, AtomSkeletonAvatar, AtomSkeletonCard |
| `BlazorAtoms.Charts` | AtomSparkline, AtomBarChart, AtomLineChart, AtomDonut, AtomGauge |
| `BlazorAtoms.Badges` *(shipped)* | AtomBadge — count/label badge in many shapes (pill/circle/square/rounded + SVG star/hexagon/diamond/shield/burst/ribbon); overlays a host corner or renders inline; any object value w/ type-aware formatting; motion opt-in via `Animation` (default `None`) with Pop/Bounce/Spin/Pulse/Ping (merged the old AtomStaticBadge + AtomAnimatedBadge) · AtomChip — interactive chip (icon slot, click/select, removable) · AtomTag — display categorization label (rounded rect, removable) · AtomPill — status pill (fully-rounded soft tint + leading dot). Chip family shares Variant + Solid/Soft/Outline Appearance. |
| `BlazorAtoms.Avatars` *(shipped)* | AtomAvatar — silhouette (solid/gradient) or image, cropped to circle/square/rounded/squircle/hexagon, bg color/gradient+angle, border ring · AtomInitialsAvatar — initials from name w/ deterministic palette color · AtomAvatarGroup — overlapping stack from names w/ "+N" overflow |
| `BlazorAtoms.Ratings` *(shipped)* | AtomRating — one component for both a read-only display (true fractional fill, e.g. 4.3/5) and an interactive input (hover preview, click, keyboard, snapping `Step`). Value is `double?` so `null` is a distinct "unrated" state; clearable; built-in star/heart/circle/square/diamond/triangle/thumb/bolt shapes or a custom SVG path; per-instance colors; optional value + count label |
| `BlazorAtoms.Layout` | **AtomDrawer** — overlay drawer panel with position (left/right/top/bottom), transitions (slide/fade/pop/bounce/grow), sizing, and declarative styling; future: container anchoring. AtomStack, AtomGrid, AtomDivider, AtomSpacer, AtomCenter, AtomAspectRatio (planned). |
| `BlazorAtoms.Transitions` *(shipped)* | **AtomTransition** — generic wrapper that plays a CSS enter/exit transition (`AtomTransitionEffect`: Fade, Pop, FadeScale, SlideUp/Down/Left/Right, ShiftBlur, FlipY20/FlipYNeg20/FlipX20/FlipXNeg20) around arbitrary child content on a `Show` toggle. CSS-native (`@starting-style`) where supported, JS fallback elsewhere — see JavaScript policy note below. **AtomHoverEffect** — generic wrapper, same "arbitrary `ChildContent`" shape, but hover-triggered: pure CSS `:hover`/`:active`, no C# state. `HoverEffect` enum picks the treatment — `Sparkle` (scattered SVG sparkles; positions are a deterministic function of index, not `Random`, to avoid a hydration-mismatch jump) and `Tilt` (decorative 3D lift, `TiltDegrees`/`TiltPerspective`). Both members share one parameter surface, which is what keeps them on an enum instead of becoming separate components — `Tilt` lives here rather than in `BlazorAtoms.Cards` because it reveals nothing and so needs no card structure; wrap any card in it to compose. **AtomHoverGlow** — wraps several children (any elements) and glows whichever is currently hovered/focused, sliding between them; pure CSS anchor positioning where supported (Chromium only today), JS fallback (event delegation + bounding-box tracking) elsewhere via `AtomBrowserSupport`. |
| `BlazorAtoms.Barcodes` *(implemented)* | AtomBarcode (1D), AtomQrCode (2D) — own C# encoder → SVG; generation only |
| `BlazorAtoms.Data` *(shipped)* | AtomDataHasher — live CRC-32 / CRC-64 / MD5 / SHA-256 / SHA-512 hex-digest panel over a text input; CRC engines implemented in-library (no `System.IO.Hashing` dependency), cryptographic engines wrap `System.Security.Cryptography`; algorithm picker toggle, `EditContext`-aware validation, `Multiline` textarea vs single-line input, `ResultColor`/`ResultBackgroundColor` theming; no JS |
| `BlazorAtoms.DragDrop` *(shipped)* | AtomDropzone&lt;TItem&gt; — generic drag-and-drop list using native HTML5 DnD (no JS), single-list reorder + cross-zone transfer with explicit group scoping via `<AtomDropzoneGroup>` and/or `Group=` key, `Accepts` / `AllowsDrag` / `CopyItem` / `MaxItems` / `InstantReplace` predicates, Vertical / Horizontal / Grid orientation, `Virtualize` support, scoped CSS with `--dropzone-highlight-color` / `--dropzone-deny-color` / `--dropzone-gap` custom properties, all events surfaced as `EventCallback<>`; pure `DropzoneEngine` reorder helper exposed for headless use |
| `BlazorAtoms.Scrollbars` *(shipped)* | AtomScrollbar — generic `ChildContent` wrapper giving its own scroll box a themed scrollbar (size, track/thumb color, optional 2-stop thumb gradient, radius, border, hover color); `Axis` (Vertical/Horizontal/Both) picks which `overflow-*` is active. Zero-JS: full control via `::-webkit-scrollbar` pseudo-elements on Chrome/Edge/Safari, `scrollbar-color`/`scrollbar-width` solid-color fallback on Firefox. |

On-brand standout: **`Charts`** — SVG, JS-free, same DNA as the activity indicators.

**What each does:**
- **ActivityIndicators** — animated "working…" loaders (gears, DNA, pulse bars) for when a task is running with no known progress %.
- **Progress** — shows how far along a task is: a filling bar, ring, or step tracker. Use when you *do* know the percentage (uploads, wizards). `AtomScrollProgressBar` (shipped) is the one exception — determinate but driven by scroll position, not an explicit `Value`.
- **Skeletons** — grey placeholder shapes shown while data loads, roughly matching the layout of the content to come, usually with a shimmer (the grey boxes YouTube/LinkedIn show before content paints). Less jarring than a spinner. Swap in the real component when data arrives.
- **Charts** — small data visuals drawn as SVG (sparkline, bar, line, donut, gauge) — lightweight, no charting library.
- **Badges** — tiny labels or counts attached to things: status pills, tags, the red "3" notification count on an icon.
- **Avatars** — circular/square images that stand in for a person or entity. `AtomInitialsAvatar` falls back to initials (or a placeholder) when there's no photo; `AtomAvatarGroup` stacks several with overlap and a "+N" overflow. Common in team lists, comment threads, and assignee rows.
- **Ratings** — star/heart scales in a single component. `AtomRating` shows a read-only score with true fractional fill (★★★★☆ 4.3/5) or, by default, is an interactive input (hover preview, click, keyboard) that snaps to a configurable `Step` (whole, half, or finer). The value is `double?` so `null` is a distinct "unrated" state (not a real 0); also clearable, a built-in icon shape or a custom SVG path, and an optional value/count label. *(Shipped as one `AtomRating` — the earlier two-component `AtomRatingInput` split was dropped in favor of a `ReadOnly` toggle.)*
- **Layout** — invisible structural building blocks: stack (spacing), grid, divider, spacer, centering — the scaffolding you arrange other components inside.
- **Transitions** — `AtomTransition` wraps arbitrary content and plays a reusable enter/leave animation (fade, slide, flip, ...) whenever `Show` toggles. CSS-native (`@starting-style`) on modern browsers with a JS fallback elsewhere, decided at runtime via `BlazorAtoms.Behaviors`'s capability check — see that library's entry and the JavaScript policy note below for the one deliberate dependency exception this creates.
- **DragDrop** — generic list drag-and-drop with `AtomDropzone<TItem>`. Native HTML5 DnD only (no JS), single-list reorder + cross-zone transfer via an outer `<AtomDropzoneGroup>` cascading context, optional `Group` key for finer scoping, `Accepts` / `AllowsDrag` / `CopyItem` / `MaxItems` / `InstantReplace` predicates, and Vertical / Horizontal / Grid orientation.
- **Scrollbars** — `AtomScrollbar` wraps any content in a scroll box with a custom-themed
  scrollbar (color, size, gradient, radius, border, hover) per instance, instead of the browser's
  default chrome. Full control on Chrome/Edge/Safari via `::-webkit-scrollbar`; Firefox falls back
  to solid colors via `scrollbar-color`/`scrollbar-width` (no gradient/radius/hover support there).
- **Barcodes** — machine-readable graphics generated from a value, rendered as SVG. "Barcode" is the umbrella: `AtomBarcode` for 1D/linear (Code128, EAN-13, Code39…) and `AtomQrCode` for 2D/matrix (QR). **Generation only, using our own C# encoder** — a third-party lib (QRCoder/ZXing) would break the 0-dep rule. Note: *reading/scanning* a code needs a camera + JS → a separate Tier C concern, not part of this library. *(Implemented: Code39, Code128, EAN-13, UPC-A, ITF, Codabar (1D) + QR byte-mode v1–40 (2D), verified by ZXing round-trip / reference tests.)*

## Tier B — feasible, minor/optional JS

| Library | Components (`Atom*`) | Note |
|---|---|---|
| `BlazorAtoms.Inputs` *(in progress)* | **The standard fields, all shipped** — AtomTextField, AtomTextArea, AtomNumberField&lt;TValue&gt;, AtomCheckbox, AtomSwitch, AtomRadioGroup&lt;TValue&gt;, AtomSelect&lt;TValue&gt;: seven native controls (`input`/`textarea`/`select`) on one **AtomInputBase&lt;TValue&gt;** — shared `@bind-Value` + `EditContext` glue, label/help/`Visible`, and three styling axes (`Variant` Outline/Filled/Underline, `Size` Small/Medium/Large, `Effect` None/FocusGlow/FocusRaise/FocusUnderline/ShakeOnError) emitted as `data-*` over a `--field-*` custom-property surface, so a look is one CSS block and no C# change. `ReadOnly` renders a real native `readonly` where the platform has one (text/textarea/number) and folds into `disabled` where it doesn't (checkbox/switch/radio/select). Checkbox/switch/radio paint their own box/track/mark over a transparent-but-focusable native input, which is also how the JS-only `indeterminate` flag is faked. Also shipped: AtomRangeInput, AtomCrtInput, AtomCrtDisplay. Still to build: nothing on the core-forms list | core forms; JS-free throughout |
| `BlazorAtoms.Buttons` | AtomButton, AtomIconButton, AtomButtonGroup, AtomToggleButton, AtomSplitButton | JS-free |
| `BlazorAtoms.Cards` *(in progress)* | Hover-reveal card family — each shows a themed face (title/subtitle/background image/dot indicator) with a staggered entrance on mount, then uncovers a body panel on hover; they differ in *how*. **AtomCardReveal** *(shipped)* — overlay slides away along a `Direction` (Left/Right/Up/Down) leaving an image sliver; `RevealSize` is measured along the active axis, hence "size" not "width". **AtomCardFlip** *(shipped)* — card rotates 180° around `FlipAxis`, body is the back face. **AtomCardExpand** *(shipped)* — card grows to `ExpandedHeight` and the body slides up from the bottom (height transition, not `scale()`, so image/text don't distort). **AtomCardCurl** *(shipped)* — a `Corner` peels back; a CSS corner *fold*, not a photorealistic curl (CSS can't warp a plane — that needs an SVG filter or WebGL). **AtomCardSplit** *(shipped)* — the face splits along a `SplitAxis` and both halves swing open, hinged on their outer edges; each half carries the whole image pinned to its own outer edge so the closed card reads as one unbroken picture, and the halves have no back faces (they stop being drawn past 90deg) which keeps the revealed body a SINGLE element — text is never split at the seam nor duplicated in the DOM. Optional `ShowSeamCircle` reproduces the source design's circle straddling the seam. All five share 13 params via **AtomCardBase**, emitted as family-wide `--atom-card-*` custom properties — including `BorderWidth`/`BorderColor`, which decouple the frame from `AccentColor` (`BorderWidth="0"` removes it; previously the frame was a hardcoded 8px and `AccentColor="transparent"` both left its space as a gap and made the card face see-through). Separate components rather than one `CardEffect` enum because each needs a param the others can't use — the opposite call from `AtomTransition`, whose 20 effects share one enum precisely because they add nothing per-effect. 3D *tilt* deliberately lives in `BlazorAtoms.Transitions` as `HoverEffect.Tilt` (reveals nothing → needs no card structure; composes around any card). Still to build: AtomCard, AtomCardHeader, AtomCardBody, AtomCardFooter | JS-free |
| `BlazorAtoms.Tabs` | AtomTabs, AtomTab, AtomTabPanel | JS-free |
| `BlazorAtoms.Panels` | AtomAccordion, AtomCollapse, AtomPanel, AtomSplitter | accordion/collapse via native `<details>`; splitter uses pointer events — no JS |
| `BlazorAtoms.Tooltips` *(shipped)* | AtomTooltip — pure-CSS bubble (rect/pill/ellipse/thought/burst/folded); CSS placement + `:hover`/`:focus-within` · AtomShapedTooltip — outline drawn as inline SVG so border works on every shape (adds cloud) · AtomPaintedTooltip — SVG that also paints (gradient fill, SVG stroke, optional shadow). Shared `Placement`; per-component `Shape` enums. JS-free except the opt-in `Cursor` follow mode (self-loaded module). |
| `BlazorAtoms.Highlights` *(implemented)* | AtomHighlight — zero-JS highlighter for plain-text child content; renders `<mark>` during Blazor render, works in every render mode · AtomHighlightDeep — zero-JS highlighter for rich HTML content (mixed markup: headings, lists, tables, links) supplied as an HTML string; wraps matches in `<mark>` during Blazor's render pass, safe across re-renders. Both share `HighlightStyle` (`Mark`/`Underline`/`Outline`) and CSS variables. · AtomHighlighter — highlights keyword matches in the live DOM via a self-imported JS module scoped to its container; works through arbitrarily nested child components since it operates on real rendered output, not the render tree. |
| `BlazorAtoms.Alerts` | AtomAlert, AtomToast, AtomBanner, AtomCallout | toast timing via C# timer |
| `BlazorAtoms.Icons` | AtomIcon (+ optional curated SVG set) | renderer is 0-dep; a bundled icon set is larger |
| `BlazorAtoms.Typography` *(in progress)* | AtomTextCycle *(shipped)* — zero-JS vertical flip-cascade word rotator, cycles `Words` in an infinite loop; per-instance-generated `@keyframes` sized to word count, duplicate-first-word row for a seamless wrap. AtomTextScramble *(shipped)* — zero-JS one-shot per-character entrance animation for a single `Word` (7 effects), static scoped CSS (keyframes don't vary by instance), auto-replays on `Word` change, optional `Replay()` method for on-demand repeat. AtomTextLava *(shipped)* — zero-JS single word rising out of an animated molten-lava-gradient background; `Loop` (default on) bubbles up/down forever via `animation-direction:alternate` reusing one keyframe block, off rises once and holds. AtomTextSparkle *(shipped)* — zero-JS hover effect (layered 3D text-shadow, colorized glare sweep, scattered SVG sparkles), pure CSS `:hover`/`:active` trigger with no C# state at all; sparkle positions are a deterministic function of index (not `Random`) to avoid a hydration mismatch jump. Still to build: AtomHeading, AtomText (remaining hover/loop text effects), AtomCode, AtomKbd, AtomTruncate | JS-free |
| `BlazorAtoms.Clocks` *(shipped)* | AtomClock — live single-zone clock (server/browser/UTC or explicit zone), opt-out ticking, semantic `<time>` · AtomAnalogClock — same sources on a scalable SVG dial (hands, minute ticks, numerals) · AtomClockPair — two clocks (e.g. server + local) side-by-side or stacked · AtomClockStrip — N-zone world-clock row/grid/list (digital or analog, viewer highlight, relative offset, sort, select) · AtomTimeZoneMap — inline-SVG world map: continents + 24 UTC±N bands + day/night terminator + sun marker + accurate city pins · AtomTimeZonePicker — searchable combobox over every system zone (filter, region groups, per-zone offset, "use my zone" detect), `@bind-Value` on the IANA id | browser-tz detect via self-loaded JS module (opt-in); ticking via C# `PeriodicTimer`; city times/zone list via `TimeZoneInfo`; no map service/CDN/raster; else JS-free |

**What each does:**
- **Inputs** — the core form fields, two-way bound to your data. The seven standard ones (text, textarea, number, checkbox, switch, radio group, select) are shipped and share one base: one `@bind-Value` contract, one `EditContext`/`DataAnnotations`-aware error state, and `Variant`/`Size`/`Effect` axes over `--field-*` custom properties, so the whole family themes together. Each wraps a native control, so keyboard behavior, form submission, and mobile pickers come from the platform. `AtomRangeInput` is a labeled slider on the same `EditContext` contract (its own, older copy of the glue — see DEVELOPMENT.md).
- **Buttons** — clickable actions in variants: standard, icon-only, grouped, toggle, and split (button + dropdown arrow).
- **Cards** — `AtomCardReveal` (shipped) is a hover-reveal info card: title/subtitle/background
  image/dot indicator overlay plays a staggered entrance on mount, then slides away on hover to
  reveal a scrollable body panel — pure CSS, no C# trigger state. Still to build: a plain bordered
  content container with optional header/body/footer (`AtomCard`/`AtomCardHeader`/`AtomCardBody`/
  `AtomCardFooter`) — the box you put a summary or preview in.
- **Tabs** — switch between panels of content in the same space via a tab strip.
- **Panels** — expand/collapse content regions: accordion, collapsible section, and resizable splitter.
- **Tooltips** — small hover/focus hint bubbles shown beside an element.
- **Alerts** — messages to the user: inline alert/callout, a page banner, and toast (an auto-dismissing popup notification).
- **Icons** — an `AtomIcon` renderer for SVG glyphs, optionally paired with a bundled icon set.
- **Typography** — text primitives. `AtomTextCycle` (shipped) cycles a list of words/phrases in an
  infinite vertical flip-cascade loop, zero JS. `AtomTextScramble` (shipped) plays a one-shot
  per-character entrance animation for a single word (7 effects) — not a cycling component,
  auto-replays on word change, `Replay()` for on-demand repeat. `AtomTextLava` (shipped) rises a
  single word out of an animated molten-lava background, looping (bubbling) by default or
  one-shot via `Loop="false"`. `AtomTextSparkle` (shipped) is a hover effect (3D text-shadow,
  glare sweep, scattered sparkles) driven by pure CSS `:hover`/`:active` — no C# trigger state at
  all. Still to build: headings, body text, inline code, keyboard keys, truncation.
- **Clocks** — live time displays: `AtomClock` ticks a single zone (server, UTC, browser-local, or an explicit `TimeZoneInfo`); `AtomAnalogClock` shows the same on a scalable SVG dial; `AtomClockPair` shows two (e.g. server + local) side-by-side or stacked; `AtomClockStrip` shows N zones as a world-clock row/grid/list; `AtomTimeZoneMap` is a whole-earth timezone map (inline SVG continents + UTC±N bands + day/night terminator + accurate city pins); `AtomTimeZonePicker` is a searchable dropdown over every system timezone (filter, region groups, per-zone offset, auto-detect), two-way bound on the IANA id. Browser zone is auto-detected via a tiny self-loaded JS module.

## Tier C — heavier (needs JS interop or a dependency). Decide later; may dent the 0-dep goal.

| Library | Reason it's heavier |
|---|---|
| `BlazorAtoms.Canvas` *(shipped)* | AtomCanvas — declarative, serializable shape model (line/rect/circle/freehand path/text/image) with Static/Draw/Select/Pan modes, selection + zoom/pan view, + a batched imperative `Canvas2DContext` escape hatch · AtomSignaturePad — signature pad (`@bind-Value` PNG data-URL, Clear/Undo, PNG/SVG export) · AtomCanvasStudio — batteries-included, extensible workbench (toolbar, shape/stamp insert with click-to-place, pen/fill/bg, undo/redo, zoom/pan, layers panel, save/load JSON, PNG/SVG export) with a public slot + cascading-`AtomCanvasStudioContext` extension API (the family's first `CascadingValue`). The family's first intentional `<canvas>` — justified by the Graphics-policy hard-perf carve-out (60 fps freehand ink + `toDataURL` raster export SVG can't do). Ships and self-imports its own `atom-canvas.js` (no `<script>`/DI); gesture stays in JS so it's smooth on Server. |
| `BlazorAtoms.Overlays` (Modal, Drawer, Popover, Dropdown, Menu) | positioning + focus-trap + scroll-lock usually want JS |
| `BlazorAtoms.Pickers` (Date / Time / Color) | popup/calendar logic; color picker is closer to JS-free |
| `BlazorAtoms.Tables` / `DataGrid` | a simple table is JS-free; sorting/virtualization is heavy |
| `BlazorAtoms.Navigation` *(shipped)* | AtomScrollTo — scroll-to-top/bottom or scroll-to-anchor button; default SVG chevron or custom-icon slot, tooltip/aria, page-or-container scope, optional auto-hide-until-scrolled (passive + rAF-coalesced watcher); self-imports `atom-navigation.js` for smooth scroll + visibility. *Planned:* Breadcrumbs, Pagination, Stepper. |
| `BlazorAtoms.Behaviors` *(shipped)* | **AtomBrowserSupport** — cached runtime CSS-feature-support check (`CSS.supports()` via a tiny self-imported JS module) · **TransitionState** — reusable enter/exit animation state machine, the engine behind `BlazorAtoms.Transitions`'s `AtomTransition`, usable directly by future components (carousel, text animation, image effects) without wrapper markup. *Planned:* ClickOutside, FocusTrap, Clipboard, Portal. |

**What each does:**
- **Canvas** — native `<canvas>` drawing behind a clean C# API: a declarative shape model, freehand ink (signature capture), and drag-to-move, plus a batched raw-2D-context escape hatch. Tops out at `AtomCanvasStudio`, a full drop-in workbench (toolbar/stamps/layers/undo-redo/zoom/save-load) that is extensible via slots + a cascading context. The family's first raster surface; owns its own tiny JS module so consumers write no interop.
- **Overlays** — things that float above the page: modal dialog, side drawer, popover, dropdown, menu.
- **Pickers** — specialized value selectors for date, time, and color.
- **Tables** — tabular data display, from a simple static table up to a sortable/virtualized data grid.
- **Navigation** — moving around an app. Shipped: `AtomScrollTo` (scroll-to-top/bottom/anchor button, page or container scope, auto-hide-until-scrolled). Planned: breadcrumbs, pagination, stepper.
- **Behaviors** — headless helpers with no visuals of their own. Shipped: `AtomBrowserSupport` (runtime CSS-feature detection) and `TransitionState` (the enter/exit engine behind `AtomTransition`). Planned: click-outside, focus-trap, clipboard copy, portal.

---

## Suggested first wave (after ActivityIndicators)

`Progress` → `Skeletons` → `Badges` → `Charts`

All Tier A: they prove the packaging template, ship quickly, and keep the 0-dependency guarantee.

> Status: brainstorm / not committed to. Names and groupings are provisional — refine before scaffolding.
