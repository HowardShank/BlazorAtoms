# BlazorAtoms.Clocks

Live time-display components for Blazor — one library, three components:

- **`AtomClock`** — a ticking digital clock for a single time source: the **server/host** zone,
  **UTC**, or the **auto-detected browser** zone (or any explicit `TimeZoneInfo`). Configurable
  format/culture, optional label, opt-out ticking.
- **`AtomAnalogClock`** — the same time sources on a scalable **SVG dial** with hour / minute /
  (optional) second hands, optional minute ticks and numerals.
- **`AtomClockPair`** — two `AtomClock`s together (by default **server + local**), **side-by-side**
  or **stacked**.

`AtomClock` and `AtomAnalogClock` share their time-source, tick, and browser-timezone plumbing
(`ClockBase`), so `Kind` / `TimeZone` / `Live` behave identically across both.

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

## Render modes & the browser zone

`Kind="Browser"` needs the browser's timezone, which only JS can report. During static SSR /
prerender the browser clock renders **UTC**; on the first interactive render it loads
`atom-clocks.js`, resolves the real zone, and re-renders. `Server`/`Utc`/explicit `TimeZone` clocks
are identical in every render mode. In WebAssembly, `Server` already *is* the browser zone
(`TimeZoneInfo.Local`), so `Browser` and `Server` coincide.

Ticking is disabled implicitly during non-interactive renders (the timer starts in
`OnAfterRenderAsync`, which doesn't run there) — a static snapshot is shown. No JS is loaded unless
`Kind="Browser"` is actually used.
