# BlazorAtoms.Scrollbars

Custom-styled scrollbars for Blazor. Ships **`AtomScrollbar`** — a generic wrapper that gives its
child content its own scroll box with a themed scrollbar (color, size, gradient, radius, border,
hover), replacing the browser default per-instance.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Scrollbars\BlazorAtoms.Scrollbars.csproj" />
```
```razor
@using BlazorAtoms.Scrollbars
```

## AtomScrollbar

```razor
<AtomScrollbar BoxHeight="300px" ThumbColor="#555" TrackColor="#f5f5f5">
    ... long content ...
</AtomScrollbar>
```

Wraps arbitrary `ChildContent` in a `overflow: auto` box. No trigger, no state — purely
declarative styling, always on.

### Cross-browser behavior

- **WebKit (Chrome/Edge/Safari)**: full control via `::-webkit-scrollbar` /
  `::-webkit-scrollbar-track` / `::-webkit-scrollbar-thumb` / `::-webkit-scrollbar-thumb:hover`,
  scoped to this component's own scroll box — every parameter below applies.
- **Firefox**: only exposes `scrollbar-color` (a single thumb + track color pair) and
  `scrollbar-width` (`thin`/`auto`/`none` keywords, not an arbitrary length). `ScrollbarSize` is
  heuristically mapped to `thin` at 10px or below, `auto` above. Gradient, radius, border, and
  hover color have no Firefox equivalent, so it falls back to solid `ThumbColor`/`TrackColor`.

### Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | — | Content to scroll. |
| `Axis` | `ScrollbarAxis` | `Vertical` | `Vertical` / `Horizontal` / `Both` — sets which `overflow-*` is `auto` vs `hidden`. |
| `BoxHeight` | `string` | `"300px"` | Scroll box height. Any CSS length. Matters for `Vertical`/`Both`. |
| `BoxWidth` | `string` | `"100%"` | Scroll box width. Any CSS length. |
| `ScrollbarSize` | `string` | `"12px"` | Thumb/track thickness (WebKit `width`/`height`); also drives the Firefox thin/auto fallback. |
| `TrackColor` | `string` | `"#f5f5f5"` | Track background color. |
| `TrackBorderRadius` | `string` | `"0px"` | Track corner radius (WebKit only). |
| `ThumbColor` | `string` | `"#555"` | Thumb color (and Firefox's solid fallback). |
| `ThumbGradientEnd` | `string?` | `null` | Optional 2nd stop — thumb becomes a linear gradient on WebKit. Ignored on Firefox. |
| `ThumbGradientAngle` | `string` | `"180deg"` | Gradient angle, only used when `ThumbGradientEnd` is set. |
| `ThumbHoverColor` | `string?` | `null` | Thumb color while hovered (WebKit only). Defaults to `ThumbColor`/gradient (no visible change) when unset. |
| `ThumbBorderRadius` | `string` | `"0px"` | Thumb corner radius (WebKit only). |
| `ThumbBorder` | `string?` | `null` | Raw CSS `border` shorthand for the thumb, e.g. `"2px solid #555555"` (WebKit only). |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root `<div>`.

## Notes

- **Zero JS.** Purely declarative CSS — no JS module, no `BlazorAtoms.Behaviors` dependency, this
  package has zero BlazorAtoms deps.
- **One instance, one scrollbar.** Each `<AtomScrollbar>` styles only its own scroll box (not a
  global override), so different instances can carry different themes on the same page.
- **`ScrollbarSize` and `scrollbar-width`.** Firefox's `scrollbar-width` keywords are coarser than
  a CSS length — a numeric size like `"12px"` is heuristically bucketed to `thin`/`auto`, it isn't
  applied verbatim there the way it is on WebKit.
