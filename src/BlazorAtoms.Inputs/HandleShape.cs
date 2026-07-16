namespace BlazorAtoms.Inputs;

/// <summary>
/// Shape of <see cref="AtomRangeInput{TValue}"/>'s drag handle. Purely CSS-driven via
/// <c>data-handle-shape</c> on the native input — new shapes can be added without changing the
/// component's C# surface or markup.
/// </summary>
public enum HandleShape
{
    /// <summary>Default circular handle.</summary>
    Round,

    /// <summary>Square handle with slightly rounded corners.</summary>
    Square,

    /// <summary>Heart-shaped handle.</summary>
    Heart,

    /// <summary>Five-pointed star.</summary>
    Star,

    /// <summary>Diamond (rotated square).</summary>
    Diamond,

    /// <summary>Upward triangle.</summary>
    Triangle,

    /// <summary>Pear / teardrop.</summary>
    Teardrop,

    /// <summary>Brilliant-cut gemstone.</summary>
    Gem,

    /// <summary>Lightning bolt.</summary>
    Bolt,
}

// Heart, Star, Diamond, Triangle, Teardrop, Gem, and Bolt render via an SVG mask
// (see HandleGlyphs); Round and Square are CSS-only. Add a new shape by adding an enum value plus
// its path in HandleGlyphs — no other code changes needed.
