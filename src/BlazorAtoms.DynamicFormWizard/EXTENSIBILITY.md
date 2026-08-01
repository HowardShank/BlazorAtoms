# BlazorAtoms.DynamicFormWizard — Extensibility Reference

Two seams let a consumer customize rendering without forking the engine. This doc explains when
to reach for which, then gives the full worked example for the type-registry seam. See
`DESIGN-DISCUSSION.md` A.2–A.4 for the decisions behind both, and `FLOW.md` diagram 4 for how they
fit into the render dispatch order.

## The two seams, side by side

| | `FieldTemplate` | Type-to-component registry |
|---|---|---|
| **Scope** | The *entire form* — every field | *One type* — everything else stays on defaults |
| **Shape** | A `RenderFragment`-style parameter the wizard calls per field instead of its own switch | A `Dictionary<Type, Type>` the engine checks before its own built-in switch |
| **Use when** | You want the whole wizard to look like the rest of the Atom family (this is what the future render-adapter package does, wiring in `AtomTextField`/`AtomSelect`/etc.) | 95% of your model is fine with defaults, but one or two property types need specialized handling (a `Money` struct, a `Coordinates` picker, a rich color swatch) |
| **Consumer effort** | Must handle every field type that can appear in the model | Only write a component for the one type you're overriding |
| **Dispatch priority** | Bypasses tiers entirely — replaces the whole per-field render call | Tier 1 of 4 — checked first, falls through to the built-in switch/auto-expand/fallback for every other type |

Reach for the registry first — it's the narrower, lower-effort tool. Reach for `FieldTemplate`
when you're restyling the whole form at once (or building an adapter package that does).

## Worked example: a `Money` custom-type registry entry

The type itself — any plain C# type works, this happens to be a record:

```csharp
public record Money(decimal Amount, string Currency);
```

A plain Blazor component — written by the consumer, **not** part of `BlazorAtoms.DynamicFormWizard`
or any BlazorAtoms package — following the same `Value`/`ValueChanged` contract every built-in
field already uses:

```razor
@* MoneyInput.razor *@
<div class="money-field">
    <span class="money-field__symbol">@CurrencySymbol</span>

    <input type="number"
           step="0.01"
           class="money-field__amount"
           value="@Value.Amount"
           @onchange="OnAmountChanged" />

    <select class="money-field__currency"
            value="@Value.Currency"
            @onchange="OnCurrencyChanged">
        <option value="USD">USD</option>
        <option value="EUR">EUR</option>
        <option value="GBP">GBP</option>
    </select>
</div>

@code {
    [Parameter] public Money Value { get; set; }
    [Parameter] public EventCallback<Money> ValueChanged { get; set; }

    private string CurrencySymbol => Value.Currency switch
    {
        "EUR" => "€",
        "GBP" => "£",
        _ => "$"
    };

    private async Task OnAmountChanged(ChangeEventArgs e)
    {
        if (decimal.TryParse(e.Value?.ToString(), out var amount))
            await ValueChanged.InvokeAsync(Value with { Amount = amount });
    }

    private async Task OnCurrencyChanged(ChangeEventArgs e)
        => await ValueChanged.InvokeAsync(Value with { Currency = e.Value?.ToString() ?? "USD" });
}
```

Bare CSS, themeable like every other BlazorAtoms field — the consumer's own file, not shipped by
this package:

```css
/* MoneyInput.razor.css */
.money-field {
    display: flex;
    align-items: center;
    gap: var(--money-field-gap, 0.5rem);
}
.money-field__amount {
    width: var(--money-amount-width, 8ch);
}
```

Registration — the consumer's page hands the wizard a `Type → component Type` map:

```razor
<DynamicWizard TModel="LoanApplication"
               Model="@application"
               FieldRenderers="@(new() { [typeof(Money)] = typeof(MoneyInput) })" />
```

## What the engine does with it (illustrative)

This is tier 1 of the four-tier dispatch (`FLOW.md` diagram 4) — checked before the built-in
scalar switch, auto-expansion, or the `ToString()` fallback:

```csharp
if (FieldRenderers.TryGetValue(property.PropertyType, out var customComponentType))
{
    // Same OpenComponent pattern the engine already uses for InputText/InputSelect/etc. —
    // one more branch, not new plumbing.
    builder.OpenComponent(0, customComponentType);
    builder.AddAttribute(1, "Value", property.GetValue(Model));
    builder.AddAttribute(2, "ValueChanged", /* same EventCallback wiring as every built-in field */);
    builder.CloseComponent();
    return;
}
```

The registry is `Type → component Type`, not a bespoke `RenderFragment`-returning interface — it
reuses the exact dynamic-component-opening mechanism the engine already needs for its built-in
scalar types. `MoneyInput` is an ordinary component the consumer owns: stylable, testable, and
reusable entirely outside the wizard.

## Validation still applies normally

Because `Money` is just another property on the model, standard or custom `ValidationAttribute`s
still work on it exactly as described in `DESIGN-DISCUSSION.md` D.13 — e.g. a
`[MinimumAmount(0.01)]` custom validator would run through the same `Validator.TryValidateValue`
path as every other field, with zero registry-specific validation code needed.
