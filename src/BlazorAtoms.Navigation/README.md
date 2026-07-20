# BlazorAtoms.Navigation

Navigation components for Blazor. Ships **`AtomScrollTo`** — a scroll-to-top / scroll-to-bottom
(or scroll-to-anchor) button. Renders a default SVG chevron, takes a custom icon, shows a tooltip,
and optionally auto-hides until the user scrolls. Works on the whole page or the nearest scrollable
container, and can jump to a named `<a>` anchor / element id.

Smooth scrolling and the auto-hide watcher run through a small JS module the component lazily
imports itself — no `<script>` tag, no DI registration, no setup.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Navigation\BlazorAtoms.Navigation.csproj" />
```
```razor
@using BlazorAtoms.Navigation
```

## AtomScrollTo

```razor
@* Classic floating "back to top" — pins to the viewport corner (no host CSS), appears after 300px *@
<AtomScrollTo Position="ScrollPosition.FixedBottomRight" VisibleAfter="300" Tooltip="Back to top" />

@* Scroll the nearest scrollable container to the bottom *@
<AtomScrollTo Direction="ScrollDirection.Down" Scope="ScrollScope.Container"
              Tooltip="Jump to bottom" />

@* Jump to a named anchor / element id *@
<AtomScrollTo Target="section-3" Tooltip="Go to section 3" />

@* Custom icon instead of the default chevron *@
<AtomScrollTo Tooltip="Top">
    <MyRocketIcon />
</AtomScrollTo>

@* Two at once — top + bottom, both pinned to the viewport with zero host CSS *@
<AtomScrollTo Direction="ScrollDirection.Up"   Position="ScrollPosition.FixedTopRight"    Tooltip="Top" />
<AtomScrollTo Direction="ScrollDirection.Down" Position="ScrollPosition.FixedBottomRight" Tooltip="Bottom" />
```

### Targeting

- **Top / bottom** — leave `Target` unset. `Direction` picks which end (and the default arrow glyph):
  `Up` scrolls to the start, `Down` to the end.
- **Anchor / element** — set `Target` to a bare id/anchor name (`"section-3"`), an id selector
  (`"#section-3"`), or any CSS selector. The component resolves id → `[name=...]` → `querySelector`
  in that order and calls `scrollIntoView`.
- **Scope** — `Page` (default) scrolls the window; `Container` scrolls the nearest scrollable
  ancestor of the button.
- **`ScrollContainer`** — a CSS selector naming the scroll box explicitly (`"#log-panel"`). Use it
  when the button is *not* inside the box it should scroll — e.g. an overlay sibling (see the
  in-panel pattern below). When set it overrides the `Scope` ancestor-walk. Unset = ancestor-walk.

### In-panel scroller (pinned to a scrollable box)

To keep a button in a scrollable panel's visible corner, wrap the scroll box in a
non-scrolling `position:relative` container and make the button an **overlay sibling** — an
absolute child *inside* a scroller scrolls away with the content; a sibling of the scroller pins to
the (non-scrolling) wrapper instead. Bind it to the box with `ScrollContainer`:

```razor
<div style="position:relative">
    <div id="log-panel" style="height:280px; overflow-y:auto">
        @* …scrollable content… *@
    </div>

    <AtomScrollTo ScrollContainer="#log-panel" Scope="ScrollScope.Container"
                  Position="ScrollPosition.AbsoluteBottomRight"
                  Direction="ScrollDirection.Down" Tooltip="Jump to newest" />
</div>
```

The wrapper doesn't scroll, so the `Absolute*` button parks in the panel corner; `ScrollContainer`
makes the click (and any `VisibleAfter` watcher) act on the inner box, not the page.

### Positioning (`Position`, no host CSS)

`Position` pins the button for you — no `position:fixed` boilerplate, no host stylesheet:

- **`Inline`** *(default)* — the button flows in place where the tag sits. Position it yourself via
  the inherited `Style` / `CssClass` if you want something custom.
- **`Fixed*`** (`FixedBottomRight`, `FixedBottomLeft`, `FixedTopRight`, `FixedTopLeft`,
  `FixedBottomCenter`, `FixedTopCenter`) — `position:fixed`; pins to that **viewport** corner and
  stays put as the page scrolls. Works anywhere with zero host CSS.
- **`Absolute*`** (`AbsoluteBottomRight`, `AbsoluteBottomLeft`, `AbsoluteTopRight`,
  `AbsoluteTopLeft`) — `position:absolute`; pins to that corner of the **nearest positioned
  ancestor** and scrolls with the page. ⚠️ That ancestor must be positioned
  (`position:relative` or similar) — the one place this may need a one-line host edit. If no
  ancestor is positioned, the button falls back to the viewport.

`OffsetV` / `OffsetH` (each default `1.5rem`) set the distance from the pinned vertical (top/bottom)
and horizontal (left/right) edges independently. Any CSS length works — `px`, `rem`, `%`, `vh`,
`clamp()`, or `calc(1rem + env(safe-area-inset-bottom))` for an iOS home-bar-safe float. `OffsetH`
is ignored for `*Center` positions (they center via `translate`). Override the stacking order with
the `--scrollto-z` custom property (defaults: 1000 for fixed, 10 for absolute).

### Multiple buttons

Every `AtomScrollTo` is independent — render as many as you like. A top + bottom pair is just two
tags with opposite `Direction` + opposite `Position` (see the two-at-once example above).

### Auto-hide

`VisibleAfter` (px) hides the button until the scroll position passes that threshold, then fades it
in — the familiar auto-appearing back-to-top affordance. The watcher uses a **passive** scroll
listener coalesced through `requestAnimationFrame`, so it touches the DOM at most once per frame
regardless of scroll frequency. Leave `VisibleAfter` null to keep the button always visible.

### Avoiding overlap (`HideNear`)

A floating button can cover content behind it. `HideNear` takes a CSS selector; while any matching
element is visible in the scroller, an `IntersectionObserver` fades the button out, and it returns
when that element scrolls away — the standard "don't cover the footer" behaviour:

```razor
<AtomScrollTo Position="ScrollPosition.FixedBottomRight" VisibleAfter="300"
              HideNear="footer" Tooltip="Back to top" />
