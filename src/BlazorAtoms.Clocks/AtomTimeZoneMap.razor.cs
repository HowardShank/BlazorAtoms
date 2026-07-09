using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Clocks;

/// <summary>
/// A live world timezone map: an equirectangular SVG earth with continents, 24 nominal meridian
/// timezone bands, a day/night terminator, a "now" sun marker, and accurate city pins whose local
/// times come from <see cref="TimeZoneInfo"/> (DST-aware, no bundled tz data). Everything is drawn
/// inline — no map service, no CDN, no raster. Shares the tick + browser-detection plumbing with the
/// clocks via <see cref="ClockInfraBase"/>, so it updates each second unless <see cref="ClockInfraBase.Live"/>
/// is false, and can highlight the viewer's own zone once detected.
/// </summary>
public partial class AtomTimeZoneMap : ClockInfraBase
{
    /// <summary>Rendered width in px (default 640). The map keeps a 2:1 aspect. Sets <c>--tzm-width</c>.</summary>
    [Parameter] public double Width { get; set; } = 640;

    /// <summary>City pins to plot. Null = a built-in spread of major cities.</summary>
    [Parameter] public IReadOnlyList<MapCity>? Cities { get; set; }

    /// <summary>Draw the continent outlines (default true).</summary>
    [Parameter] public bool ShowContinents { get; set; } = true;

    /// <summary>Draw the 24 timezone bands (default true).</summary>
    [Parameter] public bool ShowBands { get; set; } = true;

    /// <summary>Draw the per-band UTC±N + time labels (default true).</summary>
    [Parameter] public bool ShowBandLabels { get; set; } = true;

    /// <summary>Draw city pins (default true).</summary>
    [Parameter] public bool ShowPins { get; set; } = true;

    /// <summary>Draw the per-city name + date/time labels (default true).</summary>
    [Parameter] public bool ShowPinLabels { get; set; } = true;

    /// <summary>Shade the night hemisphere with the day/night terminator (default true).</summary>
    [Parameter] public bool ShowTerminator { get; set; } = true;

    /// <summary>Draw the sun marker at the current subsolar point (default true).</summary>
    [Parameter] public bool ShowSunMarker { get; set; } = true;

    /// <summary>Draw a light lat/long graticule (default false).</summary>
    [Parameter] public bool ShowGraticule { get; set; }

    /// <summary>Detect the browser timezone and highlight its band (default true).</summary>
    [Parameter] public bool HighlightViewerZone { get; set; } = true;

    /// <summary>Make bands and pins clickable (default false).</summary>
    [Parameter] public bool Selectable { get; set; }

    /// <summary>Controlled selected band offset (UTC±N). Null = uncontrolled (tracks clicks).</summary>
    [Parameter] public int? SelectedOffset { get; set; }

    /// <summary>Raised when a band is clicked (offset in whole hours).</summary>
    [Parameter] public EventCallback<int> OnBandSelect { get; set; }

    /// <summary>Raised when a city pin is clicked.</summary>
    [Parameter] public EventCallback<MapCity> OnCitySelect { get; set; }

    /// <summary>Time format for band/pin labels (default <c>"h:mm tt"</c>).</summary>
    [Parameter] public string TimeFormat { get; set; } = "h:mm tt";

    /// <summary>Date format for pin labels (default <c>"MMM d"</c>).</summary>
    [Parameter] public string DateFormat { get; set; } = "MMM d";

    /// <summary>Formatting culture. Null = <see cref="CultureInfo.CurrentCulture"/>.</summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>Color overrides → CSS custom properties on the root.</summary>
    [Parameter] public string? Ocean { get; set; }
    [Parameter] public string? Land { get; set; }
    [Parameter] public string? BandColor { get; set; }
    [Parameter] public string? NightColor { get; set; }
    [Parameter] public string? PinColor { get; set; }
    [Parameter] public string? AccentColor { get; set; }
    [Parameter] public string? HighlightColor { get; set; }
    [Parameter] public string? InkColor { get; set; }

