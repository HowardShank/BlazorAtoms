namespace BlazorAtoms.AnimatedBadges;

/// <summary>Badge outline shape.</summary>
public enum Shape
{
    /// <summary>Stadium / pill — fully rounded ends (default).</summary>
    Pill,
    /// <summary>Circle — equal width/height, for single digits or a dot.</summary>
    Circle,
    /// <summary>Square with sharp corners.</summary>
    Square,
    /// <summary>Rounded rectangle; corner radius via <c>Radius</c>.</summary>
    Rounded,
}
