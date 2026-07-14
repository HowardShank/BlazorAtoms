using System.Globalization;
using System.Text.RegularExpressions;

namespace BlazorAtoms.Highlights;

/// <summary>
/// Strategy used by <see cref="HighlightColor"/> suggestions.
/// </summary>
public enum HighlightColorStrategy
{
    /// <summary>
    /// Pick a curated color that maximizes readability contrast.
    /// </summary>
    BestContrast,

    /// <summary>
    /// Pick a color opposite the source color on the color wheel.
    /// </summary>
    Complementary,

    /// <summary>
    /// Pick a lighter or darker variant of the source color.
    /// </summary>
    Monochromatic
}

/// <summary>
/// Color helpers for text highlights. Suggests accessible foreground or background
/// colors given the other color and can report WCAG contrast ratios.
/// </summary>
public static partial class HighlightColor
{
    /// <summary>
    /// Returns a foreground color (black or white) with the best contrast against
    /// the supplied background color.
    /// </summary>
    /// <param name="background">CSS hex color (#RGB or #RRGGBB) or named color.</param>
    /// <param name="strategy">Strategy controlling how the suggestion is chosen.</param>
    public static string SuggestForeground(string? background, HighlightColorStrategy strategy = HighlightColorStrategy.BestContrast)
    {
        if (!TryParseColor(background, out var rgb)) return "#1f2937";

        var bgHsl = RgbToHsl(rgb);
        var sourceLuminance = Luminance(rgb);

        return strategy switch
        {
            HighlightColorStrategy.Complementary => BestAgainstHsl(new Hsl((bgHsl.H + 180) % 360, bgHsl.S, bgHsl.L), sourceLuminance),
            HighlightColorStrategy.Monochromatic => BestAgainstHsl(bgHsl with { L = bgHsl.L > 50 ? 15 : 90 }, sourceLuminance),
            _ => sourceLuminance > 0.179 ? "#1f2937" : "#ffffff"
        };
    }

    /// <summary>
    /// Returns a background color with good contrast against the supplied foreground
    /// color. Defaults to a pale yellow for dark text and a dark blue for light text.
    /// </summary>
    /// <param name="foreground">CSS hex color (#RGB or #RRGGBB) or named color.</param>
    /// <param name="strategy">Strategy controlling how the suggestion is chosen.</param>
    public static string SuggestBackground(string? foreground, HighlightColorStrategy strategy = HighlightColorStrategy.BestContrast)
    {
        if (!TryParseColor(foreground, out var rgb)) return "#fde047";

        var fgHsl = RgbToHsl(rgb);
        var sourceLuminance = Luminance(rgb);

        return strategy switch
        {
            HighlightColorStrategy.Complementary => BestAgainstHsl(new Hsl((fgHsl.H + 180) % 360, fgHsl.S, fgHsl.L), sourceLuminance),
            HighlightColorStrategy.Monochromatic => BestAgainstHsl(fgHsl with { L = fgHsl.L > 50 ? 15 : 90 }, sourceLuminance),
            _ => sourceLuminance > 0.179 ? "#1e3a8a" : "#fde047"
        };
    }

    /// <summary>
    /// Returns the WCAG 2.1 contrast ratio between two colors.
    /// 1:1 is no contrast; 21:1 is maximum contrast.
    /// </summary>
    public static double ContrastRatio(string? a, string? b)
    {
        if (!TryParseColor(a, out var ca) || !TryParseColor(b, out var cb)) return 1.0;

        var la = Luminance(ca) + 0.05;
        var lb = Luminance(cb) + 0.05;
        return la > lb ? la / lb : lb / la;
    }

    [GeneratedRegex("^#(?<r>[0-9a-fA-F])(?<g>[0-9a-fA-F])(?<b>[0-9a-fA-F])$")]
    private static partial Regex ShortHexRegex();

    [GeneratedRegex("^#(?<r>[0-9a-fA-F]{2})(?<g>[0-9a-fA-F]{2})(?<b>[0-9a-fA-F]{2})$")]
    private static partial Regex LongHexRegex();