```

`HideNear` combines with `VisibleAfter` (both must allow it for the button to show). For simpler
cases you can also dim the button at rest with `Style="opacity:.55"` (bring it to full on `:hover`
via `CssClass`), reserve a corner with content `padding`, or pick an emptier `Position`.

### Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Target` | `string?` | `null` | Anchor name / element id / CSS selector. Wins over `Direction`. |
| `Direction` | `ScrollDirection` | `Up` | `Up` or `Down` — target end + default arrow. |
| `Scope` | `ScrollScope` | `Page` | `Page` (window) or `Container` (nearest scrollable ancestor). |
| `ScrollContainer` | `string?` | `null` | CSS selector naming the scroll box explicitly (for overlay-sibling buttons). Overrides `Scope` when set. |
| `Motion` | `ScrollMotion` | `Smooth` | `Smooth` or `Auto` — maps to DOM `ScrollOptions.behavior`. |
| `ChildContent` | `RenderFragment?` | `null` | Custom icon. Null = default SVG chevron. |
| `Tooltip` | `string?` | `null` | `title` attribute; also `aria-label` when set. |
| `Color` | `string?` | `#ffffff` | Icon color → `--scrollto-color`. |
| `Background` | `string?` | `#2563eb` | Button background → `--scrollto-bg`. |
| `Size` | `string?` | `44px` | Button diameter → `--scrollto-size`. |
| `Radius` | `string?` | `50%` | Corner radius → `--scrollto-radius`. |
| `Position` | `ScrollPosition` | `Inline` | Self-pin to a viewport (`Fixed*`) or ancestor (`Absolute*`) corner — no host CSS. |
| `OffsetV` | `string?` | `1.5rem` | Distance from the pinned top/bottom edge (Fixed*/Absolute* only) → `--scrollto-offset-v`. Any CSS length (`px`/`rem`/`%`/`vh`/`calc`/`clamp`/`env`). |
| `OffsetH` | `string?` | `1.5rem` | Distance from the pinned left/right edge → `--scrollto-offset-h`. Ignored for `*Center`. |
| `ArrowStrokeWidth` | `double` | `2` | Default-arrow stroke width (ignored with custom icon). |
| `VisibleAfter` | `int?` | `null` | Auto-hide until scrolled this many px. Null = always visible. |
| `HideNear` | `string?` | `null` | CSS selector of an element the button must not cover — it fades out while that element is on-screen (IntersectionObserver) and returns when it leaves. |
| `OnScrolled` | `EventCallback` | — | Fires after a click-triggered scroll. |
| `OnVisibilityChanged` | `EventCallback<bool>` | — | Fires when auto-hide state flips (`true` = visible). |

Plus the shared escape hatch on every Atom component (from `AtomComponentBase`): `CssClass`,
`Style`, and arbitrary splatted attributes (`title`, `data-*`, `id`, ARIA, event handlers, …,
including inline `position:fixed` styling to float the button) on the root `<button>`.

### Styling

The root `<button class="atom-scroll-to">` exposes four CSS custom properties
(`--scrollto-color`, `--scrollto-bg`, `--scrollto-size`, `--scrollto-radius`) set by the params
above; override them (or the whole look) via `CssClass` / `Style`. Default arrow inherits
`currentColor`, so setting `Color` recolors it.

## Notes

- **Render modes.** JS interop can't run during static SSR / prerender — the button renders its
  markup first and wires up scrolling once interactive. Clicks before hydration are no-ops.
- **Exception handling.** Every interop call is wrapped; `JSDisconnectedException`,
  `OperationCanceledException`, and a not-found selector (`JSException`) are swallowed so a dead
  circuit or a bad target never throws into your UI.
- **Cleanup.** The visibility watcher and the imported module are released in `DisposeAsync`; the
  passive scroll listener is removed from the DOM.
