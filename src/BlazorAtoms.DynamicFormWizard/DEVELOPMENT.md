# BlazorAtoms.DynamicFormWizard — Development Notes

Internal architecture notes for maintainers. See `README.md` for consumer-facing usage,
`DESIGN-DISCUSSION.md` for the full decision log and rationale behind everything below, `FLOW.md`
for diagrams, and `EXTENSIBILITY.md` for the two override seams.

## File layout

```
Attributes/     FormStep, FormOrder, DependsOn, FormPathEnd, FormSelect, FormDynamicSelect, FormLayout
Validators/     FormRegex, DateRange, MaxFileCount, MaxFileSize, AllowedExtensions
Services/       IWizardLookupService (FormDynamicSelect's DI contract)
Schema/         WizardPropertySchema, WizardStepSchema, WizardModelSchema (reflection, cached per Type),
                WizardTypeInspection (shared complex-vs-scalar test)
Navigation/     WizardNavigator -- headless step/DependsOn engine, no Razor dependency
Files/          WizardFileAttachment
Rendering/      WizardFieldContext, WizardFieldCssClassProvider
DynamicWizard.razor(.cs)     component shell: EditContext, nav chrome, accessibility
DynamicWizard.Fields.cs      the 4-tier field-render dispatch
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

## The 4-tier field-render dispatch (`DynamicWizard.Fields.cs`)

1. Consumer `FieldRenderers` registry match (exact type) — always wins.
2. Built-in scalar types (bool/enum/string/DateTime/int·decimal·double/file).
3. Auto-expand: a complex type's own public read/write properties become a field group, validated
   recursively via `Validator.TryValidateObject` (as opposed to `TryValidateValue` for leaves).
4. Fallback: read-only `ToString()` + a `Debug.WriteLine` warning — an unhandled type never
   silently disappears from the form.

`WizardTypeInspection.IsComplexType` is the single shared predicate deciding tier 3 vs. everything
else, used identically by rendering (this dispatch) *and* validation (`WizardNavigator`) — they
must agree, or a property could render as a group but validate as a leaf.

## `FieldTarget` and why nested rendering reuses the same dispatch

`FieldTarget` (a `record struct` in `DynamicWizard.Fields.cs`) is `(object Owner, PropertyInfo
Info, string Label)` — the object that actually owns a value and the reflected property that
reads/writes it. For a normal field, `Owner` is the top-level `Model`; when `RenderExpandedGroup`
recurses into a complex property's own properties (tier 3), it constructs a new `FieldTarget` with
`Owner` set to the *nested* instance. This is what lets the same `RenderDispatched` method handle
both top-level and nested fields without duplicating the type-switch.

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
