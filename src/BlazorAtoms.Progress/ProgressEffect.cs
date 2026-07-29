namespace BlazorAtoms.Progress;

/// <summary>
/// Opt-in motion/texture for the filled portion, driven entirely by CSS — no C# trigger state, so it
/// behaves identically in every render mode. Emitted as <c>data-effect</c> on the component root
/// (omitted for <see cref="None"/>, so the default costs nothing). Adding an effect is one enum
/// member plus one CSS block.
/// </summary>
/// <remarks>
/// Independent of the indeterminate state: a null <c>Value</c> plays its own sweep keyframe
/// regardless of which effect is selected, since "we don't know the amount" is a different thing
/// from "decorate the amount we do know".
/// </remarks>
public enum ProgressEffect
{
    /// <summary>Flat fill. Default.</summary>
    None,

    /// <summary>Static 45° candy stripes over the fill.</summary>
    Stripes,

    /// <summary>The same stripes, scrolling along the track.</summary>
    StripesAnimated,

    /// <summary>A soft highlight sweeps across the fill on a loop.</summary>
    Shimmer,

    /// <summary>Fill casts a colored halo in its own accent.</summary>
    Glow,

    /// <summary>Fill breathes between full and reduced opacity.</summary>
    Pulse,

    /// <summary>Fill is a gradient from a lighter tint of the accent to the accent itself.</summary>
    Gradient,
}
