using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Canvas;

/// <summary>
/// A native HTML <c>&lt;canvas&gt;</c> with a clean C# surface. Drawing is driven declaratively by a
/// serializable <see cref="Shapes"/> model (line / rect / circle / freehand path / text / image), and the
/// <see cref="Mode"/> switches between rendering, freehand ink capture, and drag-to-move. An imperative
/// escape hatch (<see cref="GetContext2DAsync"/> / <see cref="OnPaint"/>) exposes the raw 2D context,
/// batched so it stays usable on Blazor Server.
/// </summary>
/// <remarks>
/// The component ships and lazily self-imports its own <c>atom-canvas.js</c> module (no <c>&lt;script&gt;</c>,
/// no DI). All high-frequency pointer work stays in JS; the C# model is mutated only once per gesture (on
/// pointer-up), which is what makes freehand drawing smooth even over a Server circuit. During static
/// SSR / prerender the canvas renders empty and starts drawing once the component is interactive.
/// </remarks>
public partial class AtomCanvas : AtomComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorAtoms.Canvas/atom-canvas.js";
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _ref;
    private IJSObjectReference? _module;
    private DotNetObjectReference<AtomCanvas>? _dotNet;
    private IReadOnlyList<CanvasShape> _internal = Array.Empty<CanvasShape>();
    private bool _dirty;

    /// <summary>Logical (CSS-pixel) width. The backing store is scaled by <c>devicePixelRatio</c> for crisp lines.</summary>
    [Parameter] public double Width { get; set; } = 300;

    /// <summary>Logical (CSS-pixel) height.</summary>
    [Parameter] public double Height { get; set; } = 150;

    /// <summary>Canvas background color. Null / empty is transparent. Sets <c>--canvas-bg</c>.</summary>
    [Parameter] public string? BackgroundColor { get; set; }

    /// <summary>What pointer input does — render only, freehand draw, or drag-select. Default <see cref="CanvasMode.Static"/>.</summary>
    [Parameter] public CanvasMode Mode { get; set; } = CanvasMode.Static;

    /// <summary>The shape model to render. Two-way bindable — <see cref="CanvasMode.Draw"/> and
    /// <see cref="CanvasMode.Select"/> append / move shapes and raise <see cref="ShapesChanged"/>.</summary>
    [Parameter] public IReadOnlyList<CanvasShape>? Shapes { get; set; }

    /// <summary>Raised (with a new list instance) whenever the model changes from a draw or drag gesture.</summary>
    [Parameter] public EventCallback<IReadOnlyList<CanvasShape>> ShapesChanged { get; set; }

    /// <summary>Default stroke color for freehand ink and for shapes that leave <see cref="CanvasShape.Stroke"/> null.</summary>
    [Parameter] public string PenColor { get; set; } = "#111827";

    /// <summary>Default stroke width (px) for freehand ink and for shapes that leave <see cref="CanvasShape.StrokeWidth"/> null.</summary>
    [Parameter] public double PenWidth { get; set; } = 2;

    /// <summary>Smooth freehand strokes with quadratic curves (default true).</summary>
    [Parameter] public bool PenSmoothing { get; set; } = true;

    /// <summary>When true, pointer input is ignored (rendering continues).</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Accessible label. A canvas is opaque to assistive tech, so this is the only description AT gets.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Pre-interactive / fallback content rendered inside the canvas element.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised when a freehand gesture begins (pointer-down in <see cref="CanvasMode.Draw"/>).</summary>
    [Parameter] public EventCallback OnDrawStart { get; set; }

    /// <summary>Raised with the committed stroke when a freehand gesture ends (pointer-up in <see cref="CanvasMode.Draw"/>).</summary>
    [Parameter] public EventCallback<CanvasPath> OnDrawEnd { get; set; }

    /// <summary>Raised with a shape id when it is tapped in <see cref="CanvasMode.Static"/>.</summary>
    [Parameter] public EventCallback<string> OnShapeClick { get; set; }

    /// <summary>Fired after each model redraw with the raw 2D context, for custom overlay drawing that
    /// survives redraws (queue ops on the context; the component flushes it for you).</summary>
    [Parameter] public EventCallback<Canvas2DContext> OnPaint { get; set; }

    /// <summary>The shapes actually rendered — the bound <see cref="Shapes"/> when provided, else the internal model.</summary>
    private IReadOnlyList<CanvasShape> CurrentShapes => Shapes ?? _internal;

    private string ModeValue => Mode switch
    {
        CanvasMode.Draw => "draw",
        CanvasMode.Select => "select",
        _ => "static",
    };

    private string RootStyle => new StyleVars("canvas")
        .Add("w", Width)
        .Add("h", Height)
        .Add("bg", BackgroundColor)
        .ToString();

    private object BuildOptions() => new
    {
        mode = ModeValue,
        width = Width,
        height = Height,
        penColor = PenColor,
        penWidth = PenWidth,
        smoothing = PenSmoothing,
        background = BackgroundColor,
        disabled = Disabled,
    };

    /// <inheritdoc />
    protected override void OnParametersSet() => _dirty = true;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _dotNet = DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("init", _ref, _dotNet, BuildOptions());
            _dirty = false;
            await SyncAsync();
        }
        else if (_dirty)
        {
            _dirty = false;
            await SyncAsync();
        }
    }

    // Push the current model + options to the engine (authoritative clear + redraw), then let a caller
    // paint an overlay on top.
    private async Task SyncAsync()
    {
        if (_module is null) return;
        var json = JsonSerializer.Serialize(CurrentShapes, JsonOpts);
        await _module.InvokeVoidAsync("render", _ref, json, BuildOptions());

        if (OnPaint.HasDelegate)
        {
            var ctx = new Canvas2DContext(_module, _ref);
            await OnPaint.InvokeAsync(ctx);
            await ctx.FlushAsync();
        }
    }

    private async Task UpdateShapesAsync(IReadOnlyList<CanvasShape> next)
    {
        _internal = next;
        if (ShapesChanged.HasDelegate) await ShapesChanged.InvokeAsync(next);
        _dirty = true;
        StateHasChanged();
    }

    // --- JS -> C# callbacks. Fired at most once per gesture (on pointer-up). ---

    /// <summary>Invoked by the engine when a freehand gesture begins.</summary>
    [JSInvokable]
    public async Task NotifyDrawStart() => await OnDrawStart.InvokeAsync();

    /// <summary>Invoked by the engine when a freehand stroke is committed; appends it to the model.</summary>
    [JSInvokable]
    public async Task OnStrokeCommitted(CanvasPoint[] points)
    {
        if (points is null || points.Length == 0) return;
        var stroke = new CanvasPath(points)
        {
            Stroke = PenColor,
            StrokeWidth = PenWidth,
            Smooth = PenSmoothing,
        };
        var next = new List<CanvasShape>(CurrentShapes) { stroke };
        await UpdateShapesAsync(next);
        await OnDrawEnd.InvokeAsync(stroke);
    }

    /// <summary>Invoked by the engine when a shape is dragged; translates it in the model.</summary>
    [JSInvokable]
    public async Task OnShapeMoved(string id, double dx, double dy)
    {
        var src = CurrentShapes;
        if (src.Count == 0) return;
        var next = new List<CanvasShape>(src.Count);
        foreach (var s in src) next.Add(s.Id == id ? s.Translate(dx, dy) : s);
        await UpdateShapesAsync(next);
    }

    /// <summary>Invoked by the engine when a shape is tapped in <see cref="CanvasMode.Static"/>.</summary>
    [JSInvokable]
    public async Task OnShapeClicked(string id) => await OnShapeClick.InvokeAsync(id);

    // --- Imperative escape hatch + export ---

    /// <summary>Get a batched imperative 2D context for this canvas. Queue draw ops on it then
    /// <c>await ctx.FlushAsync()</c> (one interop round-trip). Best used on a canvas you are not also
    /// driving via <see cref="Shapes"/>, or from inside <see cref="OnPaint"/>.</summary>
    public ValueTask<Canvas2DContext> GetContext2DAsync()
        => ValueTask.FromResult(new Canvas2DContext(
            _module ?? throw new InvalidOperationException("Canvas is not interactive yet — call after first render."),
            _ref));

    /// <summary>Export the current pixels as a data URL (PNG by default). Returns empty before interactive.</summary>
    public async ValueTask<string> ToDataUrlAsync(string type = "image/png", double quality = 0.92)
    {
        if (_module is null) return "";
        return await _module.InvokeAsync<string>("toDataUrl", _ref, type, quality);
    }

    /// <summary>Build an SVG string from the current shape model (pure C#, no JS) — a vector export.</summary>
    public string ToSvg() => CanvasSvg.ToSvg(CurrentShapes, Width, Height, BackgroundColor, PenColor, PenWidth);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("dispose", _ref);
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        finally
        {
            _dotNet?.Dispose();
            _module = null;
        }
        GC.SuppressFinalize(this);
    }
}
