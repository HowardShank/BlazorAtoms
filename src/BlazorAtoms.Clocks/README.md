# BlazorAtoms.Clocks

Live time-display components for Blazor — one library, six components:

- **`AtomClock`** — a ticking digital clock for a single time source: the **server/host** zone,
  **UTC**, or the **auto-detected browser** zone (or any explicit `TimeZoneInfo`). Configurable
  format/culture, optional label, opt-out ticking.
- **`AtomAnalogClock`** — the same time sources on a scalable **SVG dial** with hour / minute /
  (optional) second hands, optional minute ticks and numerals.
- **`AtomClockPair`** — two `AtomClock`s together (by default **server + local**), **side-by-side**
  or **stacked**.
- **`AtomClockStrip`** — a **world-clock strip**: N zones as a wrapping row / grid / list, each a
  digital or analog cell. Highlights your zone, shows relative offsets, sorts by offset, selectable.
- **`AtomTimeZoneMap`** — a **world timezone map**: an inline-SVG earth with continents, 24 nominal
  `UTC±N` bands, a live day/night terminator + sun marker, and accurate city pins. No map service,
  no CDN, no raster.
- **`AtomTimeZonePicker`** — a **searchable timezone picker** over *every* system zone: type to
  filter, grouped by region, current UTC offset per zone, and a "use my zone" auto-detect.
  Two-way bound on the IANA id (`@bind-Value`).

All four share their tick + browser-timezone plumbing (`ClockInfraBase`); the single-zone clocks
add `Kind` / `TimeZone` / `CurrentTime` on top (`ClockBase`), so `Live` behaves identically across
every component.

Renders a semantic `<time datetime="…">` element. Ticks once a second via a C# `PeriodicTimer`.
Browser-timezone detection uses a tiny **self-loaded JS module** — no `<script>` tag, no DI, and
only for `Kind="Browser"`; every other mode is JS-free. Server or WebAssembly.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Clocks\BlazorAtoms.Clocks.csproj" />
```
```razor
@using BlazorAtoms.Clocks
```
Link `{App}.styles.css` (scoped-CSS bundle), as with any RCL.

## AtomClock

```razor
@* Server time, live *@
<AtomClock Label="Server" />

@* Browser-local time, 24-hour, no seconds *@
<AtomClock Kind="ClockKind.Browser" Label="Local" Format="HH:mm" />

@* A fixed zone, frozen snapshot *@
<AtomClock TimeZone="TimeZoneInfo.FindSystemTimeZoneById(\"Asia/Tokyo\")"
           Label="Tokyo" Live="false" Format="ddd h:mm tt" />
```

| Parameter | Type | Notes |
|-----------|------|-------|
| `Kind` | `ClockKind` | `Server` (default) / `Browser` / `Utc`. Ignored when `TimeZone` set. |
| `TimeZone` | `TimeZoneInfo?` | Explicit zone; overrides `Kind`. |
| `Format` | `string` | .NET format string (default `"h:mm:ss tt"`). |
| `Culture` | `CultureInfo?` | Formatting culture (default `CurrentCulture`). |
| `Label` | `string?` | Caption before the time. |
| `Live` | `bool` | Tick every second (default true); false = snapshot. |
| `Size` | `double?` | px → font-size (`--clk-size`). |
| `Background` / `TextColor` | `string?` | `--clk-bg` / `--clk-color`. |

## AtomAnalogClock

```razor
@* Server time, analog face, live *@
<AtomAnalogClock Label="Server" />

@* Browser-local, larger face with numerals, no second hand *@
<AtomAnalogClock Kind="ClockKind.Browser" Label="Local"
                 Size="220" ShowNumerals="true" ShowSeconds="false" />
```

| Parameter | Type | Notes |
|-----------|------|-------|
| `Kind` / `TimeZone` / `Live` | — | Same as `AtomClock` (shared via `ClockBase`). |
| `Size` | `double` | Face diameter px (default 160; `--aclk-size`). |
| `ShowSeconds` | `bool` | Draw the second hand (default true). |
| `ShowMinuteTicks` | `bool` | Draw the 60 minute ticks (default true; hour ticks always show). |
| `ShowNumerals` | `bool` | Draw 1–12 numerals (default false). |
| `Label` | `string?` | Caption under the dial. |
| `FaceColor` / `HandColor` / `AccentColor` | `string?` | `--aclk-face` / `--aclk-hand` / `--aclk-accent` (second hand + cap). |

The dial is a `viewBox="0 0 100 100"` SVG scaled by `Size`; hands are vertical lines rotated about
the center. Hour numerals are injected as raw SVG (Razor reserves the `<text>` tag) and carry inline
presentation attributes.

## AtomClockPair

```razor
@* Server + local, side-by-side *@
<AtomClockPair />

