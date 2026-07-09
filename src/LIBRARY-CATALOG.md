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

Every graphic the components draw is **inline SVG** — vector, crisp at any zoom/DPI, themeable via
CSS custom properties and `currentColor`, animatable in CSS. This covers charts, gauges, sparklines,
rating icons, QR/barcodes, skeleton shapes, and icons.

- **No raster for our own chrome** — no PNG/JPG UI assets, ever.
- **Avoid `<canvas>`** (it is raster *and* needs JS); use SVG unless there is a hard performance reason.
- The only raster we own is the 128×128 NuGet package icon (packaging metadata, never shipped into
  apps). **User content** (an avatar photo, an image inside a card) is raster by nature — we render
  it inside SVG/CSS chrome but don't control its quality; our frame is always vector.

---

## Tier A — strong fit (pure SVG/CSS, JS-free). Build these first.

| Library | Components (`Atom*`) |
|---|---|
| `BlazorAtoms.ActivityIndicators` *(shipped)* | AtomActivityGears, AtomActivityDna, AtomActivityFunnel, AtomActivityHourglass, AtomActivityMagnifier, AtomActivityNeural, AtomActivitySwarm, AtomPulseBar, AtomPulseScanner |
| `BlazorAtoms.Progress` | AtomProgressBar, AtomProgressRing, AtomProgressSteps, AtomMeter |
| `BlazorAtoms.Skeletons` | AtomSkeletonText, AtomSkeletonBlock, AtomSkeletonAvatar, AtomSkeletonCard |
| `BlazorAtoms.Charts` | AtomSparkline, AtomBarChart, AtomLineChart, AtomDonut, AtomGauge |
| `BlazorAtoms.Badges` *(shipped)* | AtomStaticBadge — no-animation count/label badge in many shapes (pill/circle/square/rounded + SVG star/hexagon/diamond/shield/burst/ribbon) · AtomAnimatedBadge — badge that pops in on value with Pop/Bounce/Spin/Pulse/Ping motion. Both overlay a host corner or render inline; any object value w/ type-aware formatting · AtomChip — interactive chip (icon slot, click/select, removable) · AtomTag — display categorization label (rounded rect, removable) · AtomPill — status pill (fully-rounded soft tint + leading dot). Chip family shares Variant + Solid/Soft/Outline Appearance. |
| `BlazorAtoms.Avatars` *(shipped)* | AtomAvatar — silhouette (solid/gradient) or image, cropped to circle/square/rounded/squircle/hexagon, bg color/gradient+angle, border ring · AtomInitialsAvatar — initials from name w/ deterministic palette color · AtomAvatarGroup — overlapping stack from names w/ "+N" overflow |
| `BlazorAtoms.Ratings` | AtomRating (stars/hearts), AtomRatingInput |
| `BlazorAtoms.Layout` | AtomStack, AtomGrid, AtomDivider, AtomSpacer, AtomCenter, AtomAspectRatio |
| `BlazorAtoms.Transitions` | AtomFade, AtomSlide, AtomCollapse, AtomScale (CSS-only) |
| `BlazorAtoms.Barcodes` *(implemented)* | AtomBarcode (1D), AtomQrCode (2D) — own C# encoder → SVG; generation only |

On-brand standout: **`Charts`** — SVG, JS-free, same DNA as the activity indicators.

**What each does:**
- **ActivityIndicators** — animated "working…" loaders (gears, DNA, pulse bars) for when a task is running with no known progress %.
- **Progress** — shows how far along a task is: a filling bar, ring, or step tracker. Use when you *do* know the percentage (uploads, wizards).
- **Skeletons** — grey placeholder shapes shown while data loads, roughly matching the layout of the content to come, usually with a shimmer (the grey boxes YouTube/LinkedIn show before content paints). Less jarring than a spinner. Swap in the real component when data arrives.
- **Charts** — small data visuals drawn as SVG (sparkline, bar, line, donut, gauge) — lightweight, no charting library.
- **Badges** — tiny labels or counts attached to things: status pills, tags, the red "3" notification count on an icon.
- **Avatars** — circular/square images that stand in for a person or entity. `AtomInitialsAvatar` falls back to initials (or a placeholder) when there's no photo; `AtomAvatarGroup` stacks several with overlap and a "+N" overflow. Common in team lists, comment threads, and assignee rows.
- **Ratings** — star/heart scales. `AtomRating` displays a read-only score (★★★★☆ 4/5); `AtomRatingInput` lets the user hover/click to set one (product reviews, feedback). Supports half-steps, a custom icon, and an optional count label.
- **Layout** — invisible structural building blocks: stack (spacing), grid, divider, spacer, centering — the scaffolding you arrange other components inside.
- **Transitions** — reusable enter/leave animations (fade, slide, collapse, scale) wrapped around content that appears or disappears. CSS-only.
- **Barcodes** — machine-readable graphics generated from a value, rendered as SVG. "Barcode" is the umbrella: `AtomBarcode` for 1D/linear (Code128, EAN-13, Code39…) and `AtomQrCode` for 2D/matrix (QR). **Generation only, using our own C# encoder** — a third-party lib (QRCoder/ZXing) would break the 0-dep rule. Note: *reading/scanning* a code needs a camera + JS → a separate Tier C concern, not part of this library. *(Implemented: Code39, Code128, EAN-13, UPC-A, ITF, Codabar (1D) + QR byte-mode v1–40 (2D), verified by ZXing round-trip / reference tests.)*

