# BlazorAtoms.DynamicFormWizard

Reflection/attribute-driven multi-step wizard. Decorate a POCO with `[FormStep]`/`[FormOrder]`/
`[DependsOn]`/`[FormPathEnd]` plus standard or custom `ValidationAttribute`s, and `<DynamicWizard>`
generates the entire flow: step navigation with branching and free rejoining, dynamic step
label/position (never a raw step number or a static total), partial per-step validation, file
uploads, dropdowns, and grid layout — with zero hand-written field markup.

**Interactive render modes only.** Reflection-driven rendering, `EditContext`-based validation,
and file upload all require an interactive render mode (`InteractiveServer` or
`InteractiveWebAssembly`) — unlike most BlazorAtoms components, this one does not work under
static SSR.

See `DESIGN-DISCUSSION.md` for the full design log and rationale, `FLOW.md` for diagrams, and
`EXTENSIBILITY.md` for the `FieldTemplate`/type-registry seams.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.DynamicFormWizard\BlazorAtoms.DynamicFormWizard.csproj" />
```
```razor
@using BlazorAtoms.DynamicFormWizard
@using BlazorAtoms.DynamicFormWizard.Attributes
```

## Quick start

```csharp
public enum AccountKind { Personal, Manager }

public class ContactInfo
{
    [Required(ErrorMessage = "Street is required.")]
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class ManagerInfo
{
    [Required(ErrorMessage = "Company name is required.")]
    public string CompanyName { get; set; } = string.Empty;
}

public class AccountSetupModel
{
    [FormStep(1, "Account Type")]
    public AccountKind Kind { get; set; } = AccountKind.Personal;

    // Unconditional -- shown to every path. Also the authoritative end of the Personal path,
    // even though a Manager Details step exists later in this same model.
    [FormStep(2, "Contact Info")]
    [FormPathEnd(nameof(Kind), AccountKind.Personal)]
    public ContactInfo Contact { get; set; } = new();

