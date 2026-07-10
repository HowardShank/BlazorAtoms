using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Clocks;

/// <summary>
/// A searchable timezone picker over every zone the runtime knows
/// (<see cref="TimeZoneInfo.GetSystemTimeZones"/> — IANA ids resolved via ICU, no bundled data).
/// Two-way bindable on the selected IANA id (<c>@bind-Value</c>). Filters as you type, shows each
/// zone's current UTC offset, groups the list by region, and can auto-detect the browser's own zone
/// (reusing this library's tiny self-loaded JS probe — the same one <see cref="AtomClock"/> uses).
/// </summary>
public partial class AtomTimeZonePicker : AtomComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Selected IANA timezone id (e.g. <c>"Asia/Tokyo"</c>). Two-way bindable.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Raised when the selection changes (backs <c>@bind-Value</c>).</summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Zones to offer. Null = every system zone.</summary>
    [Parameter] public IEnumerable<TimeZoneInfo>? Zones { get; set; }

    /// <summary>Text shown on the trigger when nothing is selected.</summary>
    [Parameter] public string Placeholder { get; set; } = "Select a timezone…";

    /// <summary>Placeholder inside the filter box.</summary>
    [Parameter] public string SearchPlaceholder { get; set; } = "Search zones…";

    /// <summary>Show each zone's current UTC offset (default true).</summary>
    [Parameter] public bool ShowOffset { get; set; } = true;

    /// <summary>Group the list by region — the IANA id prefix, e.g. "Asia" (default true).</summary>
    [Parameter] public bool ShowRegionGroups { get; set; } = true;

    /// <summary>Offer a "use my timezone" action that auto-detects the browser zone (default true).</summary>
    [Parameter] public bool AllowDetect { get; set; } = true;

    /// <summary>Disable the control.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Accessible label for the control.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Control width in px. Sets <c>--tzp-width</c>.</summary>
    [Parameter] public double? Width { get; set; }

    private IJSObjectReference? _module;
    private bool _open;
    private string _query = "";
    private int _active = -1;
    private DateTimeOffset _nowUtc;

    /// <summary>One display row per zone, with the bits needed for sort / filter / render precomputed.</summary>
    private sealed record Row(string Id, string Region, string City, TimeSpan Offset);

    private IEnumerable<TimeZoneInfo> AllZones => Zones ?? TimeZoneInfo.GetSystemTimeZones();

    /// <summary>Zones matching the current query, ordered for display. Flat list — the keyboard
    /// highlight (<see cref="_active"/>) indexes straight into it; region headers are derived at
    /// render time from consecutive <see cref="Row.Region"/> changes.</summary>
    private List<Row> Filtered()
    {
        _nowUtc = DateTimeOffset.UtcNow;
        var q = _query.Trim();

        var rows = AllZones.Select(z =>
        {
            var id = z.Id;
            var slash = id.IndexOf('/');
            var region = slash < 0 ? "Other" : id[..slash];
            var city = (slash < 0 ? id : id[(slash + 1)..]).Replace('_', ' ').Replace("/", " / ");
            return new Row(id, region, city, z.GetUtcOffset(_nowUtc));
        }).Where(r => Match(r, q));

        var ordered = ShowRegionGroups
            ? rows.OrderBy(r => r.Region, StringComparer.OrdinalIgnoreCase)
                  .ThenBy(r => r.City, StringComparer.OrdinalIgnoreCase)
            : rows.OrderBy(r => r.Offset).ThenBy(r => r.City, StringComparer.OrdinalIgnoreCase);

        return ordered.ToList();
    }

    private static bool Match(Row r, string q)
    {
        if (q.Length == 0) return true;
        return r.Id.Contains(q, StringComparison.OrdinalIgnoreCase)
            || r.City.Contains(q, StringComparison.OrdinalIgnoreCase)
            || r.Region.Contains(q, StringComparison.OrdinalIgnoreCase)
            || OffsetText(r.Offset).Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private static string OffsetText(TimeSpan o)
    {
        var sign = o < TimeSpan.Zero ? "-" : "+";
        return $"UTC{sign}{o.Duration():hh\\:mm}";
    }

    private string SelectedDisplay
    {
        get
        {
            if (string.IsNullOrEmpty(Value)) return Placeholder;
            var pretty = Value.Replace('_', ' ');
            if (!ShowOffset) return pretty;
            try
            {
                var off = TimeZoneInfo.FindSystemTimeZoneById(Value).GetUtcOffset(DateTimeOffset.UtcNow);
                return $"{pretty}  ({OffsetText(off)})";
            }
            catch { return pretty; }
        }
    }

    private string RootStyle =>
        Width is null ? "" : $"--tzp-width:{Width.Value.ToString(CultureInfo.InvariantCulture)}px;";

    private string RootClass =>
        "atom-tz-picker" + (_open ? " is-open" : "") + (Disabled ? " is-disabled" : "");

    private string ValueClass =>
        "tzp-value" + (string.IsNullOrEmpty(Value) ? " is-placeholder" : "");

    private static string OptionClass(bool selected, bool active) =>
        "tzp-option" + (selected ? " is-selected" : "") + (active ? " is-active" : "");

    private void Toggle()
    {
        if (Disabled) return;
        _open = !_open;
        _active = -1;
    }

    private async Task Select(string id)
    {
        _open = false;
        _query = "";
        _active = -1;
        if (id == Value) return;
        Value = id;
        await ValueChanged.InvokeAsync(id);
    }

    private async Task OnKey(KeyboardEventArgs e)
    {
        var rows = Filtered();
        switch (e.Key)
        {
            case "ArrowDown": _active = rows.Count == 0 ? -1 : Math.Min(_active + 1, rows.Count - 1); break;
            case "ArrowUp": _active = rows.Count == 0 ? -1 : Math.Max(_active - 1, 0); break;
            case "Enter":
                if (_active >= 0 && _active < rows.Count) await Select(rows[_active].Id);
                break;
            case "Escape": _open = false; break;
        }
    }

    /// <summary>Auto-detect the browser zone via the shared JS probe and select it. On-demand only —
    /// the module is imported on first use, so a picker that's never "detected" loads no JS.</summary>
    private async Task DetectAsync()
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/BlazorAtoms.Clocks/atom-clocks.js");
            var id = await _module.InvokeAsync<string?>("timezoneId");
            if (string.IsNullOrEmpty(id)) return;
            try { _ = TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { return; } // unknown on this host — leave the selection alone
            await Select(id);
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null) await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        finally { _module = null; }
        GC.SuppressFinalize(this);
    }
}
