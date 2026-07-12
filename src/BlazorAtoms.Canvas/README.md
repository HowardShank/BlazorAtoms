# BlazorAtoms.Canvas

Native HTML `<canvas>` drawing for Blazor with a clean C# API — you never write JS interop. One library,
two components:

- **`AtomCanvas`** — a drawing surface driven by a **declarative, serializable shape model** (line, rect,
  circle, freehand path, text, image). A `Mode` switch turns the same canvas into three tools: **Static**
  (render), **Draw** (freehand ink at 60 fps — this is signature capture), and **Select** (hit-test + drag
  to move shapes). An imperative **`Canvas2DContext`** escape hatch exposes the raw 2D context in C#.
- **`AtomSignaturePad`** — a ready-made signature pad built over `AtomCanvas` freehand mode: bind `Value`
  (a PNG data URL), `Clear()` / `UndoAsync()`, export PNG or SVG.

The component ships and **self-imports its own tiny JS module** (`_content/BlazorAtoms.Canvas/atom-canvas.js`)
on first render — no `<script>` tag, no DI, no setup, no third-party runtime dependency. Server or WebAssembly.

> **Why canvas (and JS) here?** The rest of the BlazorAtoms family is pure SVG/CSS. Freehand ink is the
> exception SVG can't serve: a `pointermove` stream at ~60 Hz would round-trip to the server on every point.
> This library keeps the whole gesture in JS and calls back to C# **once per stroke**, so drawing is smooth
> even on a Blazor Server circuit — and `toDataURL` gives you a raster export SVG can't.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Canvas\BlazorAtoms.Canvas.csproj" />
```
```razor
@using BlazorAtoms.Canvas
```
Link `{App}.styles.css` (the scoped-CSS bundle), as with any RCL. Nothing else to wire up.

## AtomSignaturePad

```razor
<AtomSignaturePad @ref="pad" @bind-Value="png" PenColor="#111827" PenWidth="2.5"
                  BackgroundColor="#ffffff" Width="420" Height="180" />

<button @onclick="() => pad.Clear()">Clear</button>
<button @onclick="() => pad.UndoAsync()">Undo</button>

@if (!string.IsNullOrEmpty(png))
{
    <img src="@png" alt="signature preview" />
}

@code {
    AtomSignaturePad pad = default!;
    string? png;   // a PNG data URL, refreshed after every stroke
}
```

`Value` is a PNG **data URL**, updated after each stroke — POST it or store it directly. Need the vector
form instead? Bind `Strokes` (the `CanvasPath` list) for replay/editing, or call `ToSvg()`.
`IsEmpty`, `OnStart` / `OnEnd` / `OnChange` round out the surface.

## AtomCanvas

```razor
@* Static: render a declarative scene *@
<AtomCanvas Width="320" Height="200" BackgroundColor="#fff" Shapes="scene" />

@* Draw: freehand ink, two-way bound to a stroke model *@
<AtomCanvas Width="320" Height="200" Mode="CanvasMode.Draw" @bind-Shapes="ink" PenColor="#0ea5e9" />

@* Select: click + drag shapes to move them *@
<AtomCanvas Width="320" Height="200" Mode="CanvasMode.Select" @bind-Shapes="scene" />

