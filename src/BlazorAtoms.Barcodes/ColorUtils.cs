using System.Globalization;

namespace BlazorAtoms.Barcodes;

/// <summary>Colour helpers for the QR renderer — luminance + contrast for the gradient-decode
/// safety warning, plus a permissive CSS-colour parser (accepts <c>#rgb</c>, <c>#rrggbb</c>,
/// <c>#rrggbbaa</c>, and named shortcuts).</summary>
internal static class ColorUtils
{
    /// <summary>YIQ luminance approximation (0..1). Used for perceived brightness.</summary>
    public static double Luminance(string cssColor)
    {
        if (!TryParseHex(cssColor, out var r, out var g, out var b)) return 0.5;
        return (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;
    }

    /// <summary>Michelson-style luminance contrast in [0..1]. Higher = better readability.</summary>
    public static double Contrast(string a, string b)
    {
        var la = Luminance(a);
        var lb = Luminance(b);
        var lo = System.Math.Min(la, lb);
        var hi = System.Math.Max(la, lb);
        if (hi + lo <= 0) return 0;
        return (hi - lo) / (hi + lo);
    }

    private static bool TryParseHex(string css, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(css)) return false;
        var s = css.Trim();
        if (s.StartsWith("#")) s = s.Substring(1);
        if (s.Length == 3)
        {
            r = int.Parse(new string(s[0], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            g = int.Parse(new string(s[1], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            b = int.Parse(new string(s[2], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return true;
        }
        if (s.Length == 6 || s.Length == 8)
        {
            r = int.Parse(s.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            g = int.Parse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            b = int.Parse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return true;
        }
        return false;
    }
}
