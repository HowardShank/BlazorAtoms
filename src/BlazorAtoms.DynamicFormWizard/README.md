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

See `DESIGN-DISCUSSION.md` for the full design log and rationale, `FLOW.md` for diagrams,
`EXTENSIBILITY.md` for the `FieldTemplate`/type-registry seams, and `TASKS.md` for the pending/
deferred-work list mapped to the design sections that explain each one.

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
  guaranteed stable across an inheritance hierarchy, so this isn't optional bookkeeping). Falls
  back to `[Display(Order = N)]` when no `[FormOrder]` is present — stock DataAnnotations already
  has an `Order` field for this. **Prefer `[Display(Order = N)]` for new code** — pairs naturally
  with `[Display(Name = ...)]`/`[Display(Prompt = ...)]`, which this engine already reads for
  label/placeholder. `[FormOrder]` is kept only for existing consumers and may be removed in a
  future major version.
- **`[DependsOn(nameof(Other), value, operator?)]`** — stackable (AND-combined), hides a property
  unless a sibling top-level property currently satisfies `operator` (default `Equals`) against
  `value`. Also supports `NotEquals`/`GreaterThan`/`GreaterThanOrEqual`/`LessThan`/
  `LessThanOrEqual` — a range condition (e.g. age 18–65) is just two stacked conditions on the same
  property (`GreaterThanOrEqual 18` + `LessThanOrEqual 65`), no separate range construct. A step
  with none of these on any of its properties is visible to every branch — that's how "rejoining"
  after a fork works, no separate merge construct. A dotted target string (e.g.
  `[DependsOn("CustomerInfo.Country", "USA")]`) reaches into a nested group's own field, always
  resolved from `Model` — even when declared *on* a member of that same group, the full dotted
  path is still required (a bare `nameof` there would look for a top-level sibling and find none).
  The one exception is a repeating list item's own field depending on a sibling within that *same*
  item — there, a plain `nameof(SiblingProperty)` resolves against the item instance itself, since
  a list row has no static path from `Model` to write a dotted target against (see
  `DESIGN-DISCUSSION.md` section M for the full split).
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
  **Display only — enforces no format.** `DataTypeAttribute` never overrides `IsValid` (stock
  .NET behavior), so `[DataType(DataType.EmailAddress)]` alone lets any string through. Pair it
  with the matching validation attribute for real enforcement:
  `[DataType(DataType.EmailAddress)] [EmailAddress]`, `[DataType(DataType.PhoneNumber)] [Phone]`,
  `[DataType(DataType.Url)] [Url]`. Those already work with zero engine code (same
  `Validator.TryValidateValue` path as every other stock validator above).
- **`[DisplayFormat(DataFormatString=..., NullDisplayText=...)]`** formats read-only display (the
  fallback for an unhandled type, and any field forced read-only by `[Editable(false)]`) — not
  applied to editable fields, since a display format string isn't an input mask.
- **`[Editable(false)]`** renders a field read-only regardless of its type, overriding even a
  registered custom component.
- **`[ScaffoldColumn(false)]`** excludes a property from the wizard entirely — never rendered,
  never validated, never counted toward a step's visibility.
- **`[FormLabel(LabelPosition)]`** / **`DynamicWizard.DefaultLabelPosition`** — where a field's
  label renders. `Above` (default) and `Left` keep a real, visible `<label>` (`Left` just lays it
  out beside the input instead of above it); `Inline`/`Hidden` render no visible label element at
  all — the label text moves onto the input's `placeholder`/`aria-label` instead, so `Hidden`
  never leaves a field without an accessible name. A property's own `[FormLabel]` overrides the
  wizard-level default. Does not reach a nested group member's own fields, a repeating list row,
  or a consumer's `FieldRenderers`-registered component (top-level fields only, same reach as
  `[DependsOn]`).
- **`[Display(Prompt = "...")]`** sets the rendered input's `placeholder` — stock DataAnnotations'
  own placeholder/watermark field, reused rather than inventing a new attribute. Applies
  regardless of `LabelPosition`: a visible label above the field and a placeholder hint inside it
  aren't mutually exclusive. Wins over the `Inline` label-text fallback above, but a consumer's own
  `FieldAttributes["placeholder"]` still wins over `Prompt`.
