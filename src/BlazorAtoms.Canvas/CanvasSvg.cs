using System.Globalization;
using System.Text;

namespace BlazorAtoms.Canvas;

/// <summary>
/// Renders a <see cref="CanvasShape"/> model to an <c>&lt;svg&gt;</c> string — a pure-C# vector export that
/// needs no canvas/JS. Used by <see cref="AtomCanvas.ToSvg"/>. Best-effort visual parity with the canvas
/// engine (freehand paths become smoothed polylines).
/// </summary>
public static class CanvasSvg
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static string N(double v) => v.ToString(Inv);

    /// <summary>Build an SVG document for <paramref name="shapes"/> at the given size.</summary>
    public static string ToSvg(
        IReadOnlyList<CanvasShape> shapes, double width, double height,
        string? background, string penColor, double penWidth)
    {
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(width)}\" height=\"{N(height)}\" viewBox=\"0 0 {N(width)} {N(height)}\">");
        if (!string.IsNullOrEmpty(background))
            sb.Append($"<rect x=\"0\" y=\"0\" width=\"{N(width)}\" height=\"{N(height)}\" fill=\"{Esc(background)}\"/>");

        foreach (var s in shapes)
        {
            var stroke = s.Stroke ?? penColor;
            var sw = N(s.StrokeWidth ?? penWidth);
            var fill = s.Fill is null ? "none" : Esc(s.Fill);
            var op = s.Opacity is null ? "" : $" opacity=\"{N(s.Opacity.Value)}\"";

            switch (s)
            {
                case CanvasLine l:
                    sb.Append($"<line x1=\"{N(l.X1)}\" y1=\"{N(l.Y1)}\" x2=\"{N(l.X2)}\" y2=\"{N(l.Y2)}\" stroke=\"{Esc(stroke)}\" stroke-width=\"{sw}\"{op}/>");
                    break;
                case CanvasRect r:
                    sb.Append($"<rect x=\"{N(r.X)}\" y=\"{N(r.Y)}\" width=\"{N(r.Width)}\" height=\"{N(r.Height)}\" rx=\"{N(r.Radius)}\" fill=\"{fill}\" stroke=\"{Esc(stroke)}\" stroke-width=\"{sw}\"{op}/>");
                    break;
                case CanvasCircle c:
                    sb.Append($"<circle cx=\"{N(c.Cx)}\" cy=\"{N(c.Cy)}\" r=\"{N(c.R)}\" fill=\"{fill}\" stroke=\"{Esc(stroke)}\" stroke-width=\"{sw}\"{op}/>");
                    break;
                case CanvasPath p when p.Points.Count > 0:
                    sb.Append($"<path d=\"{PathData(p)}\" fill=\"{(p.Closed ? fill : "none")}\" stroke=\"{Esc(stroke)}\" stroke-width=\"{sw}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"{op}/>");
                    break;
                case CanvasText t:
                    sb.Append($"<text x=\"{N(t.X)}\" y=\"{N(t.Y)}\" font-size=\"{N(t.FontSize)}\" font-family=\"{Esc(t.FontFamily ?? "sans-serif")}\" fill=\"{(s.Fill is null ? Esc(stroke) : fill)}\"{op}>{Esc(t.Text)}</text>");
                    break;
                case CanvasImage img:
                    sb.Append($"<image x=\"{N(img.X)}\" y=\"{N(img.Y)}\" width=\"{N(img.Width)}\" height=\"{N(img.Height)}\" href=\"{Esc(img.Src)}\"{op}/>");
                    break;
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string PathData(CanvasPath p)
    {
        var pts = p.Points;
        var sb = new StringBuilder();
        sb.Append($"M {N(pts[0].X)} {N(pts[0].Y)}");
        if (!p.Smooth || pts.Count < 3)
        {
            for (var i = 1; i < pts.Count; i++) sb.Append($" L {N(pts[i].X)} {N(pts[i].Y)}");
        }
        else
        {
            // Quadratic smoothing through midpoints (matches the engine's freehand look).
            for (var i = 1; i < pts.Count - 1; i++)
            {
                var mx = (pts[i].X + pts[i + 1].X) / 2;
                var my = (pts[i].Y + pts[i + 1].Y) / 2;
                sb.Append($" Q {N(pts[i].X)} {N(pts[i].Y)} {N(mx)} {N(my)}");
            }
            var last = pts[^1];
            sb.Append($" L {N(last.X)} {N(last.Y)}");
        }
        if (p.Closed) sb.Append(" Z");
        return sb.ToString();
    }

    private static string Esc(string s) => s
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
