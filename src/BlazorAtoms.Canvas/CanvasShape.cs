using System.Text.Json.Serialization;

namespace BlazorAtoms.Canvas;

/// <summary>A point in canvas (CSS-pixel) space. <see cref="P"/> is optional pen pressure (0..1).</summary>
public readonly record struct CanvasPoint(double X, double Y, double? P = null);

/// <summary>
/// Base type for every declarative drawing primitive an <see cref="AtomCanvas"/> renders.
/// The set is a <see cref="System.Text.Json"/> polymorphic hierarchy: a <c>kind</c> discriminator is
/// written when the shapes are serialized to the drawing engine and read back on the JS side to pick
/// the draw routine. Shapes are immutable records; edits produce new instances (e.g. <see cref="Translate"/>).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(CanvasLine), "line")]
[JsonDerivedType(typeof(CanvasRect), "rect")]
[JsonDerivedType(typeof(CanvasCircle), "circle")]
[JsonDerivedType(typeof(CanvasPath), "path")]
[JsonDerivedType(typeof(CanvasText), "text")]
[JsonDerivedType(typeof(CanvasImage), "image")]
public abstract record CanvasShape
{
    /// <summary>Stable id — used for hit-testing and drag in <see cref="CanvasMode.Select"/>.</summary>
    public string Id { get; init; } = "s" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>Stroke (outline) color. Null falls back to the canvas <c>PenColor</c>.</summary>
    public string? Stroke { get; init; }

    /// <summary>Stroke width in px. Null falls back to the canvas <c>PenWidth</c>.</summary>
    public double? StrokeWidth { get; init; }

    /// <summary>Fill color. Null means outline only (no fill).</summary>
    public string? Fill { get; init; }

    /// <summary>Overall opacity 0..1. Null is fully opaque.</summary>
    public double? Opacity { get; init; }

    /// <summary>When false, <see cref="CanvasMode.Select"/> will not pick this shape up for dragging.</summary>
    public bool Draggable { get; init; } = true;

    /// <summary>When false, the shape is kept in the model but not drawn (backs a layers show/hide toggle).</summary>
    public bool Visible { get; init; } = true;

    /// <summary>Return a copy of this shape translated by (<paramref name="dx"/>, <paramref name="dy"/>).</summary>
    public abstract CanvasShape Translate(double dx, double dy);
}

/// <summary>A straight line segment.</summary>
public record CanvasLine(double X1, double Y1, double X2, double Y2) : CanvasShape
{
    /// <inheritdoc />
    public override CanvasShape Translate(double dx, double dy) =>
        this with { X1 = X1 + dx, Y1 = Y1 + dy, X2 = X2 + dx, Y2 = Y2 + dy };
}

/// <summary>An axis-aligned rectangle with an optional corner <paramref name="Radius"/>.</summary>
public record CanvasRect(double X, double Y, double Width, double Height, double Radius = 0) : CanvasShape
{
    /// <inheritdoc />
    public override CanvasShape Translate(double dx, double dy) => this with { X = X + dx, Y = Y + dy };
}

/// <summary>A circle centered at (<paramref name="Cx"/>, <paramref name="Cy"/>) with radius <paramref name="R"/>.</summary>
public record CanvasCircle(double Cx, double Cy, double R) : CanvasShape
{
    /// <inheritdoc />
    public override CanvasShape Translate(double dx, double dy) => this with { Cx = Cx + dx, Cy = Cy + dy };
}

/// <summary>A polyline / freehand stroke through <paramref name="Points"/>. This is what a signature captures.</summary>
public record CanvasPath(IReadOnlyList<CanvasPoint> Points, bool Closed = false, bool Smooth = true) : CanvasShape
{
    /// <inheritdoc />
    public override CanvasShape Translate(double dx, double dy) =>
        this with { Points = Points.Select(p => p with { X = p.X + dx, Y = p.Y + dy }).ToList() };
}

/// <summary>A run of text with its baseline start at (<paramref name="X"/>, <paramref name="Y"/>).</summary>
public record CanvasText(double X, double Y, string Text, double FontSize = 16, string? FontFamily = null) : CanvasShape
{
    /// <inheritdoc />
    public override CanvasShape Translate(double dx, double dy) => this with { X = X + dx, Y = Y + dy };
}

/// <summary>A raster image (<paramref name="Src"/> = URL or data URI) drawn into the given box.</summary>
public record CanvasImage(double X, double Y, double Width, double Height, string Src) : CanvasShape
{
    /// <inheritdoc />
    public override CanvasShape Translate(double dx, double dy) => this with { X = X + dx, Y = Y + dy };
}
