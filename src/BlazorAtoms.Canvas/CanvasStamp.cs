namespace BlazorAtoms.Canvas;

/// <summary>
/// A palette entry for the <see cref="AtomCanvasStudio"/> insert menu. Picking one arms click-to-place:
/// the next canvas click calls <see cref="Create"/> with the world point to produce the shape to insert.
/// Consumers supply their own <see cref="AtomCanvasStudio.Stamps"/> list to extend/replace the palette.
/// </summary>
/// <param name="Key">Stable id (used as the @key and the pick handle).</param>
/// <param name="Label">Accessible label / tooltip.</param>
/// <param name="Create">Factory: given the click point (world coords), return the shape to insert.</param>
/// <param name="Glyph">Short text/emoji shown on the palette button (e.g. "★"). Optional.</param>
public sealed record CanvasStamp(
    string Key,
    string Label,
    Func<CanvasPoint, CanvasShape> Create,
    string? Glyph = null);
