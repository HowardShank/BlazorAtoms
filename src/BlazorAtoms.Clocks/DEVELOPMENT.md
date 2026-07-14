# BlazorAtoms.Clocks — internals

Notes for anyone editing this library's own source. Not packed into the NuGet readme — see
`README.md` for consumer-facing usage.

## Base-class split: `ClockInfraBase` / `ClockBase`

- **`ClockInfraBase`** (`ClockInfraBase.cs`) owns everything that's shared across *every*
  time-display component: the once-a-second `PeriodicTimer` tick loop, the on-demand
  browser-timezone JS probe, and disposal. It carries no notion of a single "current zone".
  `AtomTimeZoneMap` inherits this directly (it shows 24 bands / N cities at once, not one zone).
  `AtomClockStrip` also inherits it directly for the same reason — the strip owns one shared tick,
  not one per cell (see below).
- **`ClockBase`** (`ClockBase.cs`) adds the "which one zone am I showing" concept — `Kind` /
  `TimeZone` / `ResolvedZone` / `CurrentTime` — on top of `ClockInfraBase`. `AtomClock` and
  `AtomAnalogClock` derive from this.

The split exists so `Live`, the tick loop, and browser-zone detection behave identically across
every component, while only the single-zone components carry the extra `Kind`/`TimeZone` state.

### Render-mode mechanics

The tick starts in `OnAfterRenderAsync`, which doesn't run during static SSR/prerender — so a
non-interactive render shows a single static snapshot and only starts ticking (and detecting the
browser zone) once interactive. Subclasses hook the first interactive render via the `protected
virtual Task OnFirstInteractiveAsync()` override point (e.g. `ClockBase` uses it indirectly by
checking `Kind == ClockKind.Browser` in its own `OnAfterRenderAsync` override; `AtomTimeZoneMap`
uses it to kick off `EnsureBrowserZoneAsync()` when `HighlightViewerZone` is set).

`ClockBase.OnAfterRenderAsync` re-checks `Kind == ClockKind.Browser && TimeZone is null &&
BrowserZone is null` on *every* render, not just the first — `Kind` can flip to `Browser` at
runtime (e.g. a bound dropdown) after the first interactive render, and without this re-check the
zone would stay null and `ResolvedZone` would silently stay pinned to UTC.

## Browser-timezone detection (`atom-clocks.js`)

`ClockInfraBase.EnsureBrowserZoneAsync` lazily imports `./_content/BlazorAtoms.Clocks/atom-clocks.js`
via `IJSRuntime.InvokeAsync<IJSObjectReference>("import", ...)` — no `<script>` tag, no DI
registration, and it only happens for components/modes that actually need the viewer's zone.

The JS module (`wwwroot/atom-clocks.js`) exports two *primitive* functions rather than one
object-shaped one:

- `timezoneId()` → the IANA id from `Intl.DateTimeFormat().resolvedOptions().timeZone`, or `""`.
- `timezoneOffset()` → current UTC offset in minutes (`-Date.getTimezoneOffset()`).

**Gotcha hit while building this:** an earlier version returned a single combined `{ id,
offsetMinutes }` object and deserialized it on the .NET side; that silently yielded an empty id and
collapsed the resolved zone to UTC. Fetching the two primitives separately (`timezoneId()` +
`timezoneOffset()`) fixed it — a `timezone()` function returning the combined shape is kept in the
JS file only for backward compatibility, not used by the C# side.

`ClockInfraBase.ResolveZone(id, offsetMinutes)` prefers `TimeZoneInfo.FindSystemTimeZoneById(id)`
(DST-aware, resolved via ICU on any OS, .NET 6+) and falls back to a fixed-offset
`TimeZoneInfo.CreateCustomTimeZone` built from the reported offset if the id is empty or unknown on
the host — this is the fix that guarantees the browser's actual offset is honored instead of
silently defaulting to UTC when the id can't be resolved.

Detection is idempotent (`BrowserZone is not null` short-circuits) and swallows
`JSDisconnectedException` / `OperationCanceledException` so it's safe to call during teardown or on
a disconnected circuit.

## AtomAnalogClock: SVG structure

The dial is a `viewBox="0 0 100 100"` SVG scaled by `Size`; hands are vertical lines rotated about
the center via transform. Hour numerals are injected as raw SVG through `MarkupString` (Razor
reserves the `<text>` tag name, so numerals can't be authored as plain Razor markup) and carry
inline presentation attributes rather than relying on scoped CSS.

## AtomClockStrip: one shared tick, not N

`AtomClockStrip` inherits `ClockInfraBase` directly (not through `ClockBase`) and renders its
`AtomClock` / `AtomAnalogClock` cells with `Live="false"`. The strip's own tick loop drives a
per-second `StateHasChanged` on the strip, and that single re-render recomputes every cell's
formatted time. N zones therefore cost **one** `PeriodicTimer`, not N — this was a deliberate
perf/allocation trade-off over letting each cell own an independent timer.

## AtomTimeZoneMap: projection & solar geometry

Everything is plain C# geometry rendered into a single inline SVG — no map tiles, no CDN, no
bundled timezone-polygon data:

- **Projection**: equirectangular, `viewBox="0 0 360 180"`. `PX(lon) = lon + 180`,
  `PY(lat) = 90 - lat`. Band centers are `PX(offset * 15)`; the ±12h bands are 15°-wide nominal
  meridian slices (`internal static readonly int[] Offsets = Enumerable.Range(-12, 24)`).
- **Continents**: a compact, low-poly public-domain outline baked in as a single inline `<path>`
  (`WorldMap.Paths.cs`, exposed as `WorldMap.LandPath`) — not fetched, not a raster/tile layer.
- **Day/night terminator**: solar declination is approximated with
  `23.44° * sin(360/365 * (dayOfYear - 81))` (`Declination`), clamped away from exactly 0 so
  `tan(δ)` in the terminator formula can't divide by zero at the equinox. The subsolar longitude
  (`SubsolarLon`) is derived from UTC hour-of-day (`-15° * (hour - 12)`, wrapped to
  [-180, 180)). The terminator polygon (`TerminatorPoints`) walks longitude in 3° steps and solves
  `φ = atan(-cos(lon - subsolarLon) / tan(δ))` for latitude at each step, closing the polygon at
  whichever pole is dark that season (south when δ>0, north when δ<0).
- **City pins**: exact, DST-correct local times via `TimeZoneInfo.ConvertTime`, independent of the
  nominal band geometry above — bands are always approximate whole-hour meridians, pins are always
  exact.

## Deferred / future-work notes

- No bundled IANA polygon/tz-boundary data is used anywhere in this library (by design, to stay
  dependency-free) — the map's bands are nominal meridians, not political borders. If a future
  requirement needs actual tz-boundary shapes, that's a much bigger data-bundling trade-off than
  anything here today.
- The JS module's legacy `timezone()` combined-shape export is unused by the C# side and only kept
  for backward compatibility; it can be removed once nothing external depends on it.