    /// <summary>Accessible label. Falls back to a generic description.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // Snapshot of "now" (UTC) taken at the top of each render so every layer agrees on one instant.
    private DateTimeOffset _nowUtc;
    private int? _internalSelected;

    private CultureInfo Cult => Culture ?? CultureInfo.CurrentCulture;

    protected override async Task OnFirstInteractiveAsync()
    {
        if (HighlightViewerZone) await EnsureBrowserZoneAsync();
    }

    // ---- Projection (equirectangular, matches WorldMap) -------------------------------------
    private static double PX(double lon) => lon + 180;
    private static double PY(double lat) => 90 - lat;

    private string LandPath => WorldMap.LandPath;

    // ---- Timezone bands: 24 nominal whole-hour offsets, -12 .. +11 --------------------------
    internal static readonly int[] Offsets = Enumerable.Range(-12, 24).ToArray();

    private int? EffectiveSelected => SelectedOffset ?? _internalSelected;

    private int? ViewerOffset => BrowserZone is null
        ? null
        : (int)Math.Round(BrowserZone.GetUtcOffset(_nowUtc).TotalHours, MidpointRounding.AwayFromZero);

    private double BandX(int o) => PX(o * 15) - 7.5;
    private double BandCenterX(int o) => Math.Clamp(PX(o * 15), 8, 352);

    private string BandClass(int o)
    {
        var parity = ((o % 2) + 2) % 2;
        var sb = new StringBuilder("tz-band ").Append(parity == 0 ? "even" : "odd");
        if (o == ViewerOffset) sb.Append(" is-viewer");
        if (o == EffectiveSelected) sb.Append(" is-selected");
        return sb.ToString();
    }

    private static string OffsetLabel(int o) => o == 0 ? "UTC" : $"UTC{(o > 0 ? "+" : "")}{o}";
    private DateTimeOffset BandTime(int o) => _nowUtc.ToOffset(TimeSpan.FromHours(o));
    private string BandTimeText(int o) => BandTime(o).ToString(TimeFormat, Cult);
    private string BandTitle(int o) => $"{OffsetLabel(o)} — {BandTimeText(o)}";

    // ---- City pins --------------------------------------------------------------------------
    private IReadOnlyList<MapCity> EffectiveCities => Cities ?? WorldMap.DefaultCities;

    private DateTimeOffset CityTime(MapCity c)
    {
        try { return TimeZoneInfo.ConvertTime(_nowUtc, TimeZoneInfo.FindSystemTimeZoneById(c.TimeZoneId)); }
        catch { return _nowUtc; }
    }

    private string PinTitle(MapCity c)
    {
        var t = CityTime(c);
        return $"{c.Name} — {t.ToString(DateFormat, Cult)} {t.ToString(TimeFormat, Cult)}";
    }

    // ---- Solar geometry (day/night terminator + sun marker) ---------------------------------
    private double Declination
    {
        get
        {
            var n = _nowUtc.DayOfYear;
            var d = 23.44 * Math.Sin(Deg2Rad(360.0 / 365.0 * (n - 81)));
            // Keep away from exactly 0 (equinox) so tan(δ) can't divide-by-zero in the terminator.
            if (Math.Abs(d) < 0.25) d = d < 0 ? -0.25 : 0.25;
            return d;
        }
    }

    private double SubsolarLon
    {
        get
        {
            var h = _nowUtc.Hour + _nowUtc.Minute / 60.0;
            var l = -15.0 * (h - 12.0);
            return ((l + 180) % 360 + 360) % 360 - 180; // wrap to [-180, 180)
        }
    }

    private double SunX => PX(SubsolarLon);
    private double SunY => PY(Declination);