    private static bool TryParseColor(string? value, out (byte R, byte G, byte B) rgb)
    {
        rgb = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var v = value.Trim();

        if (ShortHexRegex().Match(v) is { Success: true } s)
        {
            rgb = (
                (byte)(Convert.ToByte(s.Groups["r"].Value, 16) * 17),
                (byte)(Convert.ToByte(s.Groups["g"].Value, 16) * 17),
                (byte)(Convert.ToByte(s.Groups["b"].Value, 16) * 17));
            return true;
        }

        if (LongHexRegex().Match(v) is { Success: true } l)
        {
            rgb = (
                Convert.ToByte(l.Groups["r"].Value, 16),
                Convert.ToByte(l.Groups["g"].Value, 16),
                Convert.ToByte(l.Groups["b"].Value, 16));
            return true;
        }

        return TryParseNamedColor(v, out rgb);
    }

    private static double Luminance((byte R, byte G, byte B) rgb)
    {
        double Channel(byte c) => c / 255.0 <= 0.03928
            ? c / 255.0 / 12.92
            : Math.Pow((c / 255.0 + 0.055) / 1.055, 2.4);

        return 0.2126 * Channel(rgb.R) + 0.7152 * Channel(rgb.G) + 0.0722 * Channel(rgb.B);
    }

    private static string BestAgainstHsl(Hsl candidate, double sourceLuminance)
    {
        // Candidate luminance at various candidate lightnesses; prefer the one
        // with the highest contrast against the source luminance.
        var best = Enumerable
            .Range(1, 9)
            .Select(i => candidate with { L = i * 10 })
            .Concat(new[] { candidate with { L = sourceLuminance > 0.179 ? 6 : 96 } })
            .Select(h => (Hsl: h, Lum: Luminance(HslToRgb(h))))
            .OrderByDescending(p =>
            {
                var la = p.Lum + 0.05;
                var lb = sourceLuminance + 0.05;
                return la > lb ? la / lb : lb / la;
            })
            .ThenByDescending(p => sourceLuminance > 0.179 ? p.Lum : 1 - p.Lum)
            .First();
        return ToHex(HslToRgb(best.Hsl));
    }

    private static string ToHex((byte R, byte G, byte B) rgb)
        => $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}";

    private static (byte R, byte G, byte B) HslToRgb(Hsl hsl)
    {
        var c = (1 - Math.Abs(2 * hsl.L / 100.0 - 1)) * (hsl.S / 100.0);
        var x = c * (1 - Math.Abs((hsl.H / 60.0) % 2 - 1));
        var m = hsl.L / 100.0 - c / 2.0;

        var (r1, g1, b1) = (hsl.H % 360) switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x)
        };

        return (
            (byte)Math.Round((r1 + m) * 255),
            (byte)Math.Round((g1 + m) * 255),
            (byte)Math.Round((b1 + m) * 255));
    }

    private static Hsl RgbToHsl((byte R, byte G, byte B) rgb)
    {
        var r = rgb.R / 255.0;
        var g = rgb.G / 255.0;
        var b = rgb.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2.0;

        if (Math.Abs(max - min) < double.Epsilon)
            return new Hsl(0, 0, l * 100);

        var d = max - min;
        var s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
        double h;
        if (Math.Abs(max - r) < double.Epsilon)
            h = (g - b) / d + (g < b ? 6 : 0);
        else if (Math.Abs(max - g) < double.Epsilon)
            h = (b - r) / d + 2;
        else
            h = (r - g) / d + 4;
        h *= 60;

        return new Hsl(h, s * 100, l * 100);
    }

    private readonly record struct Hsl(double H, double S, double L);

    private static bool TryParseNamedColor(string name, out (byte R, byte G, byte B) rgb)
    {
        rgb = name.ToLowerInvariant() switch
        {
            "black" => (0, 0, 0),
            "white" => (255, 255, 255),
            "red" => (255, 0, 0),
            "lime" => (0, 255, 0),
            "blue" => (0, 0, 255),
            "yellow" => (255, 255, 0),
            "cyan" or "aqua" => (0, 255, 255),
            "magenta" or "fuchsia" => (255, 0, 255),
            "silver" => (192, 192, 192),
            "gray" or "grey" => (128, 128, 128),
            "maroon" => (128, 0, 0),
            "olive" => (128, 128, 0),
            "green" => (0, 128, 0),
            "purple" => (128, 0, 128),
            "teal" => (0, 128, 128),
            "navy" => (0, 0, 128),
            "orange" => (255, 165, 0),
            _ => (255, 255, 255)
        };
        return true;
    }
}
