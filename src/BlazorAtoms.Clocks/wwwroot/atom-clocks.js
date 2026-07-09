// AtomClock — browser timezone detection. Self-loaded (import()) by AtomClock only when
// Kind="Browser"; no <script> tag, no DI. Returns the IANA zone id plus the current UTC offset
// in minutes (positive = ahead of UTC). C# resolves the zone from the id and ticks locally, so
// this runs once per Browser-kind clock, not every second.
export function timezone() {
    return {
        id: Intl.DateTimeFormat().resolvedOptions().timeZone,
        offsetMinutes: -new Date().getTimezoneOffset(),
    };
}
