using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Transitions;

/// <summary>Zero-JS hover-effect wrapper around arbitrary <see cref="ChildContent"/>. The trigger
/// is plain CSS <c>:hover</c>/<c>:active</c> — unlike <see cref="AtomTransition"/>, no C# state
/// drives it at all.</summary>
public partial class AtomHoverEffect
{
    /// <summary>The content the effect wraps — any element, not just text.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Which hover effect to play.</summary>
    [Parameter] public HoverEffect Effect { get; set; } = HoverEffect.Sparkle;

    /// <summary>Optional link target. When set, renders a real <c>&lt;a href&gt;</c>; when null,
    /// renders a focusable (<c>tabindex="0"</c>) element with the same hover effect but no navigation.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>Color of the glow/sparkle accents.</summary>
    [Parameter] public string GlowColor { get; set; } = "#eab308";

    /// <summary>How many sparkle SVGs to scatter around the content. Ignored for effects that
    /// don't use sparkles.</summary>
    [Parameter] public int SparkleCount { get; set; } = 5;

    /// <summary>How much the content scales up on hover (e.g. <c>1.05</c> = 5% larger).</summary>
    [Parameter] public double ScaleAmount { get; set; } = 1.05;

    private string EffectClass => $"atom-hover-effect-{Effect.ToString().ToLowerInvariant()}";

    private string RootStyle =>
        $"--atom-hover-effect-glow:{GlowColor};" +
        $"--atom-hover-effect-scale:{ScaleAmount.ToString(CultureInfo.InvariantCulture)};";

    private readonly record struct SparklePosition(double X, double Y, double Scale, int DelayStep);

    // Same deterministic-scatter reasoning as AtomTextSparkle: a pure function of the index, not
    // System.Random, so server-rendered and first-interactive markup place sparkles identically —
    // no visible jump on hydration.
    private static SparklePosition GetSparkle(int index) => new(
        X: (index * 53) % 100,
        Y: 20 + (index * 37) % 70,
        Scale: 0.4 + (index % 5) * 0.3,
        DelayStep: 1 + (index % 4));

    private static string SparkleStyle(SparklePosition s) =>
        $"left:{s.X.ToString(CultureInfo.InvariantCulture)}%;" +
        $"top:{s.Y.ToString(CultureInfo.InvariantCulture)}%;" +
        $"--atom-hover-effect-sparkle-scale:{s.Scale.ToString(CultureInfo.InvariantCulture)};" +
        $"--atom-hover-effect-sparkle-delay:{s.DelayStep}s;";
}
