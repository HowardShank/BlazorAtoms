using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Canvas;

/// <summary>
/// A batched, fluent C# proxy over an HTML canvas 2D context. Every call is queued locally; nothing
/// crosses the JS boundary until <see cref="FlushAsync"/> (or an awaiting call such as
/// <see cref="ToDataUrlAsync"/> / <see cref="ClearAsync"/>). Batching is deliberate: on Blazor Server
/// each interop call is a network round-trip, so a whole frame of draw ops ships as one message.
/// Obtain one from <see cref="AtomCanvas.GetContext2DAsync"/> (or the <c>OnPaint</c> callback).
/// </summary>
/// <remarks>
/// The op set is a curated subset of the browser 2D context, extensible as needed. Coordinates are in
/// CSS pixels — the engine already applies the <c>devicePixelRatio</c> transform for you.
/// </remarks>
public sealed class Canvas2DContext
{
    private readonly IJSObjectReference _module;
    private readonly ElementReference _canvas;
    private readonly List<object?[]> _ops = new();

    internal Canvas2DContext(IJSObjectReference module, ElementReference canvas)
    {
        _module = module;
        _canvas = canvas;
    }

    private Canvas2DContext Op(string name, params object?[] args)
    {
        var row = new object?[args.Length + 1];
        row[0] = name;
        Array.Copy(args, 0, row, 1, args.Length);
        _ops.Add(row);
        return this;
    }

    // ---- state setters ----
    /// <summary>Set the fill color / style.</summary>
    public Canvas2DContext FillStyle(string color) => Op("fillStyle", color);
    /// <summary>Set the stroke color / style.</summary>
    public Canvas2DContext StrokeStyle(string color) => Op("strokeStyle", color);
    /// <summary>Set the line width in px.</summary>
    public Canvas2DContext LineWidth(double w) => Op("lineWidth", w);
    /// <summary>Set the line cap (<c>butt</c> / <c>round</c> / <c>square</c>).</summary>
    public Canvas2DContext LineCap(string cap) => Op("lineCap", cap);
    /// <summary>Set the line join (<c>miter</c> / <c>round</c> / <c>bevel</c>).</summary>
    public Canvas2DContext LineJoin(string join) => Op("lineJoin", join);
    /// <summary>Set the font shorthand, e.g. <c>"16px sans-serif"</c>.</summary>
    public Canvas2DContext Font(string font) => Op("font", font);
    /// <summary>Set the global alpha 0..1.</summary>
    public Canvas2DContext GlobalAlpha(double a) => Op("globalAlpha", a);

    // ---- paths ----
    /// <summary>Start a new path.</summary>
    public Canvas2DContext BeginPath() => Op("beginPath");
    /// <summary>Close the current sub-path back to its start.</summary>
    public Canvas2DContext ClosePath() => Op("closePath");
    /// <summary>Move the pen to (x, y) without drawing.</summary>
    public Canvas2DContext MoveTo(double x, double y) => Op("moveTo", x, y);
    /// <summary>Add a line from the current point to (x, y).</summary>
    public Canvas2DContext LineTo(double x, double y) => Op("lineTo", x, y);
    /// <summary>Add an arc (radians).</summary>
    public Canvas2DContext Arc(double x, double y, double r, double startAngle, double endAngle, bool counterClockwise = false)
        => Op("arc", x, y, r, startAngle, endAngle, counterClockwise);
    /// <summary>Add a rectangle sub-path.</summary>
    public Canvas2DContext Rect(double x, double y, double w, double h) => Op("rect", x, y, w, h);
    /// <summary>Fill the current path.</summary>
    public Canvas2DContext Fill() => Op("fill");
    /// <summary>Stroke the current path.</summary>
    public Canvas2DContext Stroke() => Op("stroke");

    // ---- rects / text ----
    /// <summary>Paint a filled rectangle.</summary>
    public Canvas2DContext FillRect(double x, double y, double w, double h) => Op("fillRect", x, y, w, h);
    /// <summary>Paint a stroked (outlined) rectangle.</summary>
    public Canvas2DContext StrokeRect(double x, double y, double w, double h) => Op("strokeRect", x, y, w, h);
    /// <summary>Clear a rectangular region to transparent.</summary>
    public Canvas2DContext ClearRect(double x, double y, double w, double h) => Op("clearRect", x, y, w, h);
    /// <summary>Paint filled text with its baseline start at (x, y).</summary>
    public Canvas2DContext FillText(string text, double x, double y) => Op("fillText", text, x, y);
    /// <summary>Paint outlined text with its baseline start at (x, y).</summary>
    public Canvas2DContext StrokeText(string text, double x, double y) => Op("strokeText", text, x, y);

    // ---- transforms ----
    /// <summary>Push the current transform / clip / style state.</summary>
    public Canvas2DContext Save() => Op("save");
    /// <summary>Pop the last saved state.</summary>
    public Canvas2DContext Restore() => Op("restore");
    /// <summary>Translate the origin.</summary>
    public Canvas2DContext Translate(double x, double y) => Op("translate", x, y);
    /// <summary>Rotate by radians.</summary>
    public Canvas2DContext Rotate(double radians) => Op("rotate", radians);
    /// <summary>Scale the axes.</summary>
    public Canvas2DContext Scale(double x, double y) => Op("scale", x, y);

    /// <summary>Send every queued op to the canvas in a single interop call, then clear the queue.</summary>
    public async ValueTask FlushAsync()
    {
        if (_ops.Count == 0) return;
        var batch = _ops.ToArray();
        _ops.Clear();
        await _module.InvokeVoidAsync("runCommands", _canvas, batch);
    }

    /// <summary>Flush any queued ops, then clear the whole canvas surface to transparent.</summary>
    public async ValueTask ClearAsync()
    {
        await FlushAsync();
        await _module.InvokeVoidAsync("clearCanvas", _canvas);
    }

    /// <summary>Flush, then export the current pixels as a data URL (PNG by default).</summary>
    public async ValueTask<string> ToDataUrlAsync(string type = "image/png", double quality = 0.92)
    {
        await FlushAsync();
        return await _module.InvokeAsync<string>("toDataUrl", _canvas, type, quality);
    }
}