    // Only visible when Kind == Manager.
    [FormStep(3, "Manager Details")]
    [DependsOn(nameof(Kind), AccountKind.Manager)]
    public ManagerInfo Manager { get; set; } = new();
}
```

```razor
<DynamicWizard TModel="AccountSetupModel" Model="_model" OnWizardComplete="HandleComplete" />

@code {
    private AccountSetupModel _model = new();
    private void HandleComplete(AccountSetupModel model) { /* submit */ }
}
```

`Contact`/`Manager` are complex types — they auto-expand into a field group (their own properties
rendered inline, validated recursively) instead of needing to be flattened onto the top-level
model. See the live playground at `/playground/dynamicformwizard` in any of the demo apps.

## What's driven entirely by attributes

- **`[FormStep(int stepNumber, string? title = null)]`** — assigns a property to a step. The
  number is an internal key only (never shown); the displayed step count/position is always
  recomputed from what's *currently* reachable, so two branches through the same model can show
  different totals ("Step 2 of 3" vs. "Step 3 of 4") without any extra configuration.
- **`[FormOrder(int)]`** — pins render order within a step (reflection property order isn't
  guaranteed stable across an inheritance hierarchy, so this isn't optional bookkeeping).
- **`[DependsOn(nameof(Other), value, operator?)]`** — stackable (AND-combined), hides a property
  unless a sibling top-level property currently satisfies `operator` (default `Equals`) against
  `value`. Also supports `NotEquals`/`GreaterThan`/`GreaterThanOrEqual`/`LessThan`/
  `LessThanOrEqual` — a range condition (e.g. age 18–65) is just two stacked conditions on the same
  property (`GreaterThanOrEqual 18` + `LessThanOrEqual 65`), no separate range construct. A step
  with none of these on any of its properties is visible to every branch — that's how "rejoining"
  after a fork works, no separate merge construct.
- **`[FormPathEnd(nameof(Other), value)]`** — stackable, an *authoritative* end for one branch:
  stops navigation there regardless of what's declared on later steps, even if a later property is
  (perhaps mistakenly) left unconditional. Fewer attributes than gating every later field
  individually, and safe by construction rather than by bookkeeping.
- **`[FormSelect(params string[] options)]`** — a string field renders as a dropdown over a fixed
  list declared right on the model.
- **`[FormDynamicSelect(string providerKey)]`** — a string field's dropdown options are fetched
  through a registered `IWizardLookupService` (register one in DI; without it, the field shows a
  disabled "Loading..." placeholder rather than throwing).
- **`[FormLayout(int span, int totalColumns = 12)]`** — lays a field into a CSS Grid alongside its
  step siblings instead of stacking one-per-row. Bare CSS custom property
  (`--wizard-column-span`), no framework classes.
- Any `ValidationAttribute` (built-in or custom, e.g. a regex or date-range check) just works —
  validation runs through `Validator.TryValidateValue`/`TryValidateObject`, so nothing needs to be
  registered with the engine to add a new rule. This includes cross-property attributes like
  `[Compare(nameof(Other))]` — the engine's own `ValidationContext` already wraps the whole model,
  not just the one value being checked, so `[Compare]`'s reflection lookup of the sibling property
  finds it correctly with no extra wiring. Also proven to work as-is: `[CreditCard]`, `[Phone]`,
  `[Url]`, `[RegularExpression]`, `[StringLength]`, `[Length]`, `[MinLength]`, `[MaxLength]`,
  `[AllowedValues]`, `[DeniedValues]`, `[EnumDataType]`, `[Base64String]`, `[CustomValidation]`.
- **`[DataType(DataType.Password/EmailAddress/PhoneNumber/Url/MultilineText)]`** on a `string`
  property renders a real HTML5 `input type="..."` or a `<textarea>` instead of plain text.
- **`[DisplayFormat(DataFormatString=..., NullDisplayText=...)]`** formats read-only display (the
  fallback for an unhandled type, and any field forced read-only by `[Editable(false)]`) — not
  applied to editable fields, since a display format string isn't an input mask.
- **`[Editable(false)]`** renders a field read-only regardless of its type, overriding even a
  registered custom component.
- **`[ScaffoldColumn(false)]`** excludes a property from the wizard entirely — never rendered,
  never validated, never counted toward a step's visibility.
- File properties of type `IReadOnlyList<WizardFileAttachment>` render as a native file input;
  bytes are copied into memory immediately on selection (not a raw `IBrowserFile` handle, whose
  stream can't be held past the current render). Pair with `[MaxFileCount]`, `[MaxFileSize]`, or
  `[AllowedExtensions]` for constraints.
- **`List<TItem>` properties repeat.** A scalar item type (`List<string>`, `List<int>`, etc.)
  renders a repeatable row of single-value inputs; a complex item type (`List<Beneficiary>`)
  renders one sub-form `<fieldset>` per item — both with Add/Remove. Each complex item validates
  its own `ValidationAttribute`s independently. Pair with `[MinItemCount(n)]`/`[MaxItemCount(n)]`
  to constrain how many items the list may hold. Only `List<T>` itself is supported — not
  `IList<T>`/arrays/other collection types.

  ```csharp
  public class Beneficiary
  {
      [Required(ErrorMessage = "Name is required.")]
      public string Name { get; set; } = string.Empty;
      public int SharePercent { get; set; }
  }

  public class ApplicationModel
  {
      [FormStep(1)]
      public List<string> Tags { get; set; } = new();

      [FormStep(2, "Beneficiaries")]
      [MinItemCount(1)]
      [MaxItemCount(4)]
      public List<Beneficiary> Beneficiaries { get; set; } = new() { new Beneficiary() };
  }
  ```

## Extensibility

- **`FieldTemplate`** — override rendering for the *whole* form at once (e.g. to swap in
  `BlazorAtoms.Inputs`' Atom* components).
- **`FieldRenderers`** — a `Dictionary<Type, Type>` overriding rendering for *one* type only (e.g.
  a `Money` struct rendering as a single currency input instead of auto-expanding). See
  `EXTENSIBILITY.md` for the full worked example.

## Model requirements

`TModel : class, new()`. Only properties with both a getter and a setter participate. Records and
init-only properties are not supported (the engine writes values back via `PropertyInfo.SetValue`).