@* UTC over local, stacked *@
<AtomClockPair Layout="ClockLayout.Stacked"
               PrimaryKind="ClockKind.Utc" PrimaryLabel="UTC"
               SecondaryKind="ClockKind.Browser" SecondaryLabel="You" />
```

`Layout`: `SideBySide` (default) / `Stacked`. Shared `Format` / `Live` / `Size` flow to both sides;
`PrimaryKind`/`PrimaryLabel`/`PrimaryTimeZone` and the `Secondary*` trio configure each side. `Gap`
(px) sets the spacing (`--clkp-gap`).

## AtomClockStrip

A row/grid/list of clocks — the N-zone case of `AtomClockPair`. Each cell reuses `AtomClock` or
`AtomAnalogClock`.

```razor
@* Default world cities, digital, wrapping row *@
<AtomClockStrip />

@* Custom zones, analog, sorted, with offsets relative to London *@
<AtomClockStrip Face="ClockFace.Analog" Layout="ClockStripLayout.Grid"
                Zones="MyZones" SortByOffset="true"
                ShowRelativeOffset="true" ReferenceTimeZoneId="Europe/London" />
```

| Parameter | Type | Notes |
|-----------|------|-------|
| `Zones` | `IReadOnlyList<ClockZone>?` | Null = built-in spread of major cities. |
| `Face` | `ClockFace` | `Digital` (default) / `Analog`. |
| `Layout` | `ClockStripLayout` | `Row` (default) / `Grid` / `Stacked`. |
| `Format` / `Culture` | — | Passed to digital cells. |
| `Size` | `double?` | Per-cell px; null → face default (analog 120, digital 20). |
| `ShowSeconds` / `ShowMinuteTicks` / `ShowNumerals` | `bool` | Analog cells only (passed to `AtomAnalogClock`); ignored for digital. |
| `HighlightViewerZone` | `bool` | Detect + highlight the browser's zone (default true). |
| `ShowRelativeOffset` | `bool` | Show each zone's offset vs `ReferenceTimeZoneId`. |
| `ReferenceTimeZoneId` | `string?` | Reference for offsets; null = viewer zone, else UTC. |
| `SortByOffset` | `bool` | Order cells west→east by current UTC offset. |
| `Selectable` / `SelectedTimeZoneId` / `OnSelect` | `bool` / `string?` / `EventCallback<ClockZone>` | Click a cell. |
| `Gap` / `HighlightColor` | `double?` / `string?` | `--cstrip-gap` / `--cstrip-highlight`. |

`public sealed record ClockZone(string Label, string TimeZoneId);` — `ClockZone.Default` is the
built-in set.

**One timer for the whole strip.** Cells render with `Live="false"`; the strip owns the single tick
and its per-second re-render refreshes every cell. N zones cost **one** `PeriodicTimer`, not N.

## AtomTimeZoneMap

A whole-earth timezone map, drawn **entirely inline** — no map tiles, no CDN, no raster, no bundled
timezone-polygon data. It combines a nominal band ruler (approximate, longitude-based) with accurate
per-city times from `TimeZoneInfo` (DST-aware; the tz database already in the .NET runtime).

```razor
@* Full live map: continents, bands, terminator, sun, default city pins, viewer highlight *@
<AtomTimeZoneMap />

@* Wider, click-selectable, own city list, no graticule *@
<AtomTimeZoneMap Width="900" Selectable="true"
                 Cities="MyCities" OnCitySelect="c => _picked = c.Name" />
