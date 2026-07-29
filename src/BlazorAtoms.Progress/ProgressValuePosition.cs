namespace BlazorAtoms.Progress;

/// <summary>Where <c>AtomProgressBar</c> puts its formatted value readout. Only consulted when
/// <c>ShowValue</c> is true.</summary>
public enum ProgressValuePosition
{
    /// <summary>Inside the filled portion, right-aligned against its leading edge. Default.</summary>
    Inside,

    /// <summary>After the track, on the same row.</summary>
    Outside,

    /// <summary>Above the track, on the label's row (right-aligned opposite the label).</summary>
    Above,
}
