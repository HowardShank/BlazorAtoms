# BlazorAtoms.Inputs — development notes

Internal implementation notes for maintainers of `AtomRangeInput`. This is not packed into the
NuGet package; see `README.md` for consumer-facing usage docs.

## Why hand-rolled EditContext instead of InputBase<TValue>

`AtomRangeInput` is the first form/validation-integrated component in BlazorAtoms — every other
"value" component (`AtomRating`, `AtomSignaturePad`, `AtomTimeZonePicker`) uses a plain
`[Parameter] T Value` + `[Parameter] EventCallback<T> ValueChanged` pair with no `EditContext`
awareness at all.

Blazor's own `Microsoft.AspNetCore.Components.Forms.InputBase<TValue>` gets `EditContext`
cascading, `FieldIdentifier`, and field CSS classes for free — but C# only allows single
inheritance, so inheriting it would mean giving up `AtomComponentBase` (and its `CssClass`/`Style`/
`ClassAttr`/`StyleAttr` convention that every other component in this repo uses). Since future
`BlazorAtoms.Inputs` siblings (`AtomTextField`, `AtomCheckbox`, etc., already on the roadmap) will
want the same base, `AtomRangeInput` inherits `AtomComponentBase` and hand-rolls the small amount
of `EditContext` glue itself: a nullable `[CascadingParameter] EditContext?`, a `FieldIdentifier`
computed from `ValidationFor` (falling back to `ValueExpression`), a subscription to
`EditContext.OnValidationStateChanged` (unsubscribed via `IDisposable`), and
`EditContext.NotifyFieldChanged` after every value change.

## TValue conversion strategy

`TValue` spans `int`, `long`, `short`, `float`, `double`, `decimal`, and their nullable variants —
one generic parameter, no constraint (mirrors Blazor's own `InputNumber<TValue>`; nullable value
types don't satisfy `INumber<T>`). `RangeConvert` does the round-trip through `double`, since the
native `<input type="range">` only speaks strings for `value`/`min`/`max`/`step`:

- `TValue -> double` via `Convert.ToDouble`.
- `double -> TValue` via `Convert.ChangeType` against `Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue)`.

`decimal`/`double` precision loss through that round-trip is a non-issue for a UI slider.

## Disabled, ReadOnly, and Visible

An earlier iteration made `Disabled` mean "don't render" and `ReadOnly` mean "render greyed". That
was reworked: the two are now the same greyed-and-blocked state (`IsDisabled => Disabled || ReadOnly`),
and show/hide is a separate `Visible` axis.

Why ReadOnly == Disabled: the native `readonly` attribute is **not supported on
`<input type="range">`** — the HTML spec limits `readonly` to text-like inputs (text, search, url,
email, number, date, …), so a range marked `readonly` stays fully draggable in every browser. The
only native way to make a range non-interactive is `disabled`. So both `ReadOnly` and `Disabled`
map to the native `disabled` attribute plus `data-state="disabled"` (greyscale) — there's no
distinct read-only behavior to offer, and `ReadOnly` is kept only as a familiar alias.

Why `Visible` instead of render/don't-render: hiding via `display:none` (an inline style on the
root, so the element stays in the DOM) avoids tearing the component down and back up — cheaper for
Blazor's diff and it preserves any `EditContext` subscription — and it matches how the rest of the
control is themed (CSS, not conditional markup).

## Styling a native range input cross-browser

The track/thumb are styled entirely with vendor pseudo-elements, no JS. Two gotchas drove the CSS:

- **One rule per vendor.** A combined selector like
  `::-webkit-slider-thumb, ::-moz-range-thumb {…}` is dropped *whole* by any browser that doesn't
  recognize one of the selectors — so WebKit and Firefox each get their own separate rule blocks.
- **The filled portion.** Firefox has a real `::-moz-range-progress` pseudo-element, so its fill is
  free. WebKit/Blink has *nothing* equivalent — the two-tone fill there is a hard-stop
  `linear-gradient` on `::-webkit-slider-runnable-track`, driven by a `--range-fill` percentage.
  That percentage is computed in C# (`FillPercent` = `(value − min) / (max − min)`, clamped, and
  divide-by-zero-safe for an inverted range) and set as an inline custom property on the `<input>`
  each render. Both engines converge on the same look via the shared `--range-fill-color` hook,
  which the error state simply re-points at `--range-error-color`.

