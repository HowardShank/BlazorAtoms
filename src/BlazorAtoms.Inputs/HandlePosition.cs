namespace BlazorAtoms.Inputs;

/// <summary>
/// Vertical position of <see cref="AtomRangeInput{TValue}"/>'s handle relative to the track. Use
/// <see cref="AtomRangeInput{TValue}.HandleOffset"/> for a precise px override.
/// </summary>
public enum HandlePosition
{
    /// <summary>Handle centered on the track (default).</summary>
    Center,

    /// <summary>Handle raised to sit just above the track.</summary>
    Above,

    /// <summary>Handle dropped to sit just below the track.</summary>
    Below,
}
