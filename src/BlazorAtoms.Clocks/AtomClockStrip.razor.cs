using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Clocks;

/// <summary>
/// A world-clock strip: one clock per timezone, laid out as a wrapping row, a responsive grid, or a
/// vertical list. Each cell is an <see cref="AtomClock"/> (digital) or <see cref="AtomAnalogClock"/>
/// (dial) per <see cref="Face"/>, showing that zone's current time from <see cref="TimeZoneInfo"/>.
/// The whole strip ticks off a single timer (from <see cref="ClockInfraBase"/>): cells render with
/// <c>Live="false"</c> and recompute each time the strip re-renders, so N zones cost one
/// <see cref="PeriodicTimer"/>, not N. Can highlight the viewer's own zone, show each zone's offset
/// relative to a reference, sort by offset, and raise a selection event.
/// </summary>
public partial class AtomClockStrip : ClockInfraBase
{
    /// <summary>Zones to show. Null = <see cref="ClockZone.Default"/>.</summary>
    [Parameter] public IReadOnlyList<ClockZone>? Zones { get; set; }

    /// <summary>Digital (default) or analog cells.</summary>
    [Parameter] public ClockFace Face { get; set; } = ClockFace.Digital;

    /// <summary>Row (wrapping, default), Grid, or Stacked.</summary>
    [Parameter] public ClockStripLayout Layout { get; set; } = ClockStripLayout.Row;

    /// <summary>Time format for digital cells (default <c>"h:mm:ss tt"</c>).</summary>
    [Parameter] public string Format { get; set; } = "h:mm:ss tt";

    /// <summary>Formatting culture. Null = <see cref="CultureInfo.CurrentCulture"/>.</summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>Per-cell size in px. Null = a sensible default per face (analog 120, digital 20).</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Analog cells: draw the second hand (default true). Ignored for digital.</summary>
    [Parameter] public bool ShowSeconds { get; set; } = true;

    /// <summary>Analog cells: draw the minute ticks (default true). Ignored for digital.</summary>
    [Parameter] public bool ShowMinuteTicks { get; set; } = true;

    /// <summary>Analog cells: draw the hour numerals (default false). Ignored for digital.</summary>
    [Parameter] public bool ShowNumerals { get; set; }

    /// <summary>Detect the browser zone and highlight its cell (default true).</summary>
    [Parameter] public bool HighlightViewerZone { get; set; } = true;

    /// <summary>Show each zone's offset relative to <see cref="ReferenceTimeZoneId"/> (default false).</summary>
    [Parameter] public bool ShowRelativeOffset { get; set; }

    /// <summary>Reference zone for the relative offset. Null = the viewer's zone, else UTC.</summary>
    [Parameter] public string? ReferenceTimeZoneId { get; set; }

    /// <summary>Order cells by current UTC offset, west → east (default false).</summary>
    [Parameter] public bool SortByOffset { get; set; }

    /// <summary>Make cells clickable (default false).</summary>
    [Parameter] public bool Selectable { get; set; }

    /// <summary>Controlled selected zone id. Null = uncontrolled (tracks clicks).</summary>
    [Parameter] public string? SelectedTimeZoneId { get; set; }

    /// <summary>Raised when a cell is clicked.</summary>
    [Parameter] public EventCallback<ClockZone> OnSelect { get; set; }

    /// <summary>Gap between cells in px. Sets <c>--cstrip-gap</c>.</summary>
    [Parameter] public double? Gap { get; set; }

    /// <summary>Highlight color for the viewer/selected cell. Sets <c>--cstrip-highlight</c>.</summary>
    [Parameter] public string? HighlightColor { get; set; }

    /// <summary>Accessible label for the strip.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private DateTimeOffset _nowUtc;
    private string? _internalSelected;
    private readonly Dictionary<string, TimeZoneInfo> _zoneCache = new();

    protected override async Task OnFirstInteractiveAsync()
    {
        if (HighlightViewerZone) await EnsureBrowserZoneAsync();
    }

    private IReadOnlyList<ClockZone> EffectiveZones => Zones ?? ClockZone.Default;

    private IEnumerable<ClockZone> OrderedZones =>
        SortByOffset
            ? EffectiveZones.OrderBy(z => Resolve(z).GetUtcOffset(_nowUtc))
            : EffectiveZones;

    private TimeZoneInfo Resolve(ClockZone z)
    {
        if (_zoneCache.TryGetValue(z.TimeZoneId, out var tz)) return tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById(z.TimeZoneId); }
        catch { tz = TimeZoneInfo.Utc; }
        _zoneCache[z.TimeZoneId] = tz;
        return tz;
    }

    private double ResolvedSize => Size ?? (Face == ClockFace.Analog ? 120 : 20);

    private string LayoutValue => Layout switch
    {
        ClockStripLayout.Grid => "grid",
        ClockStripLayout.Stacked => "stacked",
        _ => "row",
    };

    private string FaceValue => Face == ClockFace.Analog ? "analog" : "digital";

    private string? SelectedId => SelectedTimeZoneId ?? _internalSelected;

    private string CellClass(ClockZone z)
    {
        var cls = "";
        if (HighlightViewerZone && BrowserZone is not null && z.TimeZoneId == BrowserZone.Id) cls += " is-viewer";
        if (z.TimeZoneId == SelectedId) cls += " is-selected";
        return cls;
    }

    private TimeSpan RefOffset
    {
        get
        {
            var id = ReferenceTimeZoneId ?? BrowserZone?.Id;
            if (id is null) return TimeSpan.Zero; // UTC
            try { return TimeZoneInfo.FindSystemTimeZoneById(id).GetUtcOffset(_nowUtc); }
            catch { return TimeSpan.Zero; }
        }
    }

    private string RelText(ClockZone z)
    {
        var d = Resolve(z).GetUtcOffset(_nowUtc) - RefOffset;
        if (d == TimeSpan.Zero) return "±0";
        var sign = d < TimeSpan.Zero ? "-" : "+";
        var a = d.Duration();
        var body = a.Minutes == 0 ? $"{(int)a.TotalHours}h" : $"{(int)a.TotalHours}:{a.Minutes:D2}h";
        return sign + body;
    }

    private string RootStyle => string.Concat(
        Gap is null ? "" : $"--cstrip-gap:{N(Gap.Value)}px;",
        HighlightColor is null ? "" : $"--cstrip-highlight:{HighlightColor};");

    private string EffectiveAriaLabel => AriaLabel ?? "World clocks";

    private async Task Select(ClockZone z)
    {
        if (!Selectable) return;
        _internalSelected = z.TimeZoneId;
        if (OnSelect.HasDelegate) await OnSelect.InvokeAsync(z);
    }

    private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);
}
