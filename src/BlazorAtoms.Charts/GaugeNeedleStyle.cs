namespace BlazorAtoms.Charts;

/// <summary>How <see cref="AtomGauge"/> draws its needle.</summary>
public enum GaugeNeedleStyle
{
    /// <summary>A plain stroked line from the hub to the tip. The default.</summary>
    Line,

    /// <summary>A tapered dart — wide near the hub, pointed at the tip — with a short tail
    /// counterweight opposite the tip and a two-layer hub (outer ring, inner disc).</summary>
    Tapered,

    /// <summary>A short, bold filled triangle from the hub outward — stubbier than
    /// <see cref="Tapered"/>, no tail, no separate hub layer (the face plate itself reads as the
    /// backdrop it pivots against).</summary>
    Triangle,

    /// <summary>Not a centre-pivot needle at all — a small tab straddling the band's outer edge at
    /// the value's position, tip poking into the band, base sitting just outside it. Reads as a
    /// slider riding the rim rather than a needle swinging from the middle.</summary>
    RimTab,
}