    // Night polygon: the terminator curve across all longitudes, closed off to the dark pole.
    private string TerminatorPoints
    {
        get
        {
            var d = Declination;
            var ls = SubsolarLon;
            var sb = new StringBuilder();
            for (var lon = -180.0; lon <= 180.0; lon += 3.0)
            {
                var phi = Rad2Deg(Math.Atan(-Math.Cos(Deg2Rad(lon - ls)) / Math.Tan(Deg2Rad(d))));
                sb.Append(N(PX(lon))).Append(',').Append(N(PY(phi))).Append(' ');
            }
            // δ>0 → northern summer → the SOUTH pole is dark (close to y=180); δ<0 → close to y=0.
            var yClose = d > 0 ? 180.0 : 0.0;
            sb.Append(N(360)).Append(',').Append(N(yClose)).Append(' ');
            sb.Append(N(0)).Append(',').Append(N(yClose));
            return sb.ToString();
        }
    }

    // ---- Graticule --------------------------------------------------------------------------
    internal static readonly int[] Meridians = { -150, -120, -90, -60, -30, 0, 30, 60, 90, 120, 150 };
    internal static readonly int[] Parallels = { -60, -30, 0, 30, 60 };

    // ---- Labels (built as raw SVG because Razor reserves the <text> tag) ---------------------
    private MarkupString LabelsMarkup
    {
        get
        {
            var sb = new StringBuilder();
            if (ShowBandLabels)
            {
                foreach (var o in Offsets)
                {
                    var cx = BandCenterX(o);
                    Text(sb, cx, 7, 3, 700, "var(--tzm-ink,#e6edf3)", OffsetLabel(o));
                    Text(sb, cx, 11.5, 2.7, 500, "var(--tzm-ink,#e6edf3)", BandTimeText(o));
                }
            }
            if (ShowPinLabels)
            {
                foreach (var c in EffectiveCities)
                {
                    var x = PX(c.Lon);
                    var y = PY(c.Lat);
                    var t = CityTime(c);
                    Text(sb, x, y - 3, 3, 700, "var(--tzm-ink,#e6edf3)", c.Name);
                    Text(sb, x, y + 5, 2.7, 500, "var(--tzm-ink,#e6edf3)", t.ToString(TimeFormat, Cult));
                }
            }
            return (MarkupString)sb.ToString();
        }
    }

    private static void Text(StringBuilder sb, double x, double y, double size, int weight, string fill, string content) =>
        sb.Append("<text x=\"").Append(N(x)).Append("\" y=\"").Append(N(y))
          .Append("\" text-anchor=\"middle\" style=\"fill:").Append(fill)
          .Append(";font-size:").Append(N(size)).Append("px;font-weight:").Append(weight)
          .Append("\">").Append(Esc(content)).Append("</text>");

    // ---- Root style / a11y ------------------------------------------------------------------
    private string RootStyle => string.Concat(
        $"--tzm-width:{N(Width)}px;",
        Ocean is null ? "" : $"--tzm-ocean:{Ocean};",
        Land is null ? "" : $"--tzm-land:{Land};",
        BandColor is null ? "" : $"--tzm-band:{BandColor};",
        NightColor is null ? "" : $"--tzm-night:{NightColor};",
        PinColor is null ? "" : $"--tzm-pin:{PinColor};",
        AccentColor is null ? "" : $"--tzm-accent:{AccentColor};",
        HighlightColor is null ? "" : $"--tzm-highlight:{HighlightColor};",
        InkColor is null ? "" : $"--tzm-ink:{InkColor};");

    private string EffectiveAriaLabel =>
        AriaLabel ?? "World timezone map showing current time across zones";

    // ---- Interaction ------------------------------------------------------------------------
    private async Task SelectBand(int o)
    {
        if (!Selectable) return;
        _internalSelected = o;
        if (OnBandSelect.HasDelegate) await OnBandSelect.InvokeAsync(o);
    }

    private async Task SelectCity(MapCity c)
    {
        if (!Selectable) return;
        if (OnCitySelect.HasDelegate) await OnCitySelect.InvokeAsync(c);
    }

    // ---- Helpers ----------------------------------------------------------------------------
    private static double Deg2Rad(double d) => d * Math.PI / 180.0;
    private static double Rad2Deg(double r) => r * 180.0 / Math.PI;
    private static string N(double v) => Math.Round(v, 2).ToString(CultureInfo.InvariantCulture);

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
