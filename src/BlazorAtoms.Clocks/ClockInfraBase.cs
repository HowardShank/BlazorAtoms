using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Clocks;

/// <summary>
/// Shared infrastructure for every time-display component in this library: the once-a-second tick
/// loop and on-demand browser-timezone detection, plus disposal. It carries no notion of a single
/// "current zone" — that lives in <see cref="ClockBase"/>. <see cref="AtomTimeZoneMap"/> inherits
/// this directly because it shows many zones at once.
/// </summary>
/// <remarks>
/// The tick starts in <see cref="OnAfterRenderAsync"/>, which doesn't run during static
/// SSR/prerender — so a non-interactive render shows a single static snapshot and only starts
/// ticking (and detecting the browser zone) once interactive. Subclasses hook the first interactive
/// render via <see cref="OnFirstInteractiveAsync"/>.
/// </remarks>
public abstract class ClockInfraBase : AtomComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Tick once a second (default true). False renders a static snapshot.</summary>
    [Parameter] public bool Live { get; set; } = true;

    private IJSObjectReference? _module;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private bool _ticking;

    /// <summary>The auto-detected browser timezone, once <see cref="EnsureBrowserZoneAsync"/> has
    /// resolved it (null before then). Falls back to UTC when detection yields nothing.</summary>
    protected TimeZoneInfo? BrowserZone { get; private set; }

    /// <summary>Runs on the first interactive render (never during static SSR/prerender). Override
    /// to trigger browser-zone detection when the component actually needs it.</summary>
    protected virtual Task OnFirstInteractiveAsync() => Task.CompletedTask;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await OnFirstInteractiveAsync();

        // Start/stop the tick loop to match Live (which may toggle at runtime).
        if (Live && !_ticking) StartTick();
        else if (!Live && _ticking) StopTick();
    }

    /// <summary>Detect the browser timezone via the tiny self-loaded JS module. Idempotent — the
    /// first success caches <see cref="BrowserZone"/> and later calls are no-ops. Swallows the usual
    /// disconnect/cancel exceptions so it's safe during teardown.</summary>
    protected async Task EnsureBrowserZoneAsync()
    {
        if (BrowserZone is not null) return;
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/BlazorAtoms.Clocks/atom-clocks.js");
            // Fetch primitives (string + int) rather than deserializing an object — object mapping
            // was silently yielding an empty id and collapsing the zone to UTC.
            var id = await _module.InvokeAsync<string?>("timezoneId");
            var offsetMinutes = await _module.InvokeAsync<int>("timezoneOffset");
            BrowserZone = ResolveZone(id, offsetMinutes);
            StateHasChanged();
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
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

    private static TimeZoneInfo ResolveZone(string? id, int offsetMinutes)
    {
        // Prefer the real IANA zone (DST-aware). .NET 6+ resolves IANA ids on every OS via ICU.
        if (!string.IsNullOrEmpty(id))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* unknown id on this host — fall through to the fixed-offset zone below */ }
        }

        // Fall back to a fixed-offset zone built from the reported offset. This is the key fix: even
        // with no/unknown id we honor the browser's actual offset instead of silently using UTC.
        var offset = TimeSpan.FromMinutes(offsetMinutes);
        if (offset == TimeSpan.Zero) return TimeZoneInfo.Utc;
        var label = $"UTC{(offset < TimeSpan.Zero ? "-" : "+")}{offset.Duration():hh\\:mm}";
        return TimeZoneInfo.CreateCustomTimeZone(label, offset, label, label);
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
}
