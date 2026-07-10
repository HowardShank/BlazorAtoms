using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Clocks;

/// <summary>
/// Base for single-source clocks (<see cref="AtomClock"/> digital, <see cref="AtomAnalogClock"/>
/// face). Adds the "which one zone am I showing" concept — <see cref="Kind"/> / <see cref="TimeZone"/>
/// / <see cref="ResolvedZone"/> / <see cref="CurrentTime"/> — on top of the shared tick +
/// browser-detection plumbing in <see cref="ClockInfraBase"/>. Browser detection only fires for
/// <see cref="ClockKind.Browser"/>; every other mode is JS-free and identical across render modes.
/// </summary>
public abstract class ClockBase : ClockInfraBase
{
    /// <summary>Which time source to show (default <see cref="ClockKind.Server"/>). Ignored when
    /// <see cref="TimeZone"/> is set.</summary>
    [Parameter] public ClockKind Kind { get; set; } = ClockKind.Server;

    /// <summary>Explicit timezone. Overrides <see cref="Kind"/> when non-null.</summary>
    [Parameter] public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>Resolved zone from <see cref="TimeZone"/> / <see cref="Kind"/> (browser falls back
    /// to UTC until JS detection completes).</summary>
    protected TimeZoneInfo ResolvedZone => TimeZone ?? Kind switch
    {
        ClockKind.Utc => TimeZoneInfo.Utc,
        ClockKind.Browser => BrowserZone ?? TimeZoneInfo.Utc,
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
        await base.OnAfterRenderAsync(firstRender);

        // Detect the browser zone whenever we're in Browser mode and haven't resolved it yet — not
        // only on the first render. Kind can flip to Browser at runtime (e.g. a bound dropdown), and
        // that switch happens after the first interactive render; without this the zone stays null
        // and ResolvedZone falls back to UTC. EnsureBrowserZoneAsync is idempotent.
        if (Kind == ClockKind.Browser && TimeZone is null && BrowserZone is null)
            await EnsureBrowserZoneAsync();
    }
}
