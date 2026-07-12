namespace BlazorAtoms.Ratings;

/// <summary>
/// SVG path data for the built-in <see cref="RatingIcon"/> shapes. Every path is authored for a
/// <c>0 0 24 24</c> view box so the shapes share one coordinate space and scale together. A custom
/// <see cref="AtomRating.IconPath"/> should target the same view box (override it with
/// <see cref="AtomRating.IconViewBox"/> if yours differs).
/// </summary>
public static class RatingGlyphs
{
    /// <summary>The default view box every built-in glyph is drawn in.</summary>
    public const string ViewBox = "0 0 24 24";

    private static readonly IReadOnlyDictionary<RatingIcon, string> Paths = new Dictionary<RatingIcon, string>
    {
        [RatingIcon.Star] = "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z",
        [RatingIcon.Heart] = "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z",
        [RatingIcon.Circle] = "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20z",
        [RatingIcon.Square] = "M4 4h16v16H4z",
        [RatingIcon.Diamond] = "M12 2l10 10-10 10L2 12z",
        [RatingIcon.Gem] = "M6 3h12l3.5 5.5L12 21 2.5 8.5z",
        [RatingIcon.Emerald] = "M8 4h8l4 4v8l-4 4H8l-4-4V8z",
        [RatingIcon.Marquise] = "M3 12C6 6 18 6 21 12 18 18 6 18 3 12z",
        [RatingIcon.Teardrop] = "M12 2c-1 5-7 8-7 13a7 7 0 1 0 14 0c0-5-6-8-7-13z",
        [RatingIcon.Apple] = "M12 8c-1.3-1.3-3-2-4.5-2C4.5 6 3 8.5 3 12c0 4.5 3 9 6 9 1 0 1.8-.6 3-.6s2 .6 3 .6c3 0 6-4.5 6-9 0-3.5-1.5-6-4.5-6-1.5 0-3.2.7-4.5 2zM12 8c.3-2 1.7-3.5 3.8-3.8-.2 2-1.6 3.5-3.8 3.8z",
        [RatingIcon.Cherry] = "M6.5 12.5a4 4 0 1 0 0 8 4 4 0 1 0 0-8zM15.5 13a4 4 0 1 0 0 8 4 4 0 1 0 0-8zM14 4c-2 3-5 5-7.1 8.5.5.3 1 .6 1.4 1C10 10 13 8 15 5zM14 4c1.5 2.5 2 5.5 1.5 8.7-.5-.2-1-.3-1.6-.4C14.3 9.5 14.5 6.7 14 4z",
        [RatingIcon.Lemon] = "M20.5 12c0-3.3-3.8-6-8.5-6S3.5 8.7 3.5 12s3.8 6 8.5 6 8.5-2.7 8.5-6zM3.5 12l-2.2-1.2.6 1.2-.6 1.2zM20.5 12l2.2-1.2-.6 1.2.6 1.2z",
        [RatingIcon.Grape] = "M8.5 6.6a2.4 2.4 0 1 0 0 4.8 2.4 2.4 0 1 0 0-4.8zM12 6.6a2.4 2.4 0 1 0 0 4.8 2.4 2.4 0 1 0 0-4.8zM15.5 6.6a2.4 2.4 0 1 0 0 4.8 2.4 2.4 0 1 0 0-4.8zM10.2 10.2a2.4 2.4 0 1 0 0 4.8 2.4 2.4 0 1 0 0-4.8zM13.8 10.2a2.4 2.4 0 1 0 0 4.8 2.4 2.4 0 1 0 0-4.8zM12 13.8a2.4 2.4 0 1 0 0 4.8 2.4 2.4 0 1 0 0-4.8zM10 3h4l-2 3z",
        [RatingIcon.Strawberry] = "M12 21c-4.3-2.8-7-6.3-7-9.8 0-2.4 1.9-3.7 4-3.7 1.3 0 2.2.5 3 1 .8-.5 1.7-1 3-1 2.1 0 4 1.3 4 3.7 0 3.5-2.7 7-7 9.8zM12 3.5l2.2 3.3h-4.4zM7.8 5l2.8 2.6-3.4.6zM16.2 5l-2.8 2.6 3.4.6z",
        [RatingIcon.Banana] = "M4 6C3 16 10 21 18 19 11 17 7 12 8 6 7 5 5 5 4 6z",
        [RatingIcon.Triangle] = "M12 3l9 18H3z",
        [RatingIcon.Thumb] = "M1 21h4V9H1v12zm22-11c0-1.1-.9-2-2-2h-6.31l.95-4.57.03-.32c0-.41-.17-.79-.44-1.06L14.17 1 7.59 7.59C7.22 7.96 7 8.45 7 9v10c0 1.1.9 2 2 2h9c.83 0 1.54-.5 1.84-1.22l3.02-7.05c.09-.23.14-.47.14-.73v-2z",
        [RatingIcon.Bolt] = "M7 2v11h3v9l7-12h-4l4-8z",
    };

    /// <summary>The SVG path data for <paramref name="icon"/> (falls back to the star).</summary>
    public static string Path(RatingIcon icon) =>
        Paths.TryGetValue(icon, out var d) ? d : Paths[RatingIcon.Star];
}
