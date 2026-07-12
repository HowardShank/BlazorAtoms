namespace BlazorAtoms.Canvas;

/// <summary>
/// The public extension surface of an <see cref="AtomCanvasStudio"/>. An instance is passed to every studio
/// slot as <c>@context</c> and published as a <see cref="Microsoft.AspNetCore.Components.CascadingValue{T}"/>,
/// so custom toolbar items / panels (even from a 3rd-party app, without editing source) can read state and
/// drive the canvas. All members forward to the owning studio.
/// </summary>
public sealed class AtomCanvasStudioContext
{
    private readonly AtomCanvasStudio _s;
    internal AtomCanvasStudioContext(AtomCanvasStudio s) => _s = s;

    // ---- state ----
    /// <summary>Active tool.</summary>
    public CanvasTool Tool => _s.CurrentTool;
    /// <summary>Current pen (stroke) color.</summary>
    public string PenColor => _s.PenColorValue;
    /// <summary>Current pen (stroke) width in px.</summary>
    public double PenWidth => _s.PenWidthValue;
    /// <summary>Current fill color (null = none).</summary>
    public string? FillColor => _s.FillColorValue;
    /// <summary>Current background color.</summary>
    public string? Background => _s.BackgroundValue;
    /// <summary>Current zoom factor.</summary>
    public double Scale => _s.ScaleValue;
    /// <summary>Selected shape id (null = none).</summary>
    public string? SelectedId => _s.SelectedIdValue;
    /// <summary>Number of shapes in the model.</summary>
    public int ShapeCount => _s.ShapeCountValue;
    /// <summary>True when the model is empty.</summary>
    public bool IsEmpty => _s.ShapeCountValue == 0;
    /// <summary>True when there is an undo step available.</summary>
    public bool CanUndo => _s.CanUndoValue;
    /// <summary>True when there is a redo step available.</summary>
    public bool CanRedo => _s.CanRedoValue;
    /// <summary>Whether editing is disabled.</summary>
    public bool Disabled => _s.DisabledValue;
    /// <summary>The current shape model (read-only snapshot).</summary>
    public IReadOnlyList<CanvasShape> Shapes => _s.Current;
    /// <summary>The active stamp/insert palette.</summary>
    public IReadOnlyList<CanvasStamp> Stamps => _s.StampsValue;

    // ---- actions ----
    /// <summary>Set the active tool.</summary>
    public Task SetTool(CanvasTool tool) => _s.SetToolAsync(tool);
    /// <summary>Set the pen color.</summary>
    public void SetPenColor(string color) => _s.SetPenColor(color);
    /// <summary>Set the pen width (px).</summary>
    public void SetPenWidth(double width) => _s.SetPenWidth(width);
    /// <summary>Set the fill color (null = none).</summary>
    public void SetFillColor(string? color) => _s.SetFillColor(color);
    /// <summary>Set the background color.</summary>
    public void SetBackground(string? color) => _s.SetBackground(color);
    /// <summary>Append a shape (undoable).</summary>
    public Task AddShape(CanvasShape shape) => _s.AddShapeAsync(shape);
    /// <summary>Arm click-to-place with a shape factory.</summary>
    public void BeginInsert(Func<CanvasPoint, CanvasShape> factory) => _s.BeginInsert(factory);
    /// <summary>Delete the selected shape.</summary>
    public Task DeleteSelected() => _s.DeleteSelectedAsync();
    /// <summary>Clear all shapes.</summary>
    public Task Clear() => _s.ClearAsync();
    /// <summary>Undo the last change.</summary>
    public Task Undo() => _s.UndoAsync();
    /// <summary>Redo the last undone change.</summary>
    public Task Redo() => _s.RedoAsync();
    /// <summary>Select a shape (null clears).</summary>
    public void SelectShape(string? id) => _s.SelectShape(id);
    /// <summary>Toggle a shape's visibility.</summary>
    public Task ToggleVisible(string id) => _s.ToggleVisibleAsync(id);
    /// <summary>Move a shape up one z-step.</summary>
    public Task BringForward(string id) => _s.BringForwardAsync(id);
    /// <summary>Move a shape down one z-step.</summary>
    public Task SendBackward(string id) => _s.SendBackwardAsync(id);
    /// <summary>Move a shape to the top.</summary>
    public Task BringToFront(string id) => _s.BringToFrontAsync(id);
    /// <summary>Move a shape to the bottom.</summary>
    public Task SendToBack(string id) => _s.SendToBackAsync(id);
    /// <summary>Zoom in.</summary>
    public void ZoomIn() => _s.ZoomIn();
    /// <summary>Zoom out.</summary>
    public void ZoomOut() => _s.ZoomOut();
    /// <summary>Reset zoom + pan.</summary>
    public void ZoomReset() => _s.ZoomReset();
    /// <summary>Export the current viewport as a PNG data URL.</summary>
    public ValueTask<string> ExportPngAsync() => _s.ExportPngAsync();
    /// <summary>Export the model as an SVG string.</summary>
    public string ExportSvg() => _s.ExportSvg();
    /// <summary>Serialize the model to JSON.</summary>
    public string SaveJson() => _s.SaveJson();
    /// <summary>Replace the model from JSON (undoable). Returns false if invalid.</summary>
    public Task<bool> LoadJson(string json) => _s.LoadJsonAsync(json);
}
