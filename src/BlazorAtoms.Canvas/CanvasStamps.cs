namespace BlazorAtoms.Canvas;

/// <summary>Built-in stamp palette for <see cref="AtomCanvasStudio"/>. Consumers can use, extend, or replace it.</summary>
public static class CanvasStamps
{
    /// <summary>A geometric 5-point star <see cref="CanvasPath"/> centered at <paramref name="c"/>.</summary>
    public static CanvasPath Star(CanvasPoint c, double outer = 26, double inner = 11, string? fill = "#fbbf24", string? stroke = "#f59e0b")
    {
        var pts = new List<CanvasPoint>();
        for (var i = 0; i < 10; i++)
        {
            var r = (i % 2 == 0) ? outer : inner;
            var a = -Math.PI / 2 + i * Math.PI / 5;
            pts.Add(new CanvasPoint(c.X + r * Math.Cos(a), c.Y + r * Math.Sin(a)));
        }
        return new CanvasPath(pts, Closed: true, Smooth: false) { Fill = fill, Stroke = stroke, StrokeWidth = 2 };
    }

    private static CanvasStamp Emoji(string key, string ch) =>
        new(key, ch, p => new CanvasText(p.X - 16, p.Y + 16, ch, FontSize: 34), ch);

    /// <summary>The default palette: a geometric star plus a set of emoji stamps.</summary>
    public static readonly IReadOnlyList<CanvasStamp> Default = new[]
    {
        new CanvasStamp("star", "Star", p => Star(p), "★"),
        Emoji("heart", "♥"),
        Emoji("check", "✔"),
        Emoji("arrow", "➜"),
        Emoji("pin", "📍"),
        Emoji("smile", "🙂"),
        Emoji("bulb", "💡"),
        Emoji("flag", "🚩"),
    };
}
