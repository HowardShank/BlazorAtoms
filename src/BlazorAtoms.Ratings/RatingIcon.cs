namespace BlazorAtoms.Ratings;

/// <summary>
/// Built-in icon shapes for <see cref="AtomRating"/>. Each maps to an inline-SVG path drawn in a
/// <c>0 0 24 24</c> view box (see <see cref="RatingGlyphs"/>). To use a glyph that isn't listed here,
/// leave <see cref="AtomRating.Icon"/> at its default and set <see cref="AtomRating.IconPath"/> to your
/// own path data instead.
/// </summary>
public enum RatingIcon
{
    /// <summary>Five-pointed star — the conventional rating icon.</summary>
    Star,
    /// <summary>Heart — common for "favorites" / likes.</summary>
    Heart,
    /// <summary>Filled circle / dot.</summary>
    Circle,
    /// <summary>Square.</summary>
    Square,
    /// <summary>Diamond (rotated square).</summary>
    Diamond,
    /// <summary>Brilliant-cut gemstone / jewel silhouette.</summary>
    Gem,
    /// <summary>Emerald-cut gemstone (step-cut octagon).</summary>
    Emerald,
    /// <summary>Marquise-cut gemstone (pointed oval / navette).</summary>
    Marquise,
    /// <summary>Pear / teardrop-cut gemstone.</summary>
    Teardrop,
    /// <summary>Apple fruit.</summary>
    Apple,
    /// <summary>Pair of cherries.</summary>
    Cherry,
    /// <summary>Lemon / citrus fruit.</summary>
    Lemon,
    /// <summary>Bunch of grapes.</summary>
    Grape,
    /// <summary>Strawberry.</summary>
    Strawberry,
    /// <summary>Banana.</summary>
    Banana,
    /// <summary>Upward triangle.</summary>
    Triangle,
    /// <summary>Thumbs-up.</summary>
    Thumb,
    /// <summary>Lightning bolt.</summary>
    Bolt,
}
