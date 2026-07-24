using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Typography;

/// <summary>Zero-JS hover effect: layered 3D text-shadow, a colorized glare sweep, and SVG
/// sparkles popping in around the text on hover. Pure CSS <c>:hover</c>/<c>:active</c> — no C#
/// state drives the trigger itself.</summary>
public partial class AtomTextSparkle
{
    /// <summary>The text to display. Empty renders nothing.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = "";

    /// <summary>Optional link target. When set, renders a real <c>&lt;a href&gt;</c>; when null,
    /// renders a focusable (<c>tabindex="0"</c>) element with the same hover effect but no navigation.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>Fill color of the glare-sweep text layer.</summary>
    [Parameter] public string Color { get; set; } = "#eab308";

    /// <summary>Color of the layered 3D text-shadow behind the text.</summary>
    [Parameter] public string ShadowColor { get; set; } = "#a16207";

    /// <summary>Color of the glare sweep and the sparkle SVGs.</summary>
    [Parameter] public string GlareColor { get; set; } = "hsl(0 0% 100% / 0.75)";

    /// <summary>How many sparkle SVGs to scatter around the text.</summary>
    [Parameter] public int SparkleCount { get; set; } = 7;

    /// <summary>Font size of the text (sparkle size and shadow depth scale off this, in <c>em</c>).</summary>
    [Parameter] public string FontSize { get; set; } = "1.5rem";

    private string RootStyle =>
        $"--atom-text-sparkle-color:{Color};" +
        $"--atom-text-sparkle-shadow:{ShadowColor};" +
        $"--atom-text-sparkle-glare:{GlareColor};" +
        $"--atom-text-sparkle-font-size:{FontSize};";

    private readonly record struct SparklePosition(double X, double Y, double Scale, int DelayStep);

    // Deterministic scatter per index — no System.Random. A time/instance-seeded random would
    // place sparkles differently on the server-rendered markup vs. the first interactive
    // re-render, causing a visible jump on hydration; a pure function of the index can't.
    private static SparklePosition GetSparkle(int index) => new(
        X: (index * 53) % 100,
        Y: 20 + (index * 37) % 70,
        // Wide 0.4–1.6 range (5 buckets) so sparkles read as genuinely varied in size, not the
        // narrow 0.8–1.25 cluster the original formula produced.
        Scale: 0.4 + (index % 5) * 0.3,
        DelayStep: 1 + (index % 4));

    private static string SparkleStyle(SparklePosition s) =>
        $"left:{s.X.ToString(CultureInfo.InvariantCulture)}%;" +
        $"top:{s.Y.ToString(CultureInfo.InvariantCulture)}%;" +
        $"--atom-text-sparkle-scale:{s.Scale.ToString(CultureInfo.InvariantCulture)};" +
        $"--atom-text-sparkle-delay:{s.DelayStep}s;";
}
