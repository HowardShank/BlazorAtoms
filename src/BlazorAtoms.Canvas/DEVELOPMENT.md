# BlazorAtoms.Canvas — Development Notes

Internal architecture and design-rationale notes for maintainers of this library. Not needed to *use*
`AtomCanvas`, `AtomSignaturePad`, or `AtomCanvasStudio` — see `README.md` for the public, usage-facing docs.

## Why canvas (and JS) instead of SVG

The rest of the BlazorAtoms family is pure SVG/CSS. Freehand ink is the exception SVG can't serve: a
`pointermove` stream at ~60 Hz would round-trip to the server on every point if it were driven the way the
SVG-based components are. This library instead keeps the whole gesture in JS and calls back to C# **once
per stroke**, so drawing stays smooth even on a Blazor Server circuit — and `toDataURL` gives a raster
export that SVG can't produce directly.

## Division of labor: JS owns the gesture, C# owns the model

From `wwwroot/atom-canvas.js`:

- **C#** owns the shape model (`Shapes`) and the view (`Scale`/`PanX`/`PanY`).
- **JS** owns the 60fps pointer gesture and the pixels.

**Invariant:** `render()` is authoritative — it clears the canvas and fully redraws from the serialized
model every time it runs. C# only mutates the model/view at gesture *commit* (`pointerup`), never mid-gesture,
so `render()` is guaranteed to never run while a gesture is in flight. This is what makes freehand ink /
drag / pan smooth on a Blazor Server circuit: high-frequency `pointermove` events are handled entirely in
JS and never cross the SignalR interop boundary — only one call per completed gesture does
(`OnStrokeCommitted`, `OnShapeMoved`, `NotifyViewChanged`, etc., see the `[JSInvokable]` methods on
`AtomCanvas.razor.cs`).

`render()` also defensively no-ops if a gesture is mid-flight (`if (!st.drawing) redraw(...)`), since C#
has no way to change the model while JS hasn't yet reported the gesture as committed anyway.

## Coordinate spaces and the view transform

Shapes are stored and serialized in **world space**. The view transform maps world → screen:

```js
ctx.setTransform(dpr * scale, 0, 0, dpr * scale, dpr * panX, dpr * panY)
```

(`panX`/`panY` are tracked in CSS px; `dpr` is `window.devicePixelRatio`.)

Pointer events are mapped screen → world for hit-testing, freehand capture, and click-to-place, so all of
those stay correct under zoom/pan:

```js
worldX = (clientX - rect.left - panX) / scale
worldY = (clientY - rect.top  - panY) / scale
```

**DPR scaling:** the canvas backing store is sized to `Math.round(cssSize * dpr)` while the CSS
width/height stay at the logical size (`el.style.width/height`). Combined with the `dpr * scale` transform
factor, this is what keeps strokes crisp on high-DPI displays without the consumer having to think about
pixel density — see `applySize()` in `atom-canvas.js`.

Selection-highlight stroke width and dash pattern are drawn in *screen* space by dividing by `scale`
(`inv = 1 / scale`) so the highlight doesn't get thicker/thinner as the user zooms — see `drawSelection()`.

## Redraw / interop lifecycle

- `AtomCanvas.OnAfterRenderAsync(firstRender: true)` imports the JS module, calls `init()` once (binds
  pointer listeners, sizes the canvas), then calls `SyncAsync()`.
- Any later parameter or model change sets `_dirty = true` in `OnParametersSet()`; the next
  `OnAfterRenderAsync` call runs `SyncAsync()` again.
- `SyncAsync()` serializes `CurrentShapes` to JSON and calls the JS `render()` export — always a full,
  authoritative clear + redraw, never an incremental patch. If `OnPaint` is wired, it fires after that
  redraw with a batched `Canvas2DContext`, and the component flushes it once.
- The JS → C# callbacks (`NotifyDrawStart`, `OnStrokeCommitted`, `OnShapeMoved`, `OnShapeClicked`,
  `NotifyShapeSelected`, `NotifyCanvasClick`, `NotifyViewChanged`) fire at most once per gesture, on
  `pointerup` — never per `pointermove`. This is the mechanism, not just a stated behavior: it's the reason
  this library can afford a full re-render-from-model on every sync instead of needing incremental canvas
  patching.

## Imperative context batching

`Canvas2DContext` (the `GetContext2DAsync()` / `OnPaint` escape hatch) queues method/property calls in C#
and sends the entire batch to JS in one `InvokeVoidAsync` on `FlushAsync()`, which JS applies via
`runCommands()` (a simple dispatch over a `SETTERS` allow-list for properties vs. method calls for
everything else). This keeps the escape hatch usable on Blazor Server — one interop round trip no matter
how many draw calls are queued — rather than one round trip per canvas API call.

## SSR / prerender

During static SSR / prerender there is no `OnAfterRenderAsync` call yet, so the JS module isn't imported
and the `<canvas>` element renders whatever `ChildContent` fallback is provided (or empty). The engine
initializes only once `OnAfterRenderAsync(firstRender: true)` runs, i.e. once the component is interactive
(`InteractiveServer` / `InteractiveWebAssembly` / `InteractiveAuto`).

## AtomCanvasStudio

`AtomCanvasStudio` adds no JS of its own — it is pure C# orchestration over `AtomCanvas` (which owns the
one JS module for the pair). Undo/redo, style-default tracking, and the insert/click-to-place flow are all
implemented in `AtomCanvasStudio.razor.cs` on top of `AtomCanvas`'s public parameters/events; there is no
additional interop surface to reason about beyond what's documented above for `AtomCanvas`.
