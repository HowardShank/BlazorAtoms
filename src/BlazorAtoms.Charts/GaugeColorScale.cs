using System.Globalization;
using System.Text.RegularExpressions;

namespace BlazorAtoms.Charts;

/// <summary>
/// Interpolates between two colors for the gauge family's auto-generated <see cref="GaugeBand"/>s.
/// </summary>
/// <remarks>
/// <b>Hue is swept in HSL space, not lerped in RGB.</b> A straight RGB lerp from red
/// <c>(229,72,77)</c> to green <c>(48,164,108)</c> crosses a muddy brown/olive midpoint around
/// <c>t≈0.5</c> — RGB interpolation has no notion of "the color wheel between red and green passes
/// through yellow." Converting both ends to HSL and sweeping <i>hue</i> along the shorter arc forces
/// the midpoint through orange/yellow instead, which is what every red→green status scale (traffic
/// lights, credit-score dials, health meters) actually looks like.
/// </remarks>
public static class GaugeColorScale
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly Regex HexPattern = new("^#?([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);

    /// <summary>The 147 standard CSS/SVG named colors, lower-cased name → 6-digit hex. StartColor/EndColor
    /// are meant to accept anything a plain <c>stroke</c>/<c>fill</c> attribute would — a named color
    /// works everywhere else in this family already — so <see cref="ResolveHex"/> resolves these before
    /// falling back, rather than only ever accepting literal hex.</summary>
    private static readonly Dictionary<string, string> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aliceblue"] = "#f0f8ff", ["antiquewhite"] = "#faebd7", ["aqua"] = "#00ffff", ["aquamarine"] = "#7fffd4",
        ["azure"] = "#f0ffff", ["beige"] = "#f5f5dc", ["bisque"] = "#ffe4c4", ["black"] = "#000000",
        ["blanchedalmond"] = "#ffebcd", ["blue"] = "#0000ff", ["blueviolet"] = "#8a2be2", ["brown"] = "#a52a2a",
        ["burlywood"] = "#deb887", ["cadetblue"] = "#5f9ea0", ["chartreuse"] = "#7fff00", ["chocolate"] = "#d2691e",
        ["coral"] = "#ff7f50", ["cornflowerblue"] = "#6495ed", ["cornsilk"] = "#fff8dc", ["crimson"] = "#dc143c",
        ["cyan"] = "#00ffff", ["darkblue"] = "#00008b", ["darkcyan"] = "#008b8b", ["darkgoldenrod"] = "#b8860b",
        ["darkgray"] = "#a9a9a9", ["darkgreen"] = "#006400", ["darkgrey"] = "#a9a9a9", ["darkkhaki"] = "#bdb76b",
        ["darkmagenta"] = "#8b008b", ["darkolivegreen"] = "#556b2f", ["darkorange"] = "#ff8c00", ["darkorchid"] = "#9932cc",
        ["darkred"] = "#8b0000", ["darksalmon"] = "#e9967a", ["darkseagreen"] = "#8fbc8f", ["darkslateblue"] = "#483d8b",
        ["darkslategray"] = "#2f4f4f", ["darkslategrey"] = "#2f4f4f", ["darkturquoise"] = "#00ced1", ["darkviolet"] = "#9400d3",
        ["deeppink"] = "#ff1493", ["deepskyblue"] = "#00bfff", ["dimgray"] = "#696969", ["dimgrey"] = "#696969",
        ["dodgerblue"] = "#1e90ff", ["firebrick"] = "#b22222", ["floralwhite"] = "#fffaf0", ["forestgreen"] = "#228b22",
        ["fuchsia"] = "#ff00ff", ["gainsboro"] = "#dcdcdc", ["ghostwhite"] = "#f8f8ff", ["gold"] = "#ffd700",
        ["goldenrod"] = "#daa520", ["gray"] = "#808080", ["green"] = "#008000", ["greenyellow"] = "#adff2f",
        ["grey"] = "#808080", ["honeydew"] = "#f0fff0", ["hotpink"] = "#ff69b4", ["indianred"] = "#cd5c5c",
        ["indigo"] = "#4b0082", ["ivory"] = "#fffff0", ["khaki"] = "#f0e68c", ["lavender"] = "#e6e6fa",
        ["lavenderblush"] = "#fff0f5", ["lawngreen"] = "#7cfc00", ["lemonchiffon"] = "#fffacd", ["lightblue"] = "#add8e6",
        ["lightcoral"] = "#f08080", ["lightcyan"] = "#e0ffff", ["lightgoldenrodyellow"] = "#fafad2", ["lightgray"] = "#d3d3d3",
        ["lightgreen"] = "#90ee90", ["lightgrey"] = "#d3d3d3", ["lightpink"] = "#ffb6c1", ["lightsalmon"] = "#ffa07a",
        ["lightseagreen"] = "#20b2aa", ["lightskyblue"] = "#87cefa", ["lightslategray"] = "#778899", ["lightslategrey"] = "#778899",
        ["lightsteelblue"] = "#b0c4de", ["lightyellow"] = "#ffffe0", ["lime"] = "#00ff00", ["limegreen"] = "#32cd32",
        ["linen"] = "#faf0e6", ["magenta"] = "#ff00ff", ["maroon"] = "#800000", ["mediumaquamarine"] = "#66cdaa",
        ["mediumblue"] = "#0000cd", ["mediumorchid"] = "#ba55d3", ["mediumpurple"] = "#9370db", ["mediumseagreen"] = "#3cb371",
        ["mediumslateblue"] = "#7b68ee", ["mediumspringgreen"] = "#00fa9a", ["mediumturquoise"] = "#48d1cc", ["mediumvioletred"] = "#c71585",
        ["midnightblue"] = "#191970", ["mintcream"] = "#f5fffa", ["mistyrose"] = "#ffe4e1", ["moccasin"] = "#ffe4b5",
        ["navajowhite"] = "#ffdead", ["navy"] = "#000080", ["oldlace"] = "#fdf5e6", ["olive"] = "#808000",
        ["olivedrab"] = "#6b8e23", ["orange"] = "#ffa500", ["orangered"] = "#ff4500", ["orchid"] = "#da70d6",
        ["palegoldenrod"] = "#eee8aa", ["palegreen"] = "#98fb98", ["paleturquoise"] = "#afeeee", ["palevioletred"] = "#db7093",
        ["papayawhip"] = "#ffefd5", ["peachpuff"] = "#ffdab9", ["peru"] = "#cd853f", ["pink"] = "#ffc0cb",
        ["plum"] = "#dda0dd", ["powderblue"] = "#b0e0e6", ["purple"] = "#800080", ["rebeccapurple"] = "#663399",
        ["red"] = "#ff0000", ["rosybrown"] = "#bc8f8f", ["royalblue"] = "#4169e1", ["saddlebrown"] = "#8b4513",
        ["salmon"] = "#fa8072", ["sandybrown"] = "#f4a460", ["seagreen"] = "#2e8b57", ["seashell"] = "#fff5ee",
        ["sienna"] = "#a0522d", ["silver"] = "#c0c0c0", ["skyblue"] = "#87ceeb", ["slateblue"] = "#6a5acd",
        ["slategray"] = "#708090", ["slategrey"] = "#708090", ["snow"] = "#fffafa", ["springgreen"] = "#00ff7f",
        ["steelblue"] = "#4682b4", ["tan"] = "#d2b48c", ["teal"] = "#008080", ["thistle"] = "#d8bfd8",
        ["tomato"] = "#ff6347", ["turquoise"] = "#40e0d0", ["violet"] = "#ee82ee", ["wheat"] = "#f5deb3",
        ["white"] = "#ffffff", ["whitesmoke"] = "#f5f5f5", ["yellow"] = "#ffff00", ["yellowgreen"] = "#9acd32",
    };

    /// <summary>Whether <paramref name="hex"/> is a 3- or 6-digit hex color, with or without a leading
    /// <c>#</c> — one of the two shapes <see cref="ResolveHex"/> accepts (the other being a named color).
    /// Callers that accept an arbitrary/possibly-partial color string (e.g. one bound live to a text
    /// input) should validate through <see cref="ResolveHex"/> before handing the value to
    /// <see cref="Lerp"/>, rather than relying on it to fail gracefully — see <see cref="HexToRgb"/>'s
    /// remarks for why the fallback there is a last resort, not a substitute.</summary>
    public static bool IsValidHex(string? hex) => hex is not null && HexPattern.IsMatch(hex);

    /// <summary>
    /// Resolves <paramref name="color"/> to a normalized <c>#rrggbb</c> hex string, or null if it's
    /// neither valid hex nor one of the standard CSS named colors (<see cref="NamedColors"/>) — including
    /// every partial/invalid string a live-bound text input passes through on the way to a real color.
    /// <see cref="StartColor"/>/<see cref="EndColor"/>-style parameters accept a named color exactly like
    /// a plain SVG <c>stroke</c>/<c>fill</c> attribute would elsewhere in this family; only the
    /// auto-generated scale's own HSL math needs it as hex first.
    /// </summary>
    public static string? ResolveHex(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;

        var trimmed = color.Trim();
        if (IsValidHex(trimmed)) return trimmed.StartsWith('#') ? trimmed : "#" + trimmed;

        return NamedColors.TryGetValue(trimmed, out var hex) ? hex : null;
    }

    /// <summary>Interpolates from <paramref name="startHex"/> to <paramref name="endHex"/> at
    /// <paramref name="t"/> (clamped 0..1), sweeping hue along the shorter arc between the two.</summary>
    public static string Lerp(string startHex, string endHex, double t)
    {
        t = Math.Clamp(t, 0, 1);

        var (h1, s1, l1) = RgbToHsl(HexToRgb(startHex));
        var (h2, s2, l2) = RgbToHsl(HexToRgb(endHex));

        var delta = h2 - h1;
        delta -= Math.Floor(delta / 360 + 0.5) * 360; // shortest signed path, in (-180, 180]

        var h = h1 + delta * t;
        var s = s1 + (s2 - s1) * t;
        var l = l1 + (l2 - l1) * t;

        return RgbToHex(HslToRgb(h, s, l));
    }

    /// <summary>Slices <paramref name="min"/>..<paramref name="max"/> into <paramref name="count"/>
    /// equal-width bands, colored from <paramref name="startColor"/> to <paramref name="endColor"/>.</summary>
    public static IReadOnlyList<GaugeBand> Bands(int count, double min, double max, string startColor, string endColor)
    {
        if (count <= 0) return [];

        var bands = new GaugeBand[count];
        var step = (max - min) / count;

        for (var i = 0; i < count; i++)
        {
            var t = count == 1 ? 1.0 : (double)i / (count - 1);
            bands[i] = new GaugeBand(min + step * (i + 1), Lerp(startColor, endColor, t));
        }

        return bands;
    }

    /// <summary>
    /// Never throws, even on input <see cref="IsValidHex"/> would reject — callers are expected to
    /// validate first (every gauge does, via <c>ResolvedStartColor</c>/<c>ResolvedEndColor</c>), but this
    /// is the single place every hex string in the family ultimately flows through, so it stays defensive
    /// on its own rather than trusting every call site to have checked. Malformed input falls back to
    /// black rather than crashing — visually wrong for one frame is recoverable, an unhandled exception
    /// mid-render is not.
    /// </summary>
    private static (double R, double G, double B) HexToRgb(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length == 3) h = string.Concat(h.Select(c => new string(c, 2)));
        if (h.Length != 6 || !h.All(Uri.IsHexDigit)) return (0, 0, 0);

        var r = Convert.ToInt32(h[..2], 16) / 255.0;
        var g = Convert.ToInt32(h[2..4], 16) / 255.0;
        var b = Convert.ToInt32(h[4..6], 16) / 255.0;
        return (r, g, b);
    }

    private static string RgbToHex((double R, double G, double B) rgb)
    {
        static int To255(double v) => Math.Clamp((int)Math.Round(v * 255), 0, 255);
        return string.Create(Inv, $"#{To255(rgb.R):x2}{To255(rgb.G):x2}{To255(rgb.B):x2}");
    }

    private static (double H, double S, double L) RgbToHsl((double R, double G, double B) rgb)
    {
        var (r, g, b) = rgb;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2;

        if (max == min) return (0, 0, l);

        var d = max - min;
        var s = l > 0.5 ? d / (2 - max - min) : d / (max + min);

        double h;
        if (max == r) h = ((g - b) / d + (g < b ? 6 : 0));
        else if (max == g) h = (b - r) / d + 2;
        else h = (r - g) / d + 4;

        return (h * 60, s, l);
    }

    private static (double R, double G, double B) HslToRgb(double h, double s, double l)
    {
        h = ((h % 360) + 360) % 360;
        s = Math.Clamp(s, 0, 1);
        l = Math.Clamp(l, 0, 1);

        if (s == 0) return (l, l, l);

        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        var hk = h / 360;

        return (HueToRgb(p, q, hk + 1.0 / 3), HueToRgb(p, q, hk), HueToRgb(p, q, hk - 1.0 / 3));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }
}
