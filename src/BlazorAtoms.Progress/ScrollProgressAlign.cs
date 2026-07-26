namespace BlazorAtoms.Progress;

/// <summary>Horizontal alignment of <see cref="AtomScrollProgressBar"/>'s track within its scroll
/// container, when <see cref="AtomScrollProgressBar.Width"/> makes the track narrower than the
/// container itself.</summary>
public enum ScrollProgressAlign
{
    /// <summary>Track's left edge aligns with the container's left edge.</summary>
    Start,

    /// <summary>Track is centered within the container.</summary>
    Center,

    /// <summary>Track's right edge aligns with the container's right edge.</summary>
    End,
}
