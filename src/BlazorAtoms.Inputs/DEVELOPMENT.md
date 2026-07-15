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

## Disabled vs ReadOnly

Per the original spec, these are intentionally different, not a naming accident:

- `Disabled="true"` — the whole component renders nothing (`@if (!Disabled)` around the entire
  root in `AtomRangeInput.razor`).
- `ReadOnly="true"` — everything still renders (label, track, help text), just greyed out and
  blocked from input.

One HTML-spec gotcha this ran into: the native `readonly` attribute is **not supported on
`<input type="range">`** — the spec limits `readonly` to text-like inputs (text, search, url,
email, number, date, …). A range input marked `readonly` would still be fully draggable in every
browser. `ReadOnly` is therefore enforced with the native `disabled` attribute on the `<input>`
itself (which *does* block interaction), while the component's own root/label/help-text render
normally — only the literal `<input>` element is "disabled" in HTML terms.

## Error-state CSS convention

No error-state convention existed anywhere in this repo before this component. Established here:
`aria-invalid="true"` on the `<input>` when in error, `data-state="error"` (or `"readonly"`) on the
component root and the subtext span for CSS hooking.
