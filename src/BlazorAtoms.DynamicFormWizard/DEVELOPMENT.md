# BlazorAtoms.DynamicFormWizard — Development Notes

Internal architecture notes for maintainers. See `README.md` for consumer-facing usage,
`DESIGN-DISCUSSION.md` for the full decision log and rationale behind everything below, `FLOW.md`
for diagrams, and `EXTENSIBILITY.md` for the two override seams.

## File layout

```
Attributes/     FormStep, FormOrder, DependsOn, ComparisonOperator, FormPathEnd, FormSelect,
                FormDynamicSelect, FormLayout
Validators/     FormRegex, DateRange, MaxFileCount, MaxFileSize, AllowedExtensions,
                MinItemCount, MaxItemCount
Services/       IWizardLookupService (FormDynamicSelect's DI contract)
Schema/         WizardPropertySchema, WizardStepSchema, WizardModelSchema (reflection, cached per Type),
                WizardTypeInspection (shared complex-vs-scalar-vs-List<T> tests)
Navigation/     WizardNavigator -- headless step/DependsOn engine, no Razor dependency
Files/          WizardFileAttachment
Rendering/      WizardFieldContext, WizardFieldCssClassProvider, WizardParsableInput,
                WizardNullableParsableInput
DynamicWizard.razor(.cs)     component shell: EditContext, nav chrome, accessibility
DynamicWizard.Fields.cs      the 5-tier field-render dispatch (scalar/nullable/group/fallback)
DynamicWizard.Lists.cs       tier 1b: List<T> repeating groups (see its own section below)
DynamicWizard.Selects.cs     FormSelect/FormDynamicSelect rendering + lookup-service fetch
```

## Why three separate reflection layers, not one

`WizardModelSchema` (Schema/) reflects a `TModel` **exactly once**, cached in a static
`ConcurrentDictionary<Type, WizardModelSchema>` — every `Ideas.md` iteration re-reflected
(`GetProperties`, `GetCustomAttribute`) on every render, including every keystroke. `WizardNavigator`
(Navigation/) is the stateful engine over one *model instance* — current step, visibility
evaluation, partial validation — built without any Razor/rendering dependency on purpose, so it's
directly unit-testable (see `tests/.../WizardNavigatorTests.cs`) without ever rendering a
component. `DynamicWizard<TModel>` wraps one `WizardNavigator` per instance and owns the parts that
genuinely need to be a component: `EditContext`, focus management, the render dispatch.

## The 5-tier field-render dispatch (`RenderDispatched` in `DynamicWizard.Fields.cs`)

Ahead of every tier below: `[Editable(false)]` short-circuits straight to a read-only render
(`RenderReadOnlyField`) — checked first, before even tier 1, since an explicit "don't let this be
edited" outranks any renderer's opinion about the type. See the `[DataType]`/`[DisplayFormat]`/
`[Editable]`/`[ScaffoldColumn]` section below for the full story.

1. Consumer `FieldRenderers` registry match (exact type, `Nullable<T>` unwrapped first) — always
   wins.
1b. `List<TItem>` (exactly that closed generic, see `WizardTypeInspection.TryGetListItemType`) —
   routed to `DynamicWizard.Lists.cs`, its own section below.
2. Built-in scalar types, nullable-aware (bool/enum/DateTime·DateOnly·TimeOnly/int·long·short·
   float·decimal·double/string/file natively; everything else implementing `IParsable<T>` — byte,
   sbyte, ushort, uint, ulong, char, nint, nuint, Guid, TimeSpan, DateTimeOffset, or a consumer's
   own custom struct — via `WizardParsableInput<T>`/`WizardNullableParsableInput<T>`).
