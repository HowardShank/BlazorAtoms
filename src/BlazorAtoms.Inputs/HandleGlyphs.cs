namespace BlazorAtoms.Inputs;

/// <summary>
/// SVG path data for the mask-based <see cref="HandleShape"/> handles (everything except the
/// CSS-only <see cref="HandleShape.Round"/>/<see cref="HandleShape.Square"/>). Each path is drawn in
/// a <c>0 0 24 24</c> view box so the shapes scale together to any handle size.
///
/// These silhouettes are intentionally *duplicated* (copied path strings), not imported, from the
/// equivalent glyphs in <c>BlazorAtoms.Ratings</c> — every BlazorAtoms library stays standalone with
/// zero cross-library dependencies, so a small amount of shared shape data is copied rather than
/// referenced.
/// </summary>
internal static class HandleGlyphs
{
    public const string ViewBox = "0 0 24 24";

    private static readonly IReadOnlyDictionary<HandleShape, string> Paths = new Dictionary<HandleShape, string>
    {
        [HandleShape.Heart] = "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z",
        [HandleShape.Star] = "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z",
        [HandleShape.Diamond] = "M12 2l10 10-10 10L2 12z",
        [HandleShape.Triangle] = "M12 3l9 18H3z",
        [HandleShape.Teardrop] = "M12 2c-1 5-7 8-7 13a7 7 0 1 0 14 0c0-5-6-8-7-13z",
        [HandleShape.Gem] = "M6 3h12l3.5 5.5L12 21 2.5 8.5z",
        [HandleShape.Bolt] = "M7 2v11h3v9l7-12h-4l4-8z",
    };

    /// <summary>True when the shape is rendered via an SVG mask (as opposed to the CSS-only
    /// Round/Square).</summary>
    public static bool IsGlyph(HandleShape shape) => Paths.ContainsKey(shape);

    /// <summary>The SVG path data for <paramref name="shape"/>, or null for the CSS-only shapes.</summary>
    public static string? Path(HandleShape shape) => Paths.TryGetValue(shape, out var d) ? d : null;
}