- **`DynamicWizard.FieldAttributes`** — a `Dictionary<string, IReadOnlyDictionary<string, object>>`
  keyed by top-level property name, splatting arbitrary extra HTML (`data-testid`, `autocomplete`,
  a custom `aria-*`, ...) onto that one field's rendered input. A key you supply yourself
  (`aria-label`, `placeholder`) always wins over one `[FormLabel]` would otherwise synthesize.
  Same top-level-only reach as `[FormLabel]` above, and for the same reason it can't reach a
  `FieldRenderers`-registered component: an arbitrary consumer component has no guaranteed
  attribute to receive it, and adding one it doesn't declare throws at runtime.
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

- **`[FormMatrix(answerProperty, labelProperty)]`** on a `List<TItem>` property renders a
  survey/Likert-style `<table>` instead of the ordinary repeating list — one row per item (its
  `labelProperty` as the row label, instance data rather than `[Display(Name=...)]`) rated against
  a shared column scale (its `answerProperty`, a nullable enum — nullable so an unanswered
  statement isn't recorded as a real answer). `<th scope="col">`/`<th scope="row">` give screen
  readers correct row/column association for free. Validation needs no special handling — it's the
  same `List<TItem>` complex-item shape ordinary repeating lists already validate. An unanswered
  required row gets a visible `wizard-matrix__row--invalid` outline once Submit/Next is blocked
  (never silent), and a row currently required shows a `*` marker next to its label.
- **`[RequiredUnless(nameof(SkipProperty))]`** — the per-*row* counterpart to `[Required]`, for a
  `[FormMatrix]` item type where most statements are mandatory but a specific one (flagged by its
  own `bool` property) is allowed to stay unanswered. Stock `[Required]` can't express this since
  it's type-level, applying uniformly to every row; `[RequiredUnless]` reflects the named `bool`
  sibling off that same item instance instead, so "required" becomes a per-row fact. Works
  identically outside `[FormMatrix]` too — anywhere a sibling flag should conditionally waive a
  required check.
- **`[FormRatingScale(min, max, minLabel, maxLabel)]`** on an `int?` property renders a row of
  numbered points (styled as circles) between two endpoint labels instead of a plain number input
  — e.g. `[FormRatingScale(1, 5, "Not satisfied", "Completely satisfied")]`. Nullable so an unrated
  question isn't silently recorded as a real answer. Pair with `[Required]`/`[Range(min, max)]` for
  enforcement — this attribute only changes rendering, validation already works unchanged.
- **`[FormRadioList]`** on an enum (or nullable-enum) property swaps its default `<select>`
  dropdown for a stacked native radio group. A bare marker, no constructor args. Works via
  Blazor's own `InputRadioGroup<TValue>`/`InputRadio<TValue>`, so it gets the same automatic
  `EditContext`/CSS-invalid-state wiring every other built-in field does.

## Navigation

- **`OnWizardComplete`** — raised once the final step's validation passes and Submit is pressed.
- **`ShowCancelButton`** (default `false`) / **`OnWizardCancel`** — opts a Cancel button into the
  nav row (rendered leftmost, next to Back). Clicking it fires `OnWizardCancel` immediately with no
  validation and no step/state mutation — Cancel abandons the flow rather than completing it, so
  unlike Next/Submit it never blocks on an invalid current step. No built-in confirmation dialog;
  show your own before acting on the callback if you want one.
- **`InitialStep`** / **`CurrentStep`** / **`OnStepChanged`** — draft-save/resume, with no storage
  owned by this engine. `Model` and a step number are both plain JSON-serializable state (including
  any `WizardFileAttachment`, already a `byte[]`) — read `CurrentStep` (via `@ref`) or handle
  `OnStepChanged` (fires after Next/Back actually moves, not on every field edit) to grab a snapshot
  whenever you want to save one yourself (localStorage, an API call, wherever); pass it back in as
  `Model` + `InitialStep` later to resume where you left off. `InitialStep` falls back to the first
  declared step if it isn't a real one for the model's schema.

## Extensibility

- **`FieldTemplate`** — override rendering for the *whole* form at once (e.g. to swap in
  `BlazorAtoms.Inputs`' Atom* components).
- **`FieldRenderers`** — a `Dictionary<Type, Type>` overriding rendering for *one* type only (e.g.
  a `Money` struct rendering as a single currency input instead of auto-expanding). See
  `EXTENSIBILITY.md` for the full worked example.

## Model requirements

`TModel : class, new()`. Only properties with both a getter and a setter participate. Records and
init-only properties are not supported (the engine writes values back via `PropertyInfo.SetValue`).
