# BlazorAtoms.Inputs

Form input components for Blazor. `AtomRangeInput` is a labeled slider/range control;
`AtomCrtInput` is a CRT-terminal-styled text input. No JavaScript, no dependencies, works in Server
or WebAssembly and every render mode.

## Install

```
dotnet add package BlazorAtoms.Inputs
```

No setup, no DI registration, no `<script>` tag.

## Usage

```razor
@using BlazorAtoms.Inputs

@* Basic two-way bound slider *@
<AtomRangeInput Label="Label" @bind-Value="count"
                HelpText="@($"The current value is {count}")" />

@* Inside an EditForm, with DataAnnotations validation *@
<EditForm Model="@model">
    <DataAnnotationsValidator />
    <AtomRangeInput Label="Enter Count" @bind-Value="model.Count"
                    ValidationFor="() => model.Count"
                    Min="1" Max="20" Step="1"
                    HelpText="@($"Valid Range: 1 - 20. The current value is {model.Count}")" />
</EditForm>

@* Read-only display, greyed out, no input *@
<AtomRangeInput Label="Progress" Value="42" ReadOnly="true" />

@* Built-in mute/loud icons, auto-placed at the value's min/max ends *@
<AtomRangeInput @bind-Value="volume" Label="Volume" IconPreset="RangeIconPreset.Volume" />

@* Fully custom icons instead *@
<AtomRangeInput @bind-Value="volume" Label="Volume">
    <StartIcon><svg viewBox="0 0 24 24" fill="currentColor"><path d="M4 9v6h4l5 5V4L8 9H4z" /></svg></StartIcon>
    <EndIcon><span style="color:#2563eb">🔊</span></EndIcon>
</AtomRangeInput>

@code {
    int count = 5;
    MyModel model = new();
}
```

## Parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Value` (`@bind-Value`) | `TValue` | — | Current value. `TValue` may be `int`, `long`, `short`, `float`, `double`, `decimal`, or their nullable variants. |
| `ValueChanged` | `EventCallback<TValue>` | — | Only needed directly when not using `@bind-Value`. |
| `ValueExpression` | `Expression<Func<TValue>>` | — | Populated automatically by `@bind-Value`. |
| `Min` | `TValue` | `0` | Minimum value (inclusive), may be negative. Normally less than `Max` (an inverted range is tolerated, not an error). |
| `Max` | `TValue` | `100` | Maximum value (inclusive), may be negative. Normally greater than `Min`. |
| `Step` | `TValue` | `1` | Amount the value changes per tick. May be fractional (e.g. `0.5`) when `TValue` is a floating type. |
| `Label` | `string?` | — | Form label. |
| `LabelCol` | `string` | `clr-col-12 clr-col-md-2` | Responsive classes for the label column. |
| `ControlCol` | `string` | `clr-col-12 clr-col-md-10` | Responsive classes for the control column. |
| `HelpText` | `string?` | — | Shown under the control when there's no validation error. |
| `ValidationFor` | `Expression<Func<TValue>>` | — | Wires this component to an ancestor `EditContext` (e.g. `<EditForm>` + `<DataAnnotationsValidator />`). Falls back to `ValueExpression` when unset, so a plain `@bind-Value` inside an `EditForm` still participates. |
| `Disabled` | `bool` | `false` | Greys out and blocks input. |
| `ReadOnly` | `bool` | `false` | Alias of `Disabled` (a range input has no distinct read-only state). |
| `Visible` | `bool` | `true` | When `false`, hidden via CSS `display:none` (stays in the DOM). |
| `HandleShape` | `HandleShape` | `Round` | `Round`, `Square`, `Heart`, `Star`, `Diamond`, `Triangle`, `Teardrop`, `Gem`, or `Bolt`. |
| `TrackWidth` | `double?` | `200` (CSS default) | Track width in px → `--range-track-width`. |
| `TrackHeight` | `double?` | `6` (CSS default) | Track height in px → `--range-track-height`. |
| `HandleSize` | `double?` | `18` (CSS default) | Handle size in px (w = h) → `--range-handle-size`. |
| `HandleColor` | `string?` | `#ffffff` | Handle fill, independent of the track fill color. Works on all shapes. |
| `OutlineColor` | `string?` | `#2563eb` | Handle outline (border on Round/Square, SVG stroke on glyph shapes), independent of the track fill color. |
| `OutlineWidth` | `double?` | `2` | Handle outline width in px. `0` = no outline. |
| `HandlePosition` | `HandlePosition` | `Center` | Vertical handle position: `Center`, `Above`, or `Below` the track. |
| `HandleOffset` | `double?` | — | Precise vertical handle offset in px (negative = above, positive = below, `0` = centered). Overrides `HandlePosition` when set. |
| `HandleRotation` | `double?` | — | Handle rotation in degrees (clockwise, about center) — e.g. to re-aim a Triangle/Teardrop. |
| `Orientation` | `Orientation` | `Horizontal` | `Horizontal` or `Vertical` (bottom-to-top). |
| `VerticalDirection` | `VerticalDirection` | `BottomToTop` | Which end holds the max value when vertical: `BottomToTop` (max at top) or `TopToBottom` (max at bottom). Ignored when horizontal. |
| `IconPreset` | `RangeIconPreset` | `None` | Built-in icon pair tied to the value's min/max ends: `None`, `Volume` (mute/loud), `Thermostat` (cold/hot), `Brightness` (dim/bright sun), `PlaybackSpeed` (play/fast-forward), `Price` (coin/coin stack), or `Opacity` (hollow/solid circle). |
| `IconPresetReversed` | `bool` | `false` | Swaps which built-in icon represents the min end vs the max end. |
| `StartIcon` | `RenderFragment?` | — | Content at the start of the track. Overrides `IconPreset` for this slot. Own styleable slot. |
| `EndIcon` | `RenderFragment?` | — | Content at the end of the track. Overrides `IconPreset` for this slot. Own styleable slot. |
| `AriaLabel` | `string?` | auto | Accessible label; falls back to `Label`. |

