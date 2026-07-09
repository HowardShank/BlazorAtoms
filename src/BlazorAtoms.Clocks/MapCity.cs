namespace BlazorAtoms.Clocks;

/// <summary>
/// A labelled point on <see cref="AtomTimeZoneMap"/>: a city at a geographic coordinate whose local
/// time is read from its IANA timezone (via <see cref="TimeZoneInfo.FindSystemTimeZoneById(string)"/>,
/// DST-aware, no bundled data).
/// </summary>
/// <param name="Name">Display name shown by the pin.</param>
/// <param name="Lon">Longitude in degrees, −180 (west) … +180 (east).</param>
/// <param name="Lat">Latitude in degrees, −90 (south) … +90 (north).</param>
/// <param name="TimeZoneId">IANA timezone id, e.g. <c>"America/New_York"</c>.</param>
public sealed record MapCity(string Name, double Lon, double Lat, string TimeZoneId);
