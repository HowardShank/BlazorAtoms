namespace BlazorAtoms.Progress;

/// <summary>End treatment of <c>AtomProgressRing</c>'s arc — maps straight onto SVG
/// <c>stroke-linecap</c>.</summary>
public enum ProgressRingCap
{
    /// <summary>Square-cut ends flush with the arc. Default.</summary>
    Butt,

    /// <summary>Semicircular ends that overhang the arc by half the stroke width.</summary>
    Round,
}
