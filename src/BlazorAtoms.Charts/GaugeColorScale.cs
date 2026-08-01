using System.Globalization;

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

    private static (double R, double G, double B) HexToRgb(string hex)
    {
        var h = hex.TrimStart('#');
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
