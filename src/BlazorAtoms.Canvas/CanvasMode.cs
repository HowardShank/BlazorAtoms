namespace BlazorAtoms.Canvas;

/// <summary>How an <see cref="AtomCanvas"/> reacts to pointer input.</summary>
public enum CanvasMode
{
    /// <summary>Render the shape model only. Pointer taps raise <c>OnShapeClick</c>; nothing is edited.</summary>
    Static,

    /// <summary>Freehand ink — a pointer gesture draws a new <see cref="CanvasPath"/> (this is signature capture).</summary>
    Draw,

    /// <summary>Pick the top-most shape under the pointer and drag it; the model is updated on release.</summary>
    Select,
}
