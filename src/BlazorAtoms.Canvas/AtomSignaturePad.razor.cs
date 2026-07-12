using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Canvas;

/// <summary>
/// A signature-capture pad — a friendly preset over <see cref="AtomCanvas"/> in freehand
/// (<see cref="CanvasMode.Draw"/>) mode. Bind <see cref="Value"/> to get a PNG data URL updated after each
/// stroke (ready to POST/store), and/or <see cref="Strokes"/> for the editable vector model. Clear, undo,
/// and export are methods on the component instance.
/// </summary>
public partial class AtomSignaturePad : AtomComponentBase
{
    private AtomCanvas? _canvas;
    private IReadOnlyList<CanvasShape> _internal = Array.Empty<CanvasShape>();
    private bool _needExport;

    /// <summary>The captured signature as a PNG data URL. Two-way bindable; refreshed after each stroke.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Raised with the new PNG data URL whenever the signature changes.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>The vector stroke model. Two-way bindable escape hatch for replay / editing.</summary>
    [Parameter] public IReadOnlyList<CanvasShape>? Strokes { get; set; }

    /// <summary>Raised (with a new list) whenever the stroke model changes.</summary>
    [Parameter] public EventCallback<IReadOnlyList<CanvasShape>> StrokesChanged { get; set; }

    /// <summary>Ink color. Default <c>#111827</c>.</summary>
    [Parameter] public string PenColor { get; set; } = "#111827";

    /// <summary>Ink width in px. Default <c>2.5</c>.</summary>
    [Parameter] public double PenWidth { get; set; } = 2.5;

    /// <summary>Pad background color. Default white so the exported PNG has an opaque background.</summary>
    [Parameter] public string BackgroundColor { get; set; } = "#ffffff";

    /// <summary>Pad width in px. Default 400.</summary>
    [Parameter] public double Width { get; set; } = 400;

    /// <summary>Pad height in px. Default 160.</summary>
    [Parameter] public double Height { get; set; } = 160;

    /// <summary>When true, the pad ignores input.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Accessible label. Default "Signature".</summary>
    [Parameter] public string AriaLabel { get; set; } = "Signature";

    /// <summary>Raised when the user starts a stroke.</summary>
    [Parameter] public EventCallback OnStart { get; set; }

    /// <summary>Raised after a stroke is committed and the signature has been re-exported.</summary>
    [Parameter] public EventCallback OnEnd { get; set; }

    /// <summary>Raised whenever the signature changes (draw, undo, or clear).</summary>
    [Parameter] public EventCallback OnChange { get; set; }

    private IReadOnlyList<CanvasShape> CurrentStrokes => Strokes ?? _internal;

    /// <summary>True when nothing has been drawn.</summary>
    public bool IsEmpty => CurrentStrokes.Count == 0;

    private async Task OnStrokesChanged(IReadOnlyList<CanvasShape> next)
    {
        _internal = next;
        if (StrokesChanged.HasDelegate) await StrokesChanged.InvokeAsync(next);
        _needExport = true;               // export after the canvas has redrawn (our OnAfterRenderAsync)
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_needExport) return;
        _needExport = false;
        await RefreshValueAsync();
        await OnChange.InvokeAsync();
        await OnEnd.InvokeAsync();
    }

    private async Task RefreshValueAsync()
    {
        if (_canvas is null) return;
        var png = IsEmpty ? "" : await _canvas.ToDataUrlAsync();
        if (png == Value) return;
        Value = png;
        if (ValueChanged.HasDelegate) await ValueChanged.InvokeAsync(png);
    }

    /// <summary>Erase the signature.</summary>
    public async Task Clear()
    {
        if (IsEmpty) return;
        _internal = Array.Empty<CanvasShape>();
        if (StrokesChanged.HasDelegate) await StrokesChanged.InvokeAsync(_internal);
        Value = "";
        if (ValueChanged.HasDelegate) await ValueChanged.InvokeAsync("");
        StateHasChanged();
        await OnChange.InvokeAsync();
    }

    /// <summary>Remove the most recent stroke.</summary>
    public async Task UndoAsync()
    {
        var src = CurrentStrokes;
        if (src.Count == 0) return;
        var next = new List<CanvasShape>(src);
        next.RemoveAt(next.Count - 1);
        _internal = next;
        if (StrokesChanged.HasDelegate) await StrokesChanged.InvokeAsync(next);
        _needExport = true;
        StateHasChanged();
    }

    /// <summary>Export the current signature as a PNG data URL.</summary>
    public ValueTask<string> ToPngDataUrlAsync() =>
        _canvas is null ? ValueTask.FromResult("") : _canvas.ToDataUrlAsync();

    /// <summary>Export the current signature as an SVG string (vector).</summary>
    public string ToSvg() => _canvas?.ToSvg() ?? "";
}