3. Auto-expand: a complex type's own public read/write, non-indexer properties become a field
   group, validated recursively via `Validator.TryValidateObject` (as opposed to
   `TryValidateValue` for leaves). Collections are explicitly excluded here (see
   `WizardTypeInspection.IsComplexType`'s doc comment) so a `List<T>` never gets misdetected as a
   group via its `Capacity` property — tier 1b handles it instead.
4. Fallback: read-only, `[DisplayFormat]`-aware display (`FormatDisplayValue`; plain `ToString()`
   absent a `[DisplayFormat]`) + a `Debug.WriteLine` warning — an unhandled type never silently
   disappears from the form.

`WizardTypeInspection.IsComplexType`/`TryGetListItemType` are the shared predicates deciding tiers
1b/3 vs. everything else, used identically by rendering (this dispatch) *and* validation
(`WizardNavigator.ValidateCurrentStep`) — they must agree, or a property could render one way and
validate another.

## `FieldTarget` and why nested rendering reuses the same dispatch

`FieldTarget` (a `readonly struct` in `DynamicWizard.Fields.cs`) wraps `(object Owner, PropertyInfo
Info, string Label)` behind `GetValue()`/`SetValue(value)`/`BuildValueExpression()` — the object
that actually owns a value and the reflected property that reads/writes it. For a normal field,
`Owner` is the top-level `Model`; when `RenderExpandedGroup` recurses into a complex property's own
properties (tier 3), or `RenderComplexItemRepeater` (tier 1b) recurses into one list item's own
properties, it constructs a new `FieldTarget` with `Owner` set to the *nested instance*. This is
what lets the same `RenderDispatched` method handle top-level fields, nested-group fields, and
complex-list-item fields without duplicating the type-switch three times.

`FieldTarget` is deliberately **property-only** — it has no "list index" form. One was tried and
abandoned; see the `List<T>` section below for why, and don't reintroduce it without reading that
first.

## `[DataType]`/`[DisplayFormat]`/`[Editable]`/`[ScaffoldColumn]` rendering support (DESIGN-DISCUSSION.md H.30)

Four stock DataAnnotations attributes that affect *rendering* (not validation — that's D.12/D.13
and already worked with zero engine changes; see H.29):

- **`[DataType(DataType.Password/EmailAddress/PhoneNumber/Url/MultilineText)]`** on a `string`
  property is read in `TryRenderBuiltInScalar`'s string branch (`target.Info.GetCustomAttribute<DataTypeAttribute>()`)
  and maps to a real HTML5 `input type="..."` (`StringInputHtmlTypes` dictionary) or a `<textarea>`
  for `MultilineText`, via `RenderTypedTextInput`/`RenderTextArea`. These are raw, manually-bound
  elements, **not** `InputText` with an extra attribute — `InputText.BuildRenderTree` writes its own
  `type="text"` *after* `AdditionalAttributes`, so any `"type"` passed through `AdditionalAttributes`
  is silently overwritten back to `"text"`. Don't try that shortcut again; it was checked and
  doesn't work. Other `DataType` members (`Currency`, `PostalCode`, `CreditCard`, ...) still render
  as plain text — no obvious single-input mapping, left for a real need.
- **`[DisplayFormat(DataFormatString=..., NullDisplayText=...)]`** is read at the two read-only
  render sites (`RenderFallback` for tier 4, `RenderReadOnlyField` for `[Editable(false)]`) and
  applied via the shared static `FormatDisplayValue(value, format)` helper. **Never** applied to an
  editable tier — `DataFormatString` is a one-way display format (`string.Format`), and no built-in
  `Input*` component parses a formatted string back out of its bound value on every keystroke, so
  doing that would break round-tripping.
- **`[Editable(false)]`** is checked first thing in `RenderDispatched`, ahead of tier 1's
  `FieldRenderers` registry lookup — an explicit "don't edit this" beats even a consumer's own
  custom component. Routes to `RenderReadOnlyField` (a `span.wizard-field--readonly`, formatted the
  same way tier 4's fallback is).
- **`[ScaffoldColumn(false)]`** excludes a property before it ever becomes a render/validation
  target — filtered out at all three places that enumerate a type's properties:
  `WizardModelSchema.Build` (top-level), `RenderExpandedGroup` (a nested group), and
  `RenderComplexItemRepeater` (a repeating list's item type). Add a fourth filter site if a future
  change adds a fourth place that enumerates properties — the existing three would silently miss
  it, same failure mode `IsComplexType`/`TryGetListItemType` warn about at the top of this file.

**Caching note:** all four are read directly off the reflected `PropertyInfo`
(`target.Info.GetCustomAttribute<T>()`) at the point of render, the same non-cached pattern
`RenderExpandedGroup` already uses for a nested group member's `[Display(Name=...)]` label — not
threaded through `WizardPropertySchema`'s cache (F.19), because that cache only ever covered
top-level model properties, and `target.Info` is available uniformly at every nesting depth
(top-level, nested group, list item) whereas a `WizardPropertySchema` lookup by name is not. This
mirrors an existing perf trade-off in this codebase, not a new one introduced here.

**Known gap:** a scalar list row's `FieldTarget.Info` is `ListItemBox<TItem>.Value` (see the
`List<T>` section below), not the original `List<T>` property's own `PropertyInfo` — so
`[DataType]`/`[Editable]` declared on a `List<string>` property does not reach each repeated row.
Complex list items are unaffected (their fields are ordinary property-owned targets on the real
item instance, which does carry its own attributes correctly).

## `RenderTreeBuilder` sequence discipline

Every render helper that opens more than one element/component wraps its own body in
`builder.OpenRegion(N)`/`CloseRegion()` before using local `0`-based sequence numbers internally —
the exact fix `Ideas.md` iteration 6 landed on after hitting Blazor's sequence-diffing requirements
the hard way (dynamic sequence numbers inside a loop corrupt diffing). Two disciplines to keep when
touching this code:
- A region opened at the **top** of a `RenderFragment` delegate (e.g. `RenderField`) can use a
  fixed constant (`OpenRegion(0)`) regardless of which property is being rendered, because each
  such delegate invocation gets its own independent numbering scope.
- A region opened **partway through** an existing element tree (e.g. inside
  `RenderExpandedGroup`'s per-nested-field loop, or before recursing back into `RenderDispatched`)
  must use the **next unused sequence number in that scope**, not `0` — reusing an already-consumed
  number silently corrupts the outer diffing sequence. `RenderExpandedGroup` is the canonical
  example: `div`/`label` consume sequences 0-3, so the recursive `RenderDispatched` call is wrapped
  in `OpenRegion(4)`, not `OpenRegion(0)`.

## `List<T>` repeating support (`DynamicWizard.Lists.cs`) — read this before touching it

Tier 1b handles exactly `List<TItem>` (see `WizardTypeInspection.TryGetListItemType` — deliberately
not `IList<T>`/`ICollection<T>`/arrays, to keep the mutation model simple: `IList.Add`/`RemoveAt`
against the concrete `List<T>` instance just works). Two shapes depending on `TItem`:

- **Complex `TItem`** (`RenderComplexItemRepeater`): one `<fieldset>` per item, all stacked in the
  one step — not paginated one-item-per-screen (that would need a wizard-within-a-wizard
  navigation concept that doesn't exist and wasn't worth inventing). Each item's fields are
  ordinary property-owned `FieldTarget`s with `Owner` = the item instance itself. That instance
  *is* the real list element (not a copy, not a wrapper) and is reference-stable across renders,
  so it gets full, independent `Validator.TryValidateObject` validation exactly like an existing
  nested group — `WizardNavigator.ValidateCurrentStep` has a dedicated branch for this (search for
  `TryGetListItemType` there) that validates each item and stores errors against the item instance,
  matching the `FieldIdentifier` its own rendered fields already resolve to.
- **Scalar `TItem`** (`RenderScalarItemRepeater`): a repeating row of single-value inputs, each
  reusing tier 2/2b's dispatch unchanged.

### Why scalar items need `ListItemBox<TItem>`, not a raw list index

The obvious design — a `FieldTarget` variant holding `(IList, int index)` and building a
`ValueExpression` as an `IndexExpression` (`list[i]`) — was built first and **fails at runtime**:
`FieldIdentifier.Create` throws `ArgumentException`, "FieldIdentifier only supports simple member
accessors (fields, properties) of an object." There is no supported way to make an indexer
expression satisfy `InputBase<TValue>.ValueExpression`.

The fix: `ListItemBox<TItem>` (top-level type in `DynamicWizard.Lists.cs`, **not** nested inside
`DynamicWizard<TModel>` — see the CLR landmine below) wraps one `list[index]` slot behind a single
`Value` property. `Value`'s getter/setter read/write the real list slot, but `Value` *itself* is a
simple property access, so `FieldTarget(box, valueProperty, label)` — the same property-only
`FieldTarget` every other field uses — works unmodified.

Boxes are cached per `(list, index)` in `_listItemBoxes` (a `Dictionary` field on
`DynamicWizard<TModel>`) and reused across renders. This isn't just an optimization:
`EditContext` tracks modified/invalid state by `FieldIdentifier` equality, which compares the
*owner object*. A fresh box every render would be a different owner each time, so a field just
marked invalid/modified would silently forget that state on the very next render. `RemoveAt`/`Add`
call `InvalidateListItemBoxes(list)` to evict every cached box for that list, since indices shift
meaning on a structural change — the item now at index 2 is not the item that used to be there, so
inheriting its stale validation state would be actively wrong, not just unnecessary.

### CLR landmine: don't nest `ListItemBox<TItem>` inside `DynamicWizard<TModel>`

This was tried first and produced a reproducible, non-obvious failure with **no exception at the
call site**: `typeof(ListItemBox<>).MakeGenericType(itemType)` — called on the *nested* form of the
type, with the result immediately discarded — corrupted `ElementReference` state used *later in
the same render* by `DynamicWizard.OnAfterRenderAsync`'s step-heading focus call, throwing
`InvalidOperationException: "ElementReference has not been configured correctly"` from a
completely unrelated line. Confirmed by bisection: removed every other suspect (the render loop,
`RenderDispatched`, the Add/Remove buttons, even an empty `OpenRegion`/`CloseRegion` pair) one at a
time; the *one* line that reproduced it in isolation was that single `MakeGenericType` call on the
nested generic. Closing a **non-nested** generic the identical way (`typeof(List<>).MakeGenericType
(itemType)`) never reproduced it.

The underlying CLR/JIT mechanism was never root-caused — moving `ListItemBox<TItem>` to a
top-level type sidesteps it entirely, which was cheaper and more reliable than depending on
understanding it. If you're tempted to nest a generic helper type inside `DynamicWizard<TModel>`
for tidiness, don't, or budget time to rediscover this.

## `ComparisonOperator` and `DependsOn` (`Navigation/WizardNavigator.cs`)

`DependsOnAttribute` gained an optional `ComparisonOperator` parameter (`Equals` default, backward
compatible with every existing two-argument usage): `NotEquals`, `GreaterThan`,
`GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`. `WizardNavigator`'s private `Matches` helper
(used by both `IsVisible` and, with `Equals` hardcoded, `IsPathEndMarked`) is the single place that
evaluates a condition — ordering operators cast the target's actual value to `IComparable` and
throw a clear `InvalidOperationException` naming the type if it isn't one, rather than failing with
a confusing `InvalidCastException` deep in `CompareTo`.

Combining stays AND-only; **no OR was added**. A range condition (age 18–65) is two stacked
`[DependsOn]` attributes on the same property (`GreaterThanOrEqual 18` + `LessThanOrEqual 65`) —
the existing "all stacked conditions must match" rule already expresses it, so no new combinator
was needed. If a future scenario genuinely needs OR, it's a bigger change (grouped AND/OR logic,
not a flat list) — don't bolt it onto the existing flat `Dependencies` list without redesigning the
evaluation shape.

## Typed `EventCallback` construction across a reflected type

Native `InputBase<TValue>`-derived components (and any consumer component registered via
`FieldRenderers`) declare `ValueChanged` as `EventCallback<TValue>` for their own concrete
`TValue` — not `EventCallback<object?>`. Since the property's runtime type is only known via
reflection, `CreateTypedValueChanged` builds the callback through a generic helper method
(`CreateTypedCallbackGeneric<TValue>`) invoked via `MethodInfo.MakeGenericMethod(valueType)` —
this produces a genuinely boxed `EventCallback<TValue>` whose runtime type matches the target
parameter exactly, which is what makes reflection-based `AddAttribute` assignment succeed. A plain
`EventCallback<object?>` would fail to bind to a strongly-typed parameter.

Building the matching `ValueExpression` (`Expression<Func<TValue>>`, required by every
`InputBase<TValue>` internally for its own `FieldIdentifier`) doesn't need the same generic-method
trick: `Expression.Lambda(Expression body)` (the *non-generic* overload) infers the delegate type
from the body automatically, and the object it returns is actually already the concrete
`Expression<Func<TValue>>` at runtime (just statically typed as the base `LambdaExpression` by the
method signature) — reflection-based property assignment checks the runtime type, so this already
works without an explicit generic instantiation. See `BuildValueExpression`.

## `FormSelect`/`FormDynamicSelect` scope

Both are schema-level metadata captured only for **top-level** properties (`WizardModelSchema`
reflects them the same way as everything else) — not evaluated for properties discovered while
auto-expanding a nested group (tier 3's recursion walks raw `PropertyInfo`, not
`WizardPropertySchema`). This mirrors `DependsOn`'s own top-level-only reach (see
DESIGN-DISCUSSION.md B.6) — extending either to nested groups is a possible future enhancement, not
attempted in v1.

`IWizardLookupService` is resolved lazily via `IServiceProvider.GetService(Type)` in
`OnParametersSetAsync`, not a required `[Inject]` — a model that never uses
`[FormDynamicSelect]` must not force every consumer to register the service just to use the wizard
at all. Fetches are cached per provider key for the component's lifetime (idempotent — a key
already fetched is never re-requested), covering every step's provider keys up front so navigating
to a later step never stalls mid-fetch.

