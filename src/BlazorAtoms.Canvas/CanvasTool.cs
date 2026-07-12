namespace BlazorAtoms.Canvas;

/// <summary>The active tool in an <see cref="AtomCanvasStudio"/>. Each maps to an <see cref="CanvasMode"/>.</summary>
public enum CanvasTool
{
    /// <summary>Click to select a shape, drag to move it.</summary>
    Select,

    /// <summary>Freehand ink.</summary>
    Draw,

    /// <summary>Drag to pan the view.</summary>
    Pan,

    /// <summary>Click the canvas to drop the pending shape/stamp there (click-to-place).</summary>
    Insert,

    /// <summary>Click a shape to delete it.</summary>
    Erase,
}
