using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Canvas;

/// <summary>Where the default toolbar sits relative to the canvas.</summary>
public enum ToolbarPlacement { Top, Bottom, Start, End }

/// <summary>
/// A batteries-included, extensible canvas workbench built on <see cref="AtomCanvas"/>: a default toolbar
/// (tools, shape/stamp insert, style, undo/redo, zoom, export), an optional layers panel, and save/load —
/// all driving a two-way-bound <see cref="Shapes"/> model. Works with zero config, and is fully extensible
/// without editing source: replace/inject any region via the <see cref="RenderFragment"/> slots, drive it
/// from the cascading <see cref="AtomCanvasStudioContext"/>, and extend the insert menu via <see cref="Stamps"/>.
/// </summary>
/// <remarks>Adds no JS of its own — it orchestrates <see cref="AtomCanvas"/> (which owns the one JS module).
/// The public action methods here are also the studio's imperative API (call them via <c>@ref</c>).</remarks>
public partial class AtomCanvasStudio : AtomComponentBase
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private AtomCanvas? _canvas;
    private AtomCanvasStudioContext? _context;
    private IReadOnlyList<CanvasShape> _internal = Array.Empty<CanvasShape>();
    private readonly List<IReadOnlyList<CanvasShape>> _undo = new();
    private readonly List<IReadOnlyList<CanvasShape>> _redo = new();

    private CanvasTool _tool;
    private CanvasTool? _lastToolParam;
    private string _penColor = "#111827";
    private double _penWidth = 2;
    private string? _fillColor;
    private string? _background = "#ffffff";
    private double _scale = 1, _panX, _panY;
    private string? _selectedId;
    private Func<CanvasPoint, CanvasShape>? _pendingInsert;

    // ---- parameters ----

    /// <summary>The shape model. Two-way bindable — the studio edits it and raises <see cref="ShapesChanged"/>.</summary>
    [Parameter] public IReadOnlyList<CanvasShape>? Shapes { get; set; }
    /// <summary>Raised (new list) whenever the model changes.</summary>
    [Parameter] public EventCallback<IReadOnlyList<CanvasShape>> ShapesChanged { get; set; }

    /// <summary>Canvas width in CSS px.</summary>
    [Parameter] public double Width { get; set; } = 640;
    /// <summary>Canvas height in CSS px.</summary>
    [Parameter] public double Height { get; set; } = 420;

    /// <summary>Initial active tool. Two-way bindable.</summary>
    [Parameter] public CanvasTool Tool { get; set; } = CanvasTool.Select;
    /// <summary>Raised when the active tool changes.</summary>
    [Parameter] public EventCallback<CanvasTool> ToolChanged { get; set; }

    /// <summary>Initial pen color.</summary>
    [Parameter] public string PenColor { get; set; } = "#111827";
    /// <summary>Initial pen width (px).</summary>
    [Parameter] public double PenWidth { get; set; } = 2;
    /// <summary>Initial fill color for inserted shapes (null = no fill).</summary>
    [Parameter] public string? FillColor { get; set; }
    /// <summary>Initial canvas background color.</summary>
    [Parameter] public string? Background { get; set; } = "#ffffff";

    /// <summary>The insert/stamp palette. Null uses <see cref="CanvasStamps.Default"/>.</summary>
    [Parameter] public IReadOnlyList<CanvasStamp>? Stamps { get; set; }

    /// <summary>Show the default toolbar (when no <see cref="Toolbar"/> slot is supplied).</summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;
    /// <summary>Show the default layers panel (when no <see cref="EndPanel"/> slot is supplied).</summary>
    [Parameter] public bool ShowLayers { get; set; } = true;
    /// <summary>Show the default status bar (when no <see cref="StatusBar"/> slot is supplied).</summary>
    [Parameter] public bool ShowStatusBar { get; set; } = true;
    /// <summary>Where the toolbar sits. Default <see cref="ToolbarPlacement.Top"/>.</summary>
    [Parameter] public ToolbarPlacement ToolbarPlacement { get; set; } = ToolbarPlacement.Top;

    /// <summary>Max undo depth. Default 100.</summary>
    [Parameter] public int MaxHistory { get; set; } = 100;
    /// <summary>Disable all editing.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Accessible label for the inner canvas.</summary>
    [Parameter] public string CanvasAriaLabel { get; set; } = "Drawing canvas";

    /// <summary>Raised on any edit (draw, insert, move, delete, undo/redo, load...).</summary>
    [Parameter] public EventCallback OnChange { get; set; }

    // ---- slots (each receives the studio context as @context) ----
    /// <summary>Replaces the entire default toolbar.</summary>
    [Parameter] public RenderFragment<AtomCanvasStudioContext>? Toolbar { get; set; }
    /// <summary>Injected at the start of the default toolbar.</summary>
    [Parameter] public RenderFragment<AtomCanvasStudioContext>? ToolbarStart { get; set; }
    /// <summary>Injected at the end of the default toolbar.</summary>
    [Parameter] public RenderFragment<AtomCanvasStudioContext>? ToolbarEnd { get; set; }
    /// <summary>Leading side panel.</summary>
    [Parameter] public RenderFragment<AtomCanvasStudioContext>? StartPanel { get; set; }
    /// <summary>Trailing side panel. Replaces the default layers panel.</summary>
    [Parameter] public RenderFragment<AtomCanvasStudioContext>? EndPanel { get; set; }
    /// <summary>Status bar content. Replaces the default status bar.</summary>
    [Parameter] public RenderFragment<AtomCanvasStudioContext>? StatusBar { get; set; }
    /// <summary>Absolutely-positioned overlay drawn over the canvas viewport.</summary>
    [Parameter] public RenderFragment<AtomCanvasStudioContext>? CanvasOverlay { get; set; }

    // ---- lifecycle ----

    protected override void OnInitialized()
    {
        _tool = Tool;
        _lastToolParam = Tool;
        _penColor = PenColor;
        _penWidth = PenWidth;
        _fillColor = FillColor;
        _background = Background;
        _context = new AtomCanvasStudioContext(this);
    }

    protected override void OnParametersSet()
    {
        if (_lastToolParam != Tool) { _tool = Tool; _lastToolParam = Tool; }
    }

    // ---- read surface (used by the context + default chrome) ----

    internal IReadOnlyList<CanvasShape> Current => Shapes ?? _internal;
    internal CanvasTool CurrentTool => _tool;
    internal string PenColorValue => _penColor;
    internal double PenWidthValue => _penWidth;
    internal string? FillColorValue => _fillColor;
    internal string? BackgroundValue => _background;
    internal double ScaleValue => _scale;
    internal string? SelectedIdValue => _selectedId;
    internal int ShapeCountValue => Current.Count;
    internal bool CanUndoValue => _undo.Count > 0;
    internal bool CanRedoValue => _redo.Count > 0;
    internal IReadOnlyList<CanvasStamp> StampsValue => Stamps ?? CanvasStamps.Default;
    internal bool DisabledValue => Disabled;

    private CanvasMode CanvasModeForTool => _tool switch
    {
        CanvasTool.Draw => CanvasMode.Draw,
        CanvasTool.Select => CanvasMode.Select,
        CanvasTool.Pan => CanvasMode.Pan,
        _ => CanvasMode.Static, // Insert + Erase read clicks in Static mode
    };

    // ---- history + commit ----

    private void PushHistory()
    {
        _undo.Add(Current);
        if (_undo.Count > MaxHistory) _undo.RemoveAt(0);
        _redo.Clear();
    }

    private async Task SetShapesAsync(IReadOnlyList<CanvasShape> next)
    {
        _internal = next;
        if (ShapesChanged.HasDelegate) await ShapesChanged.InvokeAsync(next);
        await OnChange.InvokeAsync();
        StateHasChanged();
    }

    // ---- public / context actions ----

    /// <summary>Set the active tool.</summary>
    public async Task SetToolAsync(CanvasTool tool)
    {
        _tool = tool;
        _lastToolParam = tool;
        if (tool != CanvasTool.Insert) _pendingInsert = null;
        if (ToolChanged.HasDelegate) await ToolChanged.InvokeAsync(tool);
        StateHasChanged();
    }

    /// <summary>Set the pen (stroke) color.</summary>
    public void SetPenColor(string color) { _penColor = color; StateHasChanged(); }
    /// <summary>Set the pen (stroke) width in px.</summary>
    public void SetPenWidth(double width) { _penWidth = width; StateHasChanged(); }
    /// <summary>Set the fill color used for inserted shapes (null = none).</summary>
    public void SetFillColor(string? color) { _fillColor = color; StateHasChanged(); }
    /// <summary>Set the canvas background color.</summary>
    public void SetBackground(string? color) { _background = color; StateHasChanged(); }

    /// <summary>Append a shape (records an undo step).</summary>
    public async Task AddShapeAsync(CanvasShape shape)
    {
        PushHistory();
        await SetShapesAsync(new List<CanvasShape>(Current) { shape });
    }

    /// <summary>Arm click-to-place: the next canvas click inserts <c>factory(point)</c>.</summary>
    public void BeginInsert(Func<CanvasPoint, CanvasShape> factory)
    {
        _pendingInsert = factory;
        _ = SetToolAsync(CanvasTool.Insert);
    }

    /// <summary>Delete the selected shape.</summary>
    public async Task DeleteSelectedAsync()
    {
        var id = _selectedId;
        if (id is null) return;
        PushHistory();
        _selectedId = null;
        await SetShapesAsync(Current.Where(s => s.Id != id).ToList());
    }

    /// <summary>Clear all shapes.</summary>
    public async Task ClearAsync()
    {
        if (Current.Count == 0) return;
        PushHistory();
        _selectedId = null;
        await SetShapesAsync(Array.Empty<CanvasShape>());
    }

    /// <summary>Undo the last change.</summary>
    public async Task UndoAsync()
    {
        if (_undo.Count == 0) return;
        _redo.Add(Current);
        var prev = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        await SetShapesAsync(prev);
    }

    /// <summary>Redo the last undone change.</summary>
    public async Task RedoAsync()
    {
        if (_redo.Count == 0) return;
        _undo.Add(Current);
        var next = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        await SetShapesAsync(next);
    }

    /// <summary>Select a shape by id (null clears).</summary>
    public void SelectShape(string? id) { _selectedId = id; StateHasChanged(); }

    /// <summary>Toggle a shape's visibility.</summary>
    public async Task ToggleVisibleAsync(string id)
    {
        PushHistory();
        await SetShapesAsync(Current.Select(s => s.Id == id ? s with { Visible = !s.Visible } : s).ToList());
    }

    /// <summary>Move a shape one step up the z-order (later in the list = drawn on top).</summary>
    public Task BringForwardAsync(string id) => MoveAsync(id, +1);
    /// <summary>Move a shape one step down the z-order.</summary>
    public Task SendBackwardAsync(string id) => MoveAsync(id, -1);
    /// <summary>Move a shape to the top of the z-order.</summary>
    public Task BringToFrontAsync(string id) => MoveToEndAsync(id, true);
    /// <summary>Move a shape to the bottom of the z-order.</summary>
    public Task SendToBackAsync(string id) => MoveToEndAsync(id, false);

    private async Task MoveAsync(string id, int delta)
    {
        var list = new List<CanvasShape>(Current);
        var i = list.FindIndex(s => s.Id == id);
        var j = i + delta;
        if (i < 0 || j < 0 || j >= list.Count) return;
        PushHistory();
        (list[i], list[j]) = (list[j], list[i]);
        await SetShapesAsync(list);
    }

    private async Task MoveToEndAsync(string id, bool front)
    {
        var list = new List<CanvasShape>(Current);
        var i = list.FindIndex(s => s.Id == id);
        if (i < 0) return;
        PushHistory();
        var s = list[i];
        list.RemoveAt(i);
        if (front) list.Add(s); else list.Insert(0, s);
        await SetShapesAsync(list);
    }

    /// <summary>Zoom in about the canvas center.</summary>
    public void ZoomIn() => SetScale(_scale * 1.25);
    /// <summary>Zoom out about the canvas center.</summary>
    public void ZoomOut() => SetScale(_scale / 1.25);
    /// <summary>Reset zoom to 100% and pan to origin.</summary>
    public void ZoomReset() { _scale = 1; _panX = 0; _panY = 0; StateHasChanged(); }

    private void SetScale(double newScale)
    {
        newScale = Math.Clamp(newScale, 0.1, 8);
        var cx = Width / 2; var cy = Height / 2;
        var worldX = (cx - _panX) / _scale;
        var worldY = (cy - _panY) / _scale;
        _scale = newScale;
        _panX = cx - worldX * _scale;
        _panY = cy - worldY * _scale;
        StateHasChanged();
    }

    /// <summary>Export the current viewport as a PNG data URL.</summary>
    public ValueTask<string> ExportPngAsync() =>
        _canvas is null ? ValueTask.FromResult("") : _canvas.ToDataUrlAsync();

    /// <summary>Export the model as an SVG string (pure vector, transform-independent).</summary>
    public string ExportSvg() => _canvas?.ToSvg() ?? "";

    /// <summary>Serialize the model to JSON.</summary>
    public string SaveJson() => JsonSerializer.Serialize(Current, JsonOpts);

    /// <summary>Replace the model from JSON (records an undo step). Returns false if the JSON is invalid.</summary>
    public async Task<bool> LoadJsonAsync(string json)
    {
        List<CanvasShape>? loaded;
        try { loaded = JsonSerializer.Deserialize<List<CanvasShape>>(json, JsonOpts); }
        catch (JsonException) { return false; }
        if (loaded is null) return false;
        PushHistory();
        _selectedId = null;
        await SetShapesAsync(loaded);
        return true;
    }

    // ---- canvas event handlers (bound in the .razor) ----

    private async Task OnCanvasShapesChanged(IReadOnlyList<CanvasShape> next)
    {
        // draw / drag committed inside AtomCanvas → treat as one undoable change
        PushHistory();
        await SetShapesAsync(next);
    }

    private void OnCanvasShapeSelected(string? id) { _selectedId = id; StateHasChanged(); }

    private async Task OnCanvasClickedEmpty(CanvasPoint p)
    {
        if (_tool == CanvasTool.Insert && _pendingInsert is not null)
            await AddShapeAsync(_pendingInsert(p));
    }

    private async Task OnCanvasShapeClicked(string id)
    {
        if (_tool != CanvasTool.Erase) return;
        PushHistory();
        if (_selectedId == id) _selectedId = null;
        await SetShapesAsync(Current.Where(s => s.Id != id).ToList());
    }

    private void OnCanvasViewChanged(CanvasView v) { _panX = v.PanX; _panY = v.PanY; _scale = v.Scale; StateHasChanged(); }

    private async Task OnLoadFileAsync(InputFileChangeEventArgs e)
    {
        using var reader = new StreamReader(e.File.OpenReadStream(2_000_000));
        var json = await reader.ReadToEndAsync();
        await LoadJsonAsync(json);
    }

    // ---- helpers for the default chrome ----

    private string PlacementValue => ToolbarPlacement switch
    {
        ToolbarPlacement.Bottom => "bottom",
        ToolbarPlacement.Start => "start",
        ToolbarPlacement.End => "end",
        _ => "top",
    };

    private string ExportJsonDataUri => "data:application/json;charset=utf-8," + Uri.EscapeDataString(SaveJson());
    private string ExportSvgDataUri => "data:image/svg+xml;charset=utf-8," + Uri.EscapeDataString(ExportSvg());

    private string? _png;
    private async Task DoExportPngAsync() { _png = await ExportPngAsync(); StateHasChanged(); }

    private static string Fmt(double v) => v.ToString(Inv);

    // default-toolbar input handlers
    private void OnPenColorInput(ChangeEventArgs e) => SetPenColor(e.Value?.ToString() ?? _penColor);
    private void OnPenWidthInput(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), NumberStyles.Any, Inv, out var v)) SetPenWidth(v);
    }
    private void OnBackgroundInput(ChangeEventArgs e) => SetBackground(e.Value?.ToString());
    private void OnFillToggle(ChangeEventArgs e)
    {
        var on = e.Value is bool b && b;
        SetFillColor(on ? (_fillColor ?? "#93c5fd") : null);
    }
    private void OnFillColorInput(ChangeEventArgs e) => SetFillColor(e.Value?.ToString());

    internal static string ShapeLabel(CanvasShape s) => s switch
    {
        CanvasLine => "Line",
        CanvasRect => "Rectangle",
        CanvasCircle => "Circle",
        CanvasText t => "Text: " + (t.Text.Length > 12 ? t.Text[..12] + "…" : t.Text),
        CanvasImage => "Image",
        CanvasPath p => $"Path ({p.Points.Count})",
        _ => "Shape",
    };

    // Built-in insert factories (fill/stroke from current style).
    private CanvasShape MakeRect(CanvasPoint p) => new CanvasRect(p.X - 40, p.Y - 28, 80, 56, 6) { Stroke = _penColor, StrokeWidth = _penWidth, Fill = _fillColor };
    private CanvasShape MakeCircle(CanvasPoint p) => new CanvasCircle(p.X, p.Y, 34) { Stroke = _penColor, StrokeWidth = _penWidth, Fill = _fillColor };
    private CanvasShape MakeLine(CanvasPoint p) => new CanvasLine(p.X - 45, p.Y, p.X + 45, p.Y) { Stroke = _penColor, StrokeWidth = _penWidth };
    private CanvasShape MakeText(CanvasPoint p) => new CanvasText(p.X - 20, p.Y, "Text", 22) { Fill = _penColor };
}
