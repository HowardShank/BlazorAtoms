using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Clocks;

/// <summary>
/// A live clock for a single time source. <see cref="Kind"/> picks the zone — the server/host zone,
/// UTC, or the auto-detected browser zone — or pass an explicit <see cref="TimeZone"/> to override.
/// Ticks once a second (set <see cref="Live"/> false to freeze). Renders a semantic
/// <c>&lt;time&gt;</c> element formatted by <see cref="Format"/> / <see cref="Culture"/>. Browser
/// detection loads a tiny JS module on demand (only for <see cref="ClockKind.Browser"/>); all other
/// modes are JS-free.
/// </summary>
public partial class AtomClock : AtomComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Which time source to show (default <see cref="ClockKind.Server"/>). Ignored when
    /// <see cref="TimeZone"/> is set.</summary>
    [Parameter] public ClockKind Kind { get; set; } = ClockKind.Server;

    /// <summary>Explicit timezone. Overrides <see cref="Kind"/> when non-null.</summary>
    [Parameter] public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>.NET date/time format string for the displayed text (default <c>"h:mm:ss tt"</c>).</summary>
    [Parameter] public string Format { get; set; } = "h:mm:ss tt";

    /// <summary>Culture used to format the text. Null = <see cref="CultureInfo.CurrentCulture"/>.</summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>Optional caption rendered before the time (e.g. "Server", "Local").</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Tick once a second (default true). False renders a static snapshot.</summary>
    [Parameter] public bool Live { get; set; } = true;

    /// <summary>Clock size in px (drives font-size). Sets <c>--clk-size</c>.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Background override. Sets <c>--clk-bg</c>.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Text color override. Sets <c>--clk-color</c>.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Accessible label for the clock. Falls back to the surrounding text.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private IJSObjectReference? _module;
    private TimeZoneInfo? _browserZone;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private bool _ticking;

    private TimeZoneInfo ResolvedZone => TimeZone ?? Kind switch
    {
        ClockKind.Utc => TimeZoneInfo.Utc,
        ClockKind.Browser => _browserZone ?? TimeZoneInfo.Utc,
        _ => TimeZoneInfo.Local,
    };

    private DateTimeOffset CurrentTime => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ResolvedZone);

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    private string FormatTime(DateTimeOffset t) => t.ToString(Format, EffectiveCulture);

    private static string IsoOf(DateTimeOffset t) =>
        t.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);

    private string KindValue => TimeZone is not null ? "custom" : Kind switch
    {
        ClockKind.Browser => "browser",
        ClockKind.Utc => "utc",
        _ => "server",
    };

    private string RootStyle => string.Concat(
        Size is null ? "" : $"--clk-size:{N(Size.Value)}px;",
        Background is null ? "" : $"--clk-bg:{Background};",
        TextColor is null ? "" : $"--clk-color:{TextColor};");

    private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Browser-zone detection: one JS call on the first interactive render. Never runs during
        // static SSR/prerender (OnAfterRenderAsync doesn't fire there), so the UTC fallback shows.
        if (firstRender && Kind == ClockKind.Browser && TimeZone is null)
        {
            try
            {
                _module ??= await JS.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/BlazorAtoms.Clocks/atom-clocks.js");
                var tz = await _module.InvokeAsync<BrowserTz>("timezone");
                _browserZone = ResolveZone(tz);
                StateHasChanged();
            }
            catch (JSDisconnectedException) { }
            catch (OperationCanceledException) { }
        }

        // Start/stop the tick loop to match Live (which may toggle at runtime).
        if (Live && !_ticking) StartTick();
        else if (!Live && _ticking) StopTick();
    }

    private void StartTick()
    {
        _ticking = true;
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = TickLoop(_timer, _cts.Token);
    }

    private void StopTick()
    {
        _ticking = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _timer?.Dispose();
        _cts = null;
        _timer = null;
    }

    private async Task TickLoop(PeriodicTimer timer, CancellationToken token)
    {
        try
        {
            while (!CancellationToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(token))
            {
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private static TimeZoneInfo ResolveZone(BrowserTz tz)
    {
        if (tz is null || string.IsNullOrEmpty(tz.Id)) return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(tz.Id);
        }
        catch
        {
            // Unknown IANA id on this host — synthesize a fixed-offset zone from the reported offset.
            var offset = TimeSpan.FromMinutes(tz.OffsetMinutes);
            return TimeZoneInfo.CreateCustomTimeZone(tz.Id, offset, tz.Id, tz.Id);
        }
    }

    public async ValueTask DisposeAsync()
    {
        StopTick();
        try
        {
            if (_module is not null) await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        finally
        {
            _module = null;
        }
        GC.SuppressFinalize(this);
    }

    // Shape of the JS timezone() return value. Blazor's JSON interop maps camelCase → these props.
    private sealed record BrowserTz(string Id, int OffsetMinutes);
}