@code {
    List<CanvasShape> scene = new()
    {
        new CanvasRect(20, 20, 120, 80, Radius: 10) { Fill = "#fde68a", Stroke = "#f59e0b" },
        new CanvasCircle(220, 90, 45) { Fill = "#bfdbfe", Stroke = "#3b82f6" },
        new CanvasText(20, 170, "Hello canvas", FontSize: 20) { Fill = "#111827" },
    };
    List<CanvasShape> ink = new();
}
```

### Shapes

The model is an immutable, `System.Text.Json`-polymorphic record hierarchy (`CanvasShape`):
`CanvasLine`, `CanvasRect` (optional corner `Radius`), `CanvasCircle`, `CanvasPath` (a freehand/polyline
stroke, `Smooth`/`Closed`), `CanvasText`, `CanvasImage`. Every shape carries optional `Stroke`,
`StrokeWidth`, `Fill`, `Opacity`, a stable `Id`, and `Draggable`. Coordinates are in CSS pixels; the
backing store is scaled by `devicePixelRatio` for crisp lines automatically.

`Draw` appends a `CanvasPath` and raises `ShapesChanged` (and `OnDrawEnd`) on pointer-up. `Select` picks the
top-most `Draggable` shape and, on release, translates it in the model. `Static` raises `OnShapeClick` with
the tapped shape's `Id`. The model is always the source of truth — every change is one authoritative redraw.

### Imperative escape hatch

For custom drawing, get a **batched** 2D context — one interop round-trip per `FlushAsync` (so it works on
Server):

```razor
<AtomCanvas @ref="canvas" Width="300" Height="150" />

@code {
    AtomCanvas canvas = default!;

    async Task Draw()
    {
        var ctx = await canvas.GetContext2DAsync();
        ctx.FillStyle("#0ea5e9").FillRect(10, 10, 120, 60)
           .StrokeStyle("#111").LineWidth(3).StrokeRect(10, 10, 120, 60);
        await ctx.FlushAsync();
    }
}
```

Or use `OnPaint` (fired with a context after each model redraw) to layer custom drawing that **survives**
redraws. Mixing the raw context with `@bind-Shapes` on the same canvas otherwise fights the authoritative
redraw — pick one per canvas, or paint via `OnPaint`.

## Parameters

### `AtomCanvas`

| Parameter | Type | Notes |
|-----------|------|-------|
| `Width` / `Height` | `double` | CSS px. Backing store scaled by `devicePixelRatio`. |
| `BackgroundColor` | `string?` | Canvas background; null is transparent. |
| `Mode` | `CanvasMode` | `Static` (default) / `Draw` / `Select`. |
| `Shapes` / `ShapesChanged` | `IReadOnlyList<CanvasShape>?` | The model. `@bind-Shapes` for draw/drag. |
| `PenColor` / `PenWidth` / `PenSmoothing` | `string` / `double` / `bool` | Freehand + shape defaults. |
| `Disabled` | `bool` | Ignore input (rendering continues). |
| `AriaLabel` | `string?` | Canvas is opaque to AT — the only description it gets. `role="img"`. |
| `OnDrawStart` / `OnDrawEnd` | `EventCallback` / `EventCallback<CanvasPath>` | Freehand gesture. |
| `OnShapeClick` | `EventCallback<string>` | Tapped shape id (Static). |
| `OnPaint` | `EventCallback<Canvas2DContext>` | Overlay draw after each redraw. |

Methods: `GetContext2DAsync()`, `ToDataUrlAsync(type, quality)`, `ToSvg()`.

### `AtomSignaturePad`

`Value`/`ValueChanged` (PNG data URL), `Strokes`/`StrokesChanged` (vector), `PenColor` (`#111827`),
`PenWidth` (`2.5`), `BackgroundColor` (`#ffffff`), `Width` (`400`), `Height` (`160`), `Disabled`,
`AriaLabel` (`Signature`), `OnStart`/`OnEnd`/`OnChange`. Methods: `Clear()`, `UndoAsync()`,
`ToPngDataUrlAsync()`, `ToSvg()`, `IsEmpty`.

## Notes

- **Render modes.** During static SSR / prerender the `<canvas>` renders empty (any child content is the
  fallback); the engine initializes in `OnAfterRenderAsync` and starts drawing once the component is
  interactive (`InteractiveServer` / `InteractiveWebAssembly` / `InteractiveAuto`).
- **Accessibility.** A canvas is opaque to assistive tech. Always set `AriaLabel`; provide visible controls
  (the signature pad's Clear/Undo). It is not a native form control.
- **Escape hatch.** Every component also takes the shared `CssClass` / `Style` / arbitrary-attribute splat
  from `AtomComponentBase`.