```

| Parameter | Type | Notes |
|-----------|------|-------|
| `Live` | `bool` | Tick every second (default true; from `ClockInfraBase`). |
| `Width` | `double` | Rendered px width, 2:1 aspect (default 640; `--tzm-width`). |
| `Cities` | `IReadOnlyList<MapCity>?` | Pins to plot. Null = built-in spread of ~13 major cities. |
| `ShowContinents` / `ShowBands` / `ShowBandLabels` | `bool` | Layer toggles (all default true). |
| `ShowPins` / `ShowPinLabels` | `bool` | City markers + their labels (default true). |
| `ShowTerminator` / `ShowSunMarker` | `bool` | Day/night shading + subsolar marker (default true). |
| `ShowGraticule` | `bool` | Light lat/long grid (default false). |
| `HighlightViewerZone` | `bool` | Detect the browser zone and highlight its band (default true). |
| `Selectable` | `bool` | Make bands/pins clickable (default false). |
| `SelectedOffset` | `int?` | Controlled selected band (UTC±N). |
| `OnBandSelect` / `OnCitySelect` | `EventCallback<int>` / `EventCallback<MapCity>` | Click events. |
| `TimeFormat` / `DateFormat` / `Culture` | — | Label formatting. |
| `Ocean` / `Land` / `BandColor` / `NightColor` / `PinColor` / `AccentColor` / `HighlightColor` / `InkColor` | `string?` | `--tzm-*` color overrides. |

`public sealed record MapCity(string Name, double Lon, double Lat, string TimeZoneId);`

**How it stays dependency-free.** The map is an equirectangular SVG (`viewBox="0 0 360 180"`,
`X = lon+180`, `Y = 90-lat`). Bands, graticule, the day/night terminator (a solar-declination sine
curve) and the sun marker are all plain C# geometry. Continents are a compact, low-poly public-domain
outline baked in as an inline `<path>`. City times are `TimeZoneInfo.ConvertTime(...)`. The **only**
JS is the shared browser-timezone probe, and only when `HighlightViewerZone` is on — during static
SSR/prerender the map renders correctly without it and simply skips the highlight until interactive.
Approximate by design: the bands are nominal whole-hour meridians, not political tz borders — the
city pins carry the exact, DST-correct local times.

## AtomTimeZonePicker

A searchable combobox over **every** zone the runtime knows
(`TimeZoneInfo.GetSystemTimeZones()` — IANA ids resolved via ICU, no bundled data). Two-way bound on
the selected IANA id, so it drops straight into `AtomClock`'s `TimeZone` / the strip's
`ReferenceTimeZoneId`.

```razor
@* Bound to an id; feed the pick to a clock *@
<AtomTimeZonePicker @bind-Value="_zone" />
@if (_zone is not null)
{
    <AtomClock TimeZone="TimeZoneInfo.FindSystemTimeZoneById(_zone)" Label="@_zone" />
}
@code { private string? _zone; }
```

| Parameter | Type | Notes |
|-----------|------|-------|
| `Value` / `ValueChanged` | `string?` | Selected IANA id — `@bind-Value`. |
| `Zones` | `IEnumerable<TimeZoneInfo>?` | Zones to offer. Null = every system zone. |
| `ShowOffset` | `bool` | Show each zone's current (DST-aware) UTC offset (default true). |
| `ShowRegionGroups` | `bool` | Group the list by region — the id prefix, e.g. "Asia" (default true). |
| `AllowDetect` | `bool` | Offer a "use my timezone" auto-detect button (default true). |
| `Placeholder` / `SearchPlaceholder` | `string` | Trigger / filter-box placeholder text. |
| `Disabled` | `bool` | Disable the control. |
| `Width` | `double?` | Control width px (`--tzp-width`). |
| `AriaLabel` | `string?` | Accessible label. |

Keyboard: type to filter, `↑`/`↓` to move the highlight, `Enter` to pick, `Esc` to close. Styling
uses CSS **system colors** (`Field`/`Canvas`/`AccentColor`…) so it's theme-correct in light and dark
out of the box; override via the `--tzp-*` tokens. The auto-detect reuses this library's shared
`atom-clocks.js` probe and is loaded **only** when the detect button is clicked — an untouched picker
loads no JS.

## Render modes & the browser zone

`Kind="Browser"` needs the browser's timezone, which only JS can report. During static SSR /
prerender the browser clock renders **UTC**; on the first interactive render it loads
`atom-clocks.js`, resolves the real zone, and re-renders. `Server`/`Utc`/explicit `TimeZone` clocks
are identical in every render mode. In WebAssembly, `Server` already *is* the browser zone
(`TimeZoneInfo.Local`), so `Browser` and `Server` coincide.

Ticking is disabled implicitly during non-interactive renders (the timer starts in
`OnAfterRenderAsync`, which doesn't run there) — a static snapshot is shown. No JS is loaded unless
`Kind="Browser"` is actually used.
