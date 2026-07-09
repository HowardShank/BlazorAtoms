using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Clocks;

/// <summary>
/// Shared plumbing for every clock component: which time source to read, the once-a-second tick
/// loop, and on-demand browser-timezone detection. Concrete clocks (<see cref="AtomClock"/> digital,
/// <see cref="AtomAnalogClock"/> face) inherit this and only add their own parameters + render.
/// Browser detection loads a tiny JS module on the first interactive render (only for
/// <see cref="ClockKind.Browser"/>); during static SSR/prerender the UTC fallback shows, then the
/// component enhances to the real zone and starts ticking once interactive.
/// </summary>
public abstract class ClockBase : AtomComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Which time source to show (default <see cref="ClockKind.Server"/>). Ignored when
    /// <see cref="TimeZone"/> is set.</summary>
    [Parameter] public ClockKind Kind { get; set; } = ClockKind.Server;

    /// <summary>Explicit timezone. Overrides <see cref="Kind"/> when non-null.</summary>
    [Parameter] public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>Tick once a second (default true). False renders a static snapshot.</summary>
    [Parameter] public bool Live { get; set; } = true;

    private IJSObjectReference? _module;
    private TimeZoneInfo? _browserZone;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private bool _ticking;

    /// <summary>Resolved zone from <see cref="TimeZone"/> / <see cref="Kind"/> (browser falls back
    /// to UTC until JS detection completes).</summary>
    protected TimeZoneInfo ResolvedZone => TimeZone ?? Kind switch
    {
        ClockKind.Utc => TimeZoneInfo.Utc,
        ClockKind.Browser => _browserZone ?? TimeZoneInfo.Utc,
        _ => TimeZoneInfo.Local,
    };

    /// <summary>Current time in the resolved zone.</summary>
    protected DateTimeOffset CurrentTime => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ResolvedZone);

    /// <summary>The <c>data-kind</c> attribute value.</summary>
    protected string KindValue => TimeZone is not null ? "custom" : Kind switch
    {
        ClockKind.Browser => "browser",
        ClockKind.Utc => "utc",
        _ => "server",
    };

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
