namespace BlazorAtoms.Clocks;

/// <summary>Which time source an <see cref="AtomClock"/> displays. Overridden when an explicit
/// <c>TimeZone</c> is supplied.</summary>
public enum ClockKind
{
    /// <summary>The host's local timezone (<see cref="System.TimeZoneInfo.Local"/>). On a server-rendered
    /// app this is the <em>server's</em> zone; in WebAssembly it is the browser's.</summary>
    Server,
    /// <summary>The browser's timezone, auto-detected via a tiny self-loaded JS module. Until interop
    /// runs (static SSR / prerender) the clock falls back to UTC, then enhances once interactive.</summary>
    Browser,
    /// <summary>Coordinated Universal Time.</summary>
    Utc,
}
