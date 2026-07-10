namespace BlazorAtoms.Clocks;

/// <summary>
/// One labelled timezone in an <see cref="AtomClockStrip"/>. The time is read from
/// <paramref name="TimeZoneId"/> via <see cref="TimeZoneInfo.FindSystemTimeZoneById(string)"/>
/// (DST-aware, no bundled tz data).
/// </summary>
/// <param name="Label">Display caption for the cell, e.g. <c>"Tokyo"</c>.</param>
/// <param name="TimeZoneId">IANA timezone id, e.g. <c>"Asia/Tokyo"</c>.</param>
public sealed record ClockZone(string Label, string TimeZoneId)
{
    /// <summary>A spread of major world cities used when a strip isn't given explicit zones.</summary>
    public static readonly IReadOnlyList<ClockZone> Default = new[]
    {
        new ClockZone("Honolulu", "Pacific/Honolulu"),
        new ClockZone("Los Angeles", "America/Los_Angeles"),
        new ClockZone("New York", "America/New_York"),
        new ClockZone("São Paulo", "America/Sao_Paulo"),
        new ClockZone("London", "Europe/London"),
        new ClockZone("Paris", "Europe/Paris"),
        new ClockZone("Johannesburg", "Africa/Johannesburg"),
        new ClockZone("Dubai", "Asia/Dubai"),
        new ClockZone("Mumbai", "Asia/Kolkata"),
        new ClockZone("Shanghai", "Asia/Shanghai"),
        new ClockZone("Tokyo", "Asia/Tokyo"),
        new ClockZone("Sydney", "Australia/Sydney"),
    };
}
