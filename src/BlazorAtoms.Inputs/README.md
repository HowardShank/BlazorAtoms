# BlazorAtoms.Inputs

Form input components for Blazor. `AtomRangeInput` is a labeled slider/range control — no
JavaScript, no dependencies, works in Server or WebAssembly and every render mode.

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
| `Min` | `TValue` | — | Minimum value (inclusive). Must be less than `Max`. |
| `Max` | `TValue` | — | Maximum value (inclusive). Must be greater than or equal to `Min`. |
| `Step` | `TValue` | — | Amount the value changes per tick. |
| `Label` | `string?` | — | Form label. |
| `LabelCol` | `string` | `clr-col-12 clr-col-md-2` | Responsive classes for the label column. |
| `ControlCol` | `string` | `clr-col-12 clr-col-md-10` | Responsive classes for the control column. |
| `HelpText` | `string?` | — | Shown under the control when there's no validation error. |
| `ValidationFor` | `Expression<Func<TValue>>` | — | Wires this component to an ancestor `EditContext` (e.g. `<EditForm>` + `<DataAnnotationsValidator />`). Falls back to `ValueExpression` when unset, so a plain `@bind-Value` inside an `EditForm` still participates. |
| `Disabled` | `bool` | `false` | **Renders nothing at all** when `true` — distinct from `ReadOnly`. |
| `ReadOnly` | `bool` | `false` | Still renders, greyed out, blocks input. |
| `HandleShape` | `HandleShape` | `Round` | `Round` or `Square`. |
| `TrackWidth` | `double?` | `200` (CSS default) | Track width in px → `--range-track-width`. |
| `TrackHeight` | `double?` | `6` (CSS default) | Track height in px → `--range-track-height`. |
| `HandleSize` | `double?` | `18` (CSS default) | Handle size in px (w = h) → `--range-handle-size`. |
| `AriaLabel` | `string?` | auto | Accessible label; falls back to `Label`. |

Plus the shared escape hatch on every Atom component: `CssClass`, `Style`, and arbitrary splatted
attributes (`title`, `data-*`, `id`, ARIA, …).

### Disabled vs ReadOnly

These are deliberately different: `Disabled="true"` removes the component from the page entirely
(nothing renders — no label, no track, nothing). `ReadOnly="true"` keeps everything visible but
greyed out, with the slider blocked from interaction.

### Error state

When wrapped in an `EditForm` with a `ValidationFor` (or a plain `@bind-Value` under an
`EditForm`), a failing validation shows a red stop-sign icon next to the track and swaps the help
text for the first validation message.

### Styling colors

`--range-track-color`, `--range-handle-color`, `--range-error-color`, and `--range-help-color` are
plain CSS custom properties (not `StyleVars`-driven) — set them via `Style`/`CssClass` or your own
stylesheet.