## Tier B — feasible, minor/optional JS

| Library | Components (`Atom*`) | Note |
|---|---|---|
| `BlazorAtoms.Inputs` | AtomTextField, AtomTextArea, AtomNumberField, AtomCheckbox, AtomRadioGroup, AtomSwitch, AtomSlider, AtomSelect | core forms; JS-free doable |
| `BlazorAtoms.Buttons` | AtomButton, AtomIconButton, AtomButtonGroup, AtomToggleButton, AtomSplitButton | JS-free |
| `BlazorAtoms.Cards` | AtomCard, AtomCardHeader, AtomCardBody, AtomCardFooter | JS-free |
| `BlazorAtoms.Tabs` | AtomTabs, AtomTab, AtomTabPanel | JS-free |
| `BlazorAtoms.Panels` | AtomAccordion, AtomCollapse, AtomPanel, AtomSplitter | accordion/collapse via native `<details>`; splitter uses pointer events — no JS |
| `BlazorAtoms.Tooltips` *(shipped)* | AtomTooltip — pure-CSS bubble (rect/pill/ellipse/thought/burst/folded); CSS placement + `:hover`/`:focus-within` · AtomShapedTooltip — outline drawn as inline SVG so border works on every shape (adds cloud) · AtomPaintedTooltip — SVG that also paints (gradient fill, SVG stroke, optional shadow). Shared `Placement`; per-component `Shape` enums. JS-free except the opt-in `Cursor` follow mode (self-loaded module). |
| `BlazorAtoms.Alerts` | AtomAlert, AtomToast, AtomBanner, AtomCallout | toast timing via C# timer |
| `BlazorAtoms.Icons` | AtomIcon (+ optional curated SVG set) | renderer is 0-dep; a bundled icon set is larger |
| `BlazorAtoms.Typography` | AtomHeading, AtomText, AtomCode, AtomKbd, AtomTruncate | JS-free |

**What each does:**
- **Inputs** — the core form fields (text, number, checkbox, radio, switch, slider, select), two-way bound to your data.
- **Buttons** — clickable actions in variants: standard, icon-only, grouped, toggle, and split (button + dropdown arrow).
- **Cards** — a bordered content container with optional header/body/footer; the box you put a summary or preview in.
- **Tabs** — switch between panels of content in the same space via a tab strip.
- **Panels** — expand/collapse content regions: accordion, collapsible section, and resizable splitter.
- **Tooltips** — small hover/focus hint bubbles shown beside an element.
- **Alerts** — messages to the user: inline alert/callout, a page banner, and toast (an auto-dismissing popup notification).
- **Icons** — an `AtomIcon` renderer for SVG glyphs, optionally paired with a bundled icon set.
- **Typography** — text primitives: headings, body text, inline code, keyboard keys, truncation.

## Tier C — heavier (needs JS interop or a dependency). Decide later; may dent the 0-dep goal.

| Library | Reason it's heavier |
|---|---|
| `BlazorAtoms.Overlays` (Modal, Drawer, Popover, Dropdown, Menu) | positioning + focus-trap + scroll-lock usually want JS |
| `BlazorAtoms.Pickers` (Date / Time / Color) | popup/calendar logic; color picker is closer to JS-free |
| `BlazorAtoms.Tables` / `DataGrid` | a simple table is JS-free; sorting/virtualization is heavy |
| `BlazorAtoms.Navigation` (Breadcrumbs, Pagination, Stepper) | mostly fine; some scroll/JS |
| `BlazorAtoms.Behaviors` (ClickOutside, FocusTrap, Clipboard, Portal) | headless, but several need JS interop |

**What each does:**
- **Overlays** — things that float above the page: modal dialog, side drawer, popover, dropdown, menu.
- **Pickers** — specialized value selectors for date, time, and color.
- **Tables** — tabular data display, from a simple static table up to a sortable/virtualized data grid.
- **Navigation** — moving around an app: breadcrumbs, pagination, stepper.
- **Behaviors** — headless helpers with no visuals of their own: click-outside, focus-trap, clipboard copy, portal.

---

## Suggested first wave (after ActivityIndicators)

`Progress` → `Skeletons` → `Badges` → `Charts`

All Tier A: they prove the packaging template, ship quickly, and keep the 0-dependency guarantee.

> Status: brainstorm / not committed to. Names and groupings are provisional — refine before scaffolding.