Plus the shared escape hatch on every Atom component: `CssClass`, `Style`, and arbitrary splatted
attributes (`title`, `data-*`, `id`, ARIA, …).

### Disabled, ReadOnly, and Visible

`Disabled` and `ReadOnly` are the same thing — both grey out the control and block input. (A native
`<input type="range">` has no meaningful read-only state of its own, so there's nothing for
ReadOnly to mean beyond Disabled; it's kept as a familiar alias.)

Showing and hiding is a separate axis: `Visible="false"` hides the control via CSS `display:none`
(it stays in the DOM), leaving it visible by default.

### Error state

When wrapped in an `EditForm` with a `ValidationFor` (or a plain `@bind-Value` under an
`EditForm`), a failing validation shows a red stop-sign icon next to the track and swaps the help
text for the first validation message.

### Styling colors

The track shows a two-tone fill (filled portion from `Min` to the value, then the remaining track)
plus a thumb, styled consistently across Chrome/Edge/Safari and Firefox. The handle's own **three
colors are independent of the track fill**: `HandleColor` (fill), `OutlineColor` (border/stroke),
and `OutlineWidth` are component parameters (above). The remaining colors are plain CSS custom
properties, set via `Style`/`CssClass` or your own stylesheet:

| Variable | Default | What it colors |
|---|---|---|
| `--range-fill-color` | `#2563eb` | Filled portion of the track + focus ring (auto-red in the error state). |
| `--range-track-color` | `#d0d5dd` | Unfilled portion of the track. |
| `--range-error-color` | `#dc2626` | Error icon + error subtext (and the handle outline, in the error state). |
| `--range-help-color` | `#6b7280` | Help/subtext. |

### Orientation

`Orientation="Orientation.Vertical"` renders a bottom-to-top bar instead of left-to-right — same
`Min`/`Max`/`Step`/handle/color/icon parameters, just reoriented. `TrackWidth` still means the
length along the track and `TrackHeight` its thickness in both orientations. Set
`VerticalDirection="VerticalDirection.TopToBottom"` to put the max at the bottom instead of the top.

### Start / end icons

Two ways to get icons flanking the track:

- **`IconPreset`** — a named built-in pair (`Volume`: mute/loud, `Thermostat`: cold/hot,
  `Brightness`: dim/bright sun, `PlaybackSpeed`: play/fast-forward, `Price`: coin/coin stack,
  `Opacity`: hollow/solid circle). Each icon
  is tied to the value's **min or max end**, not a literal screen side, so it lands in the correct
  slot automatically as `Orientation`/`VerticalDirection` change (e.g. the "hot" icon always ends up
  at the max end, whichever side that currently is). `IconPresetReversed` swaps which icon
  represents which end.
- **`StartIcon`/`EndIcon`** — free `RenderFragment` slots (put anything in them — SVG, an `<i>`
  icon-font glyph, an emoji, text). Setting either **overrides** `IconPreset` for that specific slot,
  so you can mix a preset on one side with custom content on the other, or go fully custom.

Either way, each icon renders in its own wrapper (`.atom-range-input-icon-start` /
`.atom-range-input-icon-end`) so you can style the two independently. Size them with
`--range-icon-size` (default `1.25em`) and color with `--range-icon-color` (defaults to
`currentColor`); any `<svg>`/`<img>` inside fills the slot.

#### How to add your own icons

1. Set `StartIcon` and/or `EndIcon` — either is a plain `RenderFragment`, so anything you'd put in
   normal markup works. Skip either one to leave that end plain, or to let `IconPreset` fill it.

   ```razor
   @* Inline SVG *@
   <AtomRangeInput @bind-Value="value">
       <StartIcon>
           <svg viewBox="0 0 24 24" fill="currentColor"><path d="M4 9v6h4l5 5V4L8 9H4z" /></svg>
       </StartIcon>
       <EndIcon>
           <svg viewBox="0 0 24 24" fill="currentColor"><path d="M16 8a5 5 0 0 1 0 8" /></svg>
       </EndIcon>
   </AtomRangeInput>

   @* An icon-font glyph (Bootstrap Icons, Font Awesome, Material Symbols, …) *@
   <AtomRangeInput @bind-Value="value">
       <StartIcon><i class="bi bi-volume-mute"></i></StartIcon>
       <EndIcon><i class="bi bi-volume-up"></i></EndIcon>
   </AtomRangeInput>

   @* An <img>, or just an emoji/plain text *@
   <AtomRangeInput @bind-Value="value">
       <StartIcon><img src="mute.svg" alt="" /></StartIcon>
       <EndIcon>🔊</EndIcon>
   </AtomRangeInput>
   ```

2. **Size and color** them without touching the markup, via the two custom properties every icon
   slot reads — set through `Style` (or your own stylesheet):

   ```razor
   <AtomRangeInput @bind-Value="value" Style="--range-icon-size:1.5em; --range-icon-color:#2563eb;">
       ...
   </AtomRangeInput>
   ```

   An `<svg fill="currentColor">`/`<i>` icon-font glyph picks up `--range-icon-color` automatically
   (both read `currentColor`); an `<img>` won't recolor, but still respects `--range-icon-size`.

3. **Style Start vs End differently**, or scope to one `AtomRangeInput` instance, using `CssClass`
   as an ancestor hook in your own stylesheet:

   ```razor
   <AtomRangeInput @bind-Value="value" CssClass="volume-slider">
       <StartIcon>...</StartIcon>
       <EndIcon>...</EndIcon>
   </AtomRangeInput>
   ```
   ```css
   .volume-slider .atom-range-input-icon-start { color: #888; }
   .volume-slider .atom-range-input-icon-end { color: #2563eb; }
   ```

4. **Mixing with `IconPreset`** — set `IconPreset` for one end and only the *other* `StartIcon`/
   `EndIcon`, and the explicit one wins for its slot while the preset still fills the other:

   ```razor
   @* Built-in mute icon at the min end, your own custom icon at the max end *@
   <AtomRangeInput @bind-Value="value" IconPreset="RangeIconPreset.Volume">
       <EndIcon><i class="bi bi-megaphone-fill"></i></EndIcon>
   </AtomRangeInput>
   ```

### Handle colors

The `HandleColor`/`OutlineColor`/`OutlineWidth` parameters also write
`--range-handle-color` / `--range-handle-outline-color` / `--range-handle-outline-width` for the
box-shape thumbs; you can set those custom properties directly instead of the parameters if you
prefer, but the glyph shapes only honor the parameters (their outline is baked into the SVG).

The pixel dimensions (`--range-track-width`, `--range-track-height`, `--range-handle-size`) are set
via the `TrackWidth`/`TrackHeight`/`HandleSize` parameters.

---

## AtomCrtInput

A CRT-terminal-styled text input. Phosphor color, glow, scanlines, monitor bezel, and blinking
caret over a plain native `<textarea>` (or single-line `<input type=text>` when `Multiline="false"`).
Same `EditContext`-aware validation contract as `AtomRangeInput`. No JavaScript.

### Usage

```razor
@using BlazorAtoms.Inputs

@* Default green phosphor terminal *@
<AtomCrtInput Label="Terminal" @bind-Value="text" />

@* Single-line amber, no bezel, custom width/size *@
<AtomCrtInput @bind-Value="cmd"
              Multiline="false"
              Phosphor="CrtPhosphor.Amber"
              Bezel="false"
              Width="360" FontSize="18"
              Placeholder="> _" />

@* Under an EditForm with validation *@
<EditForm Model="@model">
    <DataAnnotationsValidator />
    <AtomCrtInput Label="Notes" @bind-Value="model.Notes"
                  ValidationFor="() => model.Notes" Rows="6" />
</EditForm>
```

### AtomCrtInput parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Value` (`@bind-Value`) | `string?` | — | Current text. |
| `ValueChanged` | `EventCallback<string?>` | — | Only needed directly when not using `@bind-Value`. |
| `ValueExpression` | `Expression<Func<string?>>` | — | Populated by `@bind-Value`. |
| `Label`, `LabelCol`, `ControlCol`, `HelpText`, `Placeholder`, `AriaLabel` | — | — | Same shape as `AtomRangeInput`. |
| `ValidationFor` | `Expression<Func<string?>>` | — | Wires into an ancestor `EditContext`. |
| `Disabled` / `ReadOnly` | `bool` | `false` | Same greyed/blocked state. |
| `Visible` | `bool` | `true` | Hide via `display:none`. |
| `Multiline` | `bool` | `true` | `true` renders `<textarea>`, `false` renders `<input type=text>`. |
| `Rows` | `int` | `4` | Textarea rows (multiline only). |
| `Cols` | `int?` | — | Column hint (ignored when `Width` is set). |
| `Width` | `double?` | — | Explicit width in px → `--crt-width`. |
| `Height` | `double?` | — | Explicit height in px, multiline only → `--crt-height`. |
| `FontSize` | `double?` | — | Font size in px → `--crt-font-size`. |
| `Phosphor` | `CrtPhosphor` | `Green` | Preset color: `Green`, `Amber`, `Blue`, `Red`, or `White`. Overridden by `Color` when set. |
| `Color` | `string?` | — | Explicit text/glow/caret color (any CSS color: hex, rgb, named). |
| `BackgroundColor` | `string?` | — | Explicit screen background color. |
| `Font` | `CrtFont` | `System` | `System` (always works), `Vt323`, or `PressStart2P` — the latter two need the matching `.woff2` bundled (see below). |
| `Glow` | `bool` | `true` | Phosphor glow via `text-shadow`. |
| `Scanlines` | `bool` | `true` | Faint horizontal scanline overlay. |
| `Bezel` | `bool` | `true` | Rounded metallic monitor-bezel frame. |
| `CursorBlink` | `bool` | `true` | Phosphor-colored blinking caret (browser's native blink; `false` hides caret). |

### Bundled CRT fonts

`Font="CrtFont.Vt323"` and `Font="CrtFont.PressStart2P"` reference `.woff2` files that need to be
dropped into `src/BlazorAtoms.Inputs/wwwroot/fonts/` — both are under the SIL Open Font License 1.1
and free to redistribute. See that folder's `README.md` for the specific files (VT323.woff2,
PressStart2P.woff2). If the file isn't present, the browser silently falls back to the system
monospace stack — the component still works, it just doesn't look as authentically CRT. Default
`Font="CrtFont.System"` always works with no bundled files.

### Overriding colors

The phosphor's text color and screen background are custom properties on the root, so consumers
can nudge them via `Style`:

```razor
<AtomCrtInput @bind-Value="text"
              Style="--crt-color:#00ff41; --crt-bg:#000000;" />
```

---

## AtomCrtDisplay

Display-only CRT companion to `AtomCrtInput` — same phosphor / glow / scanlines / bezel / font look,
but there's no editable input. Text can type on with a tunable characters-per-second animation
(no JS — a cancellable C# `Task.Delay` loop drives the visible-character count). Value changes
cancel and restart the animation from the start.

### Usage

```razor
@using BlazorAtoms.Inputs

@* Boot-message reveal at 20 chars/sec *@
<AtomCrtDisplay Value="@bootLog" CharactersPerSecond="20" />

@* Amber terminal, no bezel, wide, looping *@
<AtomCrtDisplay Value="@statusLine"
                Phosphor="CrtPhosphor.Amber" Bezel="false"
                Width="640" FontSize="18"
                Loop="true" LoopDelayMs="2000" />

@* Instant, no animation *@
<AtomCrtDisplay Value="@snapshot" Animate="false" />
```

### AtomCrtDisplay parameters

Shares the entire CRT-chrome surface with `AtomCrtInput` (`Phosphor`, `Color`, `BackgroundColor`,
`Font`, `FontSize`, `Width`, `Height`, `Rows`, `Multiline`, `Glow`, `Scanlines`, `Bezel`,
`CursorBlink`, `Visible`, `Label`, `LabelCol`, `ControlCol`, `HelpText`, `Placeholder`, `AriaLabel`).
Additional parameters that are display-specific:

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Value` | `string?` | — | Text to display (one-way; there is no `ValueChanged`). |
| `Animate` | `bool` | `true` | Types the value one character at a time. `false` shows the full value immediately. |
| `CharactersPerSecond` | `double` | `20` | Typing speed. Clamped to `≥ 0.1`. Value changes restart from the start. Past ~62 cps, characters are revealed in batches — see below. |
| `Loop` | `bool` | `false` | Restarts the animation after finishing (after `LoopDelayMs`). |
| `LoopDelayMs` | `int` | `1500` | Pause between loop iterations when `Loop="true"`. |

The block-cursor after the typed text is the same `CursorBlink` toggle as `AtomCrtInput`; when the
placeholder is showing (no value), the cursor is hidden — it would read strangely hanging off empty
text.

### A note on high typing speeds

The animation is driven by `Task.Delay`, which cannot resolve below the operating system's timer
tick — about **15.6ms on Windows**. A requested 2ms wait sleeps ~16ms regardless.

Up to ~62 cps this is invisible: one character is revealed per wait, and the wait is long enough to
be honored exactly. Above that, the wait is held at the tick and *several* characters are revealed
per step instead (`CharactersPerSecond="500"` → 8 characters every 16ms). The overall rate is
correct, but the reveal is stepped rather than strictly per-character — at those speeds the
difference isn't perceptible anyway.

Practical consequence: very high values still work, but the effective rate is quantized to
multiples of ~62.5 cps, so e.g. `700` and `750` render identically.
