namespace BlazorAtoms.Charts;

/// <summary>How <see cref="AtomGauge"/> colors its arc. Mirrors <see cref="BarGaugeStyle"/>.</summary>
public enum GaugeArcStyle
{
    /// <summary>Discrete color bands from <see cref="AtomGaugeBase.EffectiveBands"/>. Default.</summary>
    Segmented,

    /// <summary>
    /// A smooth-looking sweep from <see cref="AtomGaugeBase.StartColor"/> to
    /// <see cref="AtomGaugeBase.EndColor"/>. SVG has no native curved gradient — a <c>&lt;linearGradient&gt;</c>
    /// sweeps in a straight line, which reads as a diagonal streak across an arc rather than a ring sweep —
    /// so this draws many thin arc slices instead, each one flat-colored by the same hue-sweep scale
    /// <see cref="GaugeColorScale"/> uses for bands, fine enough to look continuous.
    /// </summary>
    Gradient,

    /// <summary>Thin radial tick marks around the arc, colored by the same scale as <see cref="Gradient"/>
    /// rather than <see cref="AtomGaugeBase.EffectiveBands"/> — a tick ruler reads finer-grained than a
    /// handful of bands, so it draws its own scale independent of <see cref="AtomGaugeBase.SegmentCount"/>.</summary>
    Ticks,
}
