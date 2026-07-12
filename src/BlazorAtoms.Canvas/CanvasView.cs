namespace BlazorAtoms.Canvas;

/// <summary>
/// The canvas view transform — a pan offset (CSS px) plus a zoom <see cref="Scale"/>. Reported by
/// <see cref="AtomCanvas.OnViewChanged"/> when a <see cref="CanvasMode.Pan"/> drag ends, and settable back via
/// <see cref="AtomCanvas.PanX"/> / <see cref="AtomCanvas.PanY"/> / <see cref="AtomCanvas.Scale"/>.
/// </summary>
public readonly record struct CanvasView(double PanX, double PanY, double Scale);
