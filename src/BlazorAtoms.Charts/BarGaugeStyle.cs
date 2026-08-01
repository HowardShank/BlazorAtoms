namespace BlazorAtoms.Charts;

/// <summary>How an <see cref="AtomBarGauge"/> renders its track.</summary>
public enum BarGaugeStyle
{
    /// <summary>Discrete flat-color blocks, one per band. The default.</summary>
    Segmented,

    /// <summary>One smooth multi-stop gradient across the whole track, built from the same band
    /// colors — the one shape in the gauge family that needs an SVG <c>&lt;linearGradient&gt;</c>.</summary>
    Gradient,

    /// <summary>Many thin tick rectangles, colored per band, with the tick nearest the current value
    /// highlighted.</summary>
    Ticks,
}
