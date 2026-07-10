// AtomClock — browser timezone detection. Self-loaded (import()) only when a component needs the
// browser zone (Kind="Browser", or a map/strip highlighting the viewer). No <script> tag, no DI.
// Two primitive exports (string + number) so the .NET side never depends on object deserialization.

// IANA zone id, e.g. "America/New_York". "" if unavailable.
export function timezoneId() {
    try { return Intl.DateTimeFormat().resolvedOptions().timeZone || ""; }
    catch { return ""; }
}

// Current offset from UTC in minutes, positive = ahead of UTC (e.g. New York in summer = -240).
export function timezoneOffset() {
    return -new Date().getTimezoneOffset();
}

// Legacy combined shape, kept for compatibility.
export function timezone() {
    return { id: timezoneId(), offsetMinutes: timezoneOffset() };
}