## Testing patterns specific to this package

- `WizardNavigatorTests.cs` exercises the engine directly — no `Render<T>`, no bUnit — by
  constructing a `WizardNavigator` over a plain POCO and asserting `CurrentStep`/
  `EffectiveStepNumbers()`/`DisplayPosition()`/`ValidateCurrentStep()` directly. This is
  deliberate: the navigation/branching/validation logic should be provably correct independent of
  whether rendering works.
- `DynamicWizardTests.cs` (bUnit) exercises the actual component: field dispatch tiers, nested
  auto-expand + independent nested-object mutation, `FormSelect`/`FormDynamicSelect` (including
  registering a fake `IWizardLookupService` via bUnit's `Services`), file upload via bUnit's
  `InputFileContent`/`UploadFiles`, and the `FormLayout` inline style.
- Custom `ValidationAttribute` subclasses are tested by calling `GetValidationResult(value,
  context)` directly (the public method `ValidationAttribute` exposes) rather than routing through
  a full model + `Validator.TryValidateValue` — simpler and doesn't need a real `EditContext`.
- `ComparisonOperator` is tested via `[Theory]`/`[InlineData]` over `WizardNavigator.IsVisible`
  directly (age boundary values for the range-via-stacking case, plus a dedicated test that an
  ordering operator against a non-`IComparable` target throws instead of misbehaving silently).
- `List<T>` repeating support has full round-trip coverage in `DynamicWizardTests.cs`: render (no
  fallback span, correct row/fieldset count), edit-a-specific-row, Add, Remove, and — critically —
  per-item validation isolation (editing item 1's field doesn't affect item 0's stored value or
  validation state). If you change anything in `DynamicWizard.Lists.cs`, re-run these first; the
  `ElementReference` landmine above was caught by exactly this kind of full-render test, not a
  narrower unit test.
- `[Compare]` and every other stock `ValidationAttribute` proven-not-assumed to work (H.29) are in
  `WizardNavigatorTests.cs`, each with its own tiny single-property model rather than one shared
  model with many properties on one step — `ValidateCurrentStep` validates every *visible* property
  of a step together, so a shared model's other default-valued properties would fail their own
  unrelated attributes and corrupt the result being tested (hit this exact failure while writing
  these tests: a first attempt bundled 12 attributes into one model and every "valid value" case
  failed until each was split out). `[CustomValidation]`'s validator type must be a **public** nested
  class — `CustomValidationAttribute` throws "must be public" for a private one regardless of
  `InternalsVisibleTo`.
- `[DataType]`/`[DisplayFormat]`/`[Editable]`/`[ScaffoldColumn]` (H.30) are in
  `DynamicWizardTests.cs`: input-type/textarea rendering + round-trip for `DataType`, read-only
  rendering + registry-override precedence for `Editable(false)`, `NullDisplayText`/
  `DataFormatString` formatting for both the tier-4 fallback and the `Editable(false)` read-only
  span, and full exclusion (from both render and `ValidateCurrentStep`) for `ScaffoldColumn(false)`.