The thumb is vertically centered on the thinner track with
`margin-top: calc((track-height − handle-size) / 2)` (WebKit's `runnable-track` needs this;
Firefox centers automatically).

## Handle shapes and the handle's colors

`HandleColor` (fill), `OutlineColor` (border/stroke), and `OutlineWidth` are handle-only parameters,
**independent of the track fill** (`--range-fill-color`). How they're applied depends on the shape:

- `Round` and `Square` are pure CSS (`border-radius`). The parameters are emitted as inline
  `--range-handle-color` / `--range-handle-outline-color` / `--range-handle-outline-width` custom
  properties, which the box-shape thumb CSS reads for `background` and `border`.
- Every other shape (Heart, Star, Diamond, Triangle, Teardrop, Gem, Bolt) is a **baked SVG**: the
  component builds a data-URI `<svg>` with the chosen path plus resolved `fill`/`stroke`/
  `stroke-width` and delivers it through the inherited `--range-handle-glyph` custom property; the
  `[data-handle-glyph]` CSS rule (one per vendor) sets it as the thumb's `background-image`.

Two constraints forced the glyph design:

- **Baking (not a CSS var for the color).** A crisp two-color outline needs a real SVG `fill` +
  `stroke`; an SVG *mask* is monochrome alpha and can't hold two colors. And `background-image`
  can't read CSS custom properties at paint time, so the colors are resolved in C# and written into
  the SVG string. (Earlier this shape used a mask + a single flat color; that's why it changed.)
- **Delivery via a custom property.** The thumb is a pseudo-element; a `background-image` set
  directly on the `<input>` paints the input box, not the thumb. Only inherited custom properties
  reach the pseudo-element, so the baked URL rides in on `--range-handle-glyph`.

Details: the glyph view box is padded to `-3 -3 30 30` (paths are authored for `0 0 24 24`) so the
stroke isn't clipped; `stroke-width` is converted px→view-box-units as `OutlineWidth * 30 / HandleSize`
(approximate — exact px isn't critical on a handle); `OutlineWidth == 0` omits the stroke; and in
the error state the baked stroke uses the error color so a glyph handle turns red with the rest of
the control.

The glyph silhouettes are **copied** from the equivalent shapes in `BlazorAtoms.Ratings`
(`RatingGlyphs`), not referenced — every BlazorAtoms library is standalone with zero cross-library
dependencies, so shared shape data is duplicated by design rather than shared through a common
package. Both sets are authored in the same `0 0 24 24` view box. Fruit/thumb glyphs from Ratings
were left out: they're multi-subpath silhouettes that read as noise at a ~18px handle size.

Adding a shape is one enum value in `HandleShape` + one path entry in `HandleGlyphs` — no markup,
CSS, or playground change (the playground dropdown enumerates the enum).

## Vertical orientation

`Orientation.Vertical` uses the "rotate-in-a-box" technique, not `writing-mode` or the deprecated
WebKit-only `-webkit-appearance: slider-vertical`: the `<input>` stays a perfectly normal
horizontal range — none of its own CSS rules change — it's just repositioned:

- `.atom-range-input-track-box[data-orientation="vertical"]` reserves the **swapped** on-screen
  footprint (`width: max(track-height, handle-size)`, `height: track-width`), `position: relative`.
- `.atom-range-input-track[data-orientation="vertical"]` becomes `position: absolute`, centered in
  that box, and `rotate(-90deg)`. `-90deg` (not `90deg`) puts min at the bottom, max at the top —
  the conventional vertical-slider direction.
- `.atom-range-input-track-wrap[data-orientation="vertical"]` switches to `flex-direction: column`
  so `StartIcon` reads as "top" and `EndIcon` as "bottom", matching their DOM order.

**Why every other feature needed zero C# changes**: `FillPercent`, `HandleOffset`/`HandlePosition`,
`HandleRotation`, and the baked glyph SVGs are all computed and applied to the `<input>` *before*
this rotation happens. A CSS `transform` is purely visual and runs after layout, so it preserves the
geometry as a rigid rotation — the WebKit `linear-gradient(to right, ...)` fill naturally reads as
"grows upward" once rotated, a vertical offset/rotation still displaces the handle perpendicular to
the track (just in a different on-screen direction), etc. Only the icon row's flex direction and the
box/position of the `<input>` itself needed orientation awareness.

`TrackWidth`/`TrackHeight` keep one fixed meaning in both orientations (length along the track /
track thickness) rather than swapping with orientation — one mental model, no surprise reflow when
toggling `Orientation` with the same size params.

## Icon presets are tied to min/max, not to Start/End

`IconPreset`'s two icons (e.g. Volume's mute/loud) are modeled around the **value's ends**, not the
literal `Start`/`End` DOM slots — because the DOM slot each icon lands in has to track
`Orientation`/`VerticalDirection`. `MinIsAtStartSlot` decides that: horizontal has no reverse
concept (`Start`/left is always min), but vertical's `Start` slot always renders visually first
(top), so which value-end that is flips with `VerticalDirection`. `IconPresetReversed` is a second,
independent swap on top of that (min/max icon assignment itself), for callers who want the icon
that "usually" means min to represent max instead.

Explicit `StartIcon`/`EndIcon` still win per-slot over the preset — `StartSlotContent`/
`EndSlotContent` are `StartIcon ?? <preset-derived>` / `EndIcon ?? <preset-derived>`, so setting one
lets you keep the preset on the other side.

The four built-in SVGs are baked as trusted static markup strings (`RenderFragment`s built via
`builder.AddMarkupContent`, same technique as the error icon) rather than authored as separate
`.razor` files — they're small, fixed, and never need per-instance parameterization beyond color
(which flows through `currentColor`). Each carries a `data-icon="…"` attribute purely so tests can
assert which one rendered without string-matching the whole SVG.

`VerticalDirection` (BottomToTop default / TopToBottom) just flips which way the same rotate goes:
`--range-vertical-rotate` is `-90deg` by default and `90deg` for `TopToBottom`, read by the same
`rotate(var(--range-vertical-rotate, -90deg))` transform — one rule, no duplicated CSS. It's
ignored when horizontal (the component only emits the data attribute when vertical). Note this only
flips which end holds the max value on the *track*; the icon slots keep their fixed DOM-order
position (`StartIcon` always renders first/visually top, `EndIcon` last/bottom) regardless of
`VerticalDirection` — the two aren't coupled.

## Handle vertical position

`HandlePosition` (Center/Above/Below) and `HandleOffset` (px) move the handle off the track center.
Both feed one length, `--range-handle-offset` (negative = above, positive = below):

- The enum sets it in CSS via `[data-handle-position="above"|"below"]` rules that `calc` the offset
  from the size vars, so it tracks `HandleSize`/`TrackHeight`.
- `HandleOffset`, when set, is emitted as an **inline** `--range-handle-offset` — an inline custom
  property beats the stylesheet rule, so it overrides the enum (the component also suppresses
  `data-handle-position` in that case).

Per-vendor application differs: WebKit positions the thumb with `margin-top`, so the offset is added
into that calc; Firefox centers the thumb itself, so the offset is a `transform: translateY(...)`
(and the hover-scale rule composes `translateY(...) scale(...)` so the lift survives hover).

`HandleRotation` adds a `--range-handle-rotate` degrees var into the thumb `transform` on both
engines (WebKit's transform is otherwise free since it positions via margin; Firefox composes
`translateY(...) rotate(...)`). Every transform rule — base and hover, both vendors — includes the
rotate term so it survives the hover scale. Rotation is invisible on `Round`; use it to re-aim
`Square`/`Triangle`/`Teardrop`/glyph shapes.

To stop a moved handle from being clipped or overlapping the label/help rows, the component emits
`--range-handle-room` (the absolute offset) and the track wrapper reserves it as `padding-block`;
`overflow: visible` on the input + wrapper lets the raised thumb paint outside the track box.

## Error-state CSS convention

No error-state convention existed anywhere in this repo before this component. Established here:
`aria-invalid="true"` on the `<input>` when in error, `data-state="error"` (or `"readonly"`) on the
component root and the subtext span for CSS hooking.
