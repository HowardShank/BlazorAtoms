# BlazorAtoms.DynamicFormWizard — Development Notes

Internal architecture notes for maintainers. See `README.md` for consumer-facing usage,
`DESIGN-DISCUSSION.md` for the full decision log and rationale behind everything below, `FLOW.md`
for diagrams, `EXTENSIBILITY.md` for the two override seams, and `TASKS.md` for the pending/
deferred-work list mapped to the design sections that explain each one.

## File layout

```
Attributes/     FormStep, FormOrder, DependsOn, ComparisonOperator, FormPathEnd, FormSelect,
                FormDynamicSelect, FormLayout, FormMatrix, FormRatingScale, FormRadioList
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
DynamicWizard.Matrix.cs      tier 1b: [FormMatrix] survey/Likert table (see its own section below)
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
  **`[DataType]` never validates**, by design — `DataTypeAttribute` (base `ValidationAttribute`)
  doesn't override `IsValid`, so it always passes; it's a rendering hint only, matching stock .NET
  behavior. Deliberately not special-cased here: auto-enforcing based on the `DataType` enum value
  would diverge from the ASP.NET Core/EF convention consumers already know (`DataType` = display,
  a separate attribute = validation) and would be a heuristic guess for members with no real format
  (e.g. `DataType.Text`). A consumer wanting real enforcement pairs it with the matching validator —
  `[DataType(DataType.EmailAddress)] [EmailAddress]`, `[DataType(DataType.PhoneNumber)] [Phone]`,
  `[DataType(DataType.Url)] [Url]` — which already works with zero engine code, same as every other
  stock validator in H.29.
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

## `[FormLabel]`/`LabelPosition` + `FieldAttributes` splat + `[Display(Prompt)]` (DESIGN-DISCUSSION.md H.31/H.32, #142/#143)

Three features that share one plumbing mechanism, so they shipped as two batches (`FormLabel`/
`FieldAttributes` together, `Display.Prompt` as a same-day follow-up reusing the same merge point):

- **`[FormLabel(LabelPosition)]`** on a property, falling back to `DynamicWizard.DefaultLabelPosition`
  (an override-wins-over-default pattern, cached on `WizardPropertySchema.LabelPositionOverride`
  like every other top-level attribute; resolved against the runtime `DefaultLabelPosition`
  parameter at render time via `DynamicWizard.razor.cs`'s `EffectiveLabelPosition`, since the
  wizard-level default is a component parameter, not something the process-lifetime schema cache
  can bake in). `Above`/`Left` keep the real `<label>` element `DynamicWizard.razor` renders (`Left`
  only changes `.wizard__field-row`'s layout to put it beside the input via CSS grid, not flex, so
  the error message can still span the full row width below both). `Inline`/`Hidden` render no
  `<label>` element at all — dropping it outright for `Hidden` would leave the input with no
  accessible name, so the label text moves onto the rendered input itself instead
  (`placeholder`/`aria-label` respectively).
- **`DynamicWizard.FieldAttributes`** (`IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>`,
  keyed by top-level property name) splats arbitrary extra HTML onto one named field's rendered
  input — `data-testid`, `autocomplete`, a custom `aria-*`, whatever a consumer needs.
- **`[Display(Prompt = "...")]`** (H.32 follow-up) sets the rendered input's `placeholder` —
  reused straight from stock DataAnnotations (`WizardModelSchema.Build` already reads `display`
  for `Label`; `Placeholder` is just `display?.Prompt` off the same call, cached on
  `WizardPropertySchema.Placeholder`). Applies **regardless of `LabelPosition`** — a visible label
  above the field and a placeholder hint inside it aren't mutually exclusive, unlike `Inline`'s
  label-text fallback which only fires when nothing else set one.

**Shared plumbing:** `FieldTarget` (the `Owner`/`Info`/`Label` struct threaded through every render
helper) gained a fourth field, `ExtraAttributes` (`IReadOnlyDictionary<string, object>?`), computed
once per top-level field in `RenderField`'s `BuildExtraAttributes` — merges, in this exact
`Dictionary.TryAdd` order, the consumer's own `FieldAttributes` entry for that property, then
`Placeholder` (`Display.Prompt`), then an `aria-label`/`placeholder` synthesized from
`LabelPosition.Hidden`/`Inline`. Each step only adds a key if the previous step didn't already set
it, so `FieldAttributes` beats `Prompt` beats the `Inline` label-text fallback — same "more
specific wins" precedence as everywhere else here. The 3-arg `FieldTarget` constructor still exists as a thin wrapper that
passes `null` for `ExtraAttributes` — every nested-group member (`RenderExpandedGroup`) and list-item
target (`RenderScalarItemRepeater`/`RenderComplexItemRepeater`) still constructs via that overload,
so both are automatically out of scope for `FieldAttributes`/label-driven attrs, matching the
existing top-level-only reach `[DependsOn]`/`[FormSelect]` already have (B.6) — a known, deliberate
scope limit, not an oversight.

## `[FormOrder]` → `[Display(Order)]` fallback, and `[FormOrder]`'s planned future removal (DESIGN-DISCUSSION.md H.33)

`FormOrderAttribute` duplicates `DisplayAttribute.Order`, which already exists for exactly this
purpose — an oversight from the original batch (predates the "reuse stock DataAnnotations first"
pattern H.29+ later established). `WizardModelSchema.Build` now reads
`formOrder?.Order ?? display?.GetOrder() ?? int.MaxValue` — `[FormOrder]` still wins when present
(explicit-attribute-wins, same precedence pattern as everywhere else), but a plain
`[Display(Order = N)]` now works with no `[FormOrder]` at all.

**Gotcha worth remembering:** read `display?.GetOrder()`, never `display?.Order` directly.
`DisplayAttribute.Order`'s getter throws `InvalidOperationException` ("The Order property has not
been set. Use the GetOrder method...") when the attribute never explicitly set it — confirmed by a
throwaway script, not assumed. `GetOrder()` returns `int?` and is null-safe. This is the same
reason `WizardModelSchema.Build` already reads `Label`/`Placeholder` off `display?.Name`/
`display?.Prompt` (both plain `string?`, no throw risk) but needed the different `GetOrder()` call
specifically for `Order`.

`[FormOrder]` itself is being kept for backward compatibility only — its doc comment now states
it's a candidate for removal in a future major version, once existing consumers (including this
package's own playgrounds) have had a chance to migrate to `[Display(Order = N)]`. Not removed
this pass; do not delete it without a deliberate, separately-scoped decision.

**Splat mechanism — read this before adding a new render helper.** Every render helper that ends in
`builder.CloseComponent()`/`CloseElement()` gets one extra conditional line before the close:
`if (target.ExtraAttributes is { Count: > 0 }) { ... }`. The call inside differs by what's being
closed, and mixing them up throws at runtime:
- **Elements** (`RenderTypedTextInput`'s `<input>`, `RenderTextArea`'s `<textarea>`,
  `RenderReadOnlyField`/`RenderFallback`'s `<span>`) use `builder.AddMultipleAttributes(seq, target.ExtraAttributes)`.
- **Built-in `InputBase<TValue>` components** (`RenderInput`, `RenderEnumSelect`,
  `RenderNullableEnumSelect`, `RenderSelect` in `DynamicWizard.Selects.cs`) and `InputFile`
  (`RenderFileUpload`) also use `builder.AddMultipleAttributes`, **never**
  `builder.AddAttribute(seq, "AdditionalAttributes", target.ExtraAttributes)` — that was tried
  first and throws at runtime: `"The property 'AdditionalAttributes' ... cannot be set explicitly
  when also used to capture unmatched values."` Every one of these components declares
  `[Parameter(CaptureUnmatchedValues = true)] AdditionalAttributes`, and Blazor's own parameter
  binder captures any attribute name that doesn't match a declared `[Parameter]` into that
  dictionary automatically — `AddMultipleAttributes` splatting each key individually is exactly
  what triggers that automatic capture; explicitly naming the parameter conflicts with it.
- **`RenderRegisteredComponent`** (tier 1, a consumer's own `FieldRenderers`-registered component)
  deliberately does **not** splat `ExtraAttributes` at all — an arbitrary consumer component has no
  guaranteed `CaptureUnmatchedValues` parameter, so adding any attribute it doesn't declare throws
  the same "does not have a property matching the name ..." error components without that pattern
  always throw for an unmatched attribute. A known scope limit for #142/#143, not an oversight —
  a consumer's custom component (e.g. EXTENSIBILITY.md's `MoneyInput`) is unaffected either way.

## Cancel/close affordance (DESIGN-DISCUSSION.md G.26, #137)

`ShowCancelButton` (bool, default `false`) + `OnWizardCancel` (`EventCallback<TModel>`), both on
`DynamicWizard<TModel>`. `HandleCancel` in `DynamicWizard.razor.cs` just invokes the callback with
`Model` — no call into `_navigator`/`ValidateCurrentStep`, unlike `HandleNext`/`HandleSubmit`, since
Cancel is meant to abandon the flow, not complete it: it must work from an invalid current step.
No built-in confirmation dialog — this engine doesn't own any modal UI elsewhere either (e.g.
`[FormDynamicSelect]`'s pending-fetch state is a disabled placeholder, not a spinner overlay), so a
consumer wanting "are you sure?" wraps their own confirm around the callback.

Markup-wise, the button sits in a new `.wizard__nav-group` wrapper alongside Back (not as a third
flex child of `.wizard__nav` directly) — `.wizard__nav`'s `justify-content: space-between` expects
exactly two things to space apart (a "back-ish" group and the forward action); a bare third child
would get pushed to a middle position instead of sitting next to Back. `ShowCancelButton` guards
only the button, not the wrapper div, so the wrapper renders unconditionally and Back's position is
identical whether or not Cancel is showing.

## Draft-save/resume (DESIGN-DISCUSSION.md G.23, #134) — no storage owned by this engine

Three additions, all reused across `WizardNavigator`/`DynamicWizard<TModel>`:

- **`WizardNavigator`'s 3rd ctor param, `int? initialStep = null`.** Sets `CurrentStep` to it if
  it's a real declared step number for this schema (`schema.Steps.Any(s => s.StepNumber ==
  initialStep.Value)`), else falls back to the existing default (the first declared step) —
  handles a stale snapshot from a since-changed schema without crashing or landing on a
  nonexistent step. Optional with a default, so every existing 2-arg call site (tests included)
  is unaffected.
- **`DynamicWizard<TModel>.InitialStep`** (`int?`) is read once in `OnInitialized` and threaded
  straight into the `WizardNavigator` constructor above.
- **`DynamicWizard<TModel>.CurrentStep`** (`int`, get-only, NOT a `[Parameter]`) just proxies
  `_navigator.CurrentStep` — a consumer reads it via `@ref` any time they want to build a save
  snapshot (paired with `Model`, which they already own a reference to).
- **`DynamicWizard<TModel>.OnStepChanged`** (`EventCallback<int>`) fires from `HandleNext`/
  `HandlePrevious` — both are now `async Task` (they weren't before; `@onclick` binds to either
  shape transparently, no razor change needed) — but **only when `_navigator.CurrentStep` actually
  changed**, not on every call. `GoNext`/`GoPrevious` are no-ops at the boundaries (already-last
  step, already-first step) and `HandleNext` returns early when validation fails — none of those
  should fire a spurious "step changed" event.

**Deliberately not built:** any actual storage I/O (localStorage, an API call, IndexedDB) and any
field-level/keystroke autosave hook. This engine stays a 0-dep leaf (A.1) — it hands a consumer
everything they need to snapshot/restore state themselves (`Model` is already the consumer's own
object and is plain-JSON-serializable, right down to `WizardFileAttachment`'s `byte[]`; `CurrentStep`
is now readable too) without ever touching browser storage or an HTTP client itself. A consumer
wanting autosave-on-every-keystroke already owns `Model` and can serialize it on whatever cadence
they like (a timer, page-unload) without a per-edit callback from this engine.

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

## Nested-target `DependsOn` (DESIGN-DISCUSSION.md section M, #138)

Two previously-separate gaps, fixed together since both are "a `[DependsOn]` target that isn't a
flat top-level property name":

- **Dotted-path targeting into a nested group.** `WizardNavigator.ResolvePath(object? root, string
  path)` (private, static) walks a `.`-split chain via `GetProperty`/`GetValue`, returning `null` on
  any missing segment (never throws — a null-along-the-path is just "not yet satisfied," same as
  any other null actual value `Matches` already treats that way). `ResolveTarget` chooses the fast
  path for a plain (undotted) name — the existing `_schema.TryGetByName` cached lookup, unchanged
  from before this feature — and only falls through to `ResolvePath(_model, ...)` when a dot is
  present, so every pre-existing flat `[DependsOn]` usage pays zero extra cost.
- **List-item sibling targeting.** `WizardNavigator.IsItemPropertyVisible(object itemInstance,
  IReadOnlyList<DependsOnAttribute> dependencies)` is `static` (no schema, no `Model` involved at
  all) — it resolves purely against the item instance passed in. Called from
  `DynamicWizard.Lists.cs`'s `RenderComplexItemRepeater`, once per item property, reading that
  property's `[DependsOn]`s directly via `GetCustomAttributes` (un-cached, matching how that method
  already reads `[Display(Name=...)]` per item property with no schema involved).

**The asymmetry to remember:** a nested group member's own `[DependsOn]` still resolves from
`Model` (via the new public `AreDependenciesSatisfied(IReadOnlyList<DependsOnAttribute>)` — the same
method `IsVisible` now delegates to), so even a sibling *within the same group* needs the full
dotted path (`"Contact.IsPrimary"`, not bare `nameof(IsPrimary)`) — a nested group has a
deterministic property chain from `Model`, so there's no reason to special-case it. A list item has
no such chain (rows are runtime instances at a variable index), which is why `IsItemPropertyVisible`
is the one place a bare `nameof` resolves against something other than `Model`. Get this backwards
and a nested-group sibling `[DependsOn]` will silently always evaluate to hidden (a plain name looks
up the *top-level* schema, finds nothing, `Matches(null, ...)` is always `false`) — there's no
exception to catch, so this is easy to author wrong and not notice without a render test.

Both `RenderExpandedGroup` and `RenderComplexItemRepeater` now `continue` the property loop when a
nested/item field's dependencies aren't satisfied, instead of unconditionally rendering every
property — previously neither method looked at `[DependsOn]` at all for a nested/item member (not
merely "top-level only," genuinely never checked), so this also fixes a latent bug that predates
G.27: a nested group member carrying `[DependsOn]` before this fix was rendered regardless, silently
ignoring the attribute. Step-level visibility (`EffectiveStepNumbers`/`DisplayPosition`, C.9) is
untouched — both fixes are purely inside the field-render dispatch, the same tier `[DataType]`
already operates at.

## `[FormMatrix]`/`[FormRatingScale]`/`[FormRadioList]` — the three G.29/30/31 field types (fully shipped end to end, #163-172)

Deliberately split into attribute+wiring vs. tests vs. docs+playground per feature (own tracker
tasks each) — read `TASKS.md` for exactly which sub-task covers what. `[FormMatrix]`'s own
attribute+schema/dispatch+markup/tests/docs+playground split (#163/#164/#165/#166) happened across
two separate work sessions; J/K (rating scale/radio list) shipped their full render path in one
pass each (#167/#170), with tests (#168/#171) as a later, separate pass.

**A real bug was caught only by writing #171's radio-list tests, not by re-reading the #170 code:**
`InputRadioGroup<TValue>` renders no wrapping DOM element of its own — it only supplies a cascading
value to its `ChildContent`. The original #170 pass had passed `class="wizard-radio-list"` straight
to the component (the same pattern every other tier-2 component uses, since they *do* render their
own element), which meant the class was silently dropped — no exception, no visible symptom short
of a missing element in the rendered markup. Confirmed by a temporary `throw new
Exception(cut.Markup)` in the failing test, not by reading Blazor's source and assuming. Fixed by
having `RenderEnumRadioList`/`RenderNullableEnumRadioList` open an explicit `<div
class="wizard-radio-list">` themselves, around the component. **Second finding from the same
debugging pass:** a real `onchange` on `InputRadio<TValue>` carries the radio's own `value`
attribute (the enum member name, a string) as the event payload — not a boolean "checked" flag the
way a native checkbox's change event does. A bUnit test must call `.Change("EnumMemberName")`, not
`.Change(true)`, or the click silently no-ops. This does not affect `RenderRatingScale`'s or
`RenderMatrixGrid`'s own radios, both of which are hand-rolled raw `<input>` elements whose
`onchange` handlers ignore the event payload entirely and close over the target value directly.

- **`[FormMatrix]` (#163/#164) — attribute+schema, then dispatch+markup, in that order.**
  `WizardPropertySchema.Matrix`/`WizardModelSchema.Build` read the attribute like every other
  per-property one (#163). `RenderDispatched`'s tier 1b (`TryGetListItemType`) checks for it ahead
  of the ordinary `RenderListProperty` call, routing to `RenderMatrixGrid` in the new
  `DynamicWizard.Matrix.cs` partial instead (#164) — a real `<table>`, `<th scope="col">`/
  `<th scope="row">` for accessible row/column association, one radio group per row (`name` =
  `$"{target.Info.Name}-{index}"`, unique per row so exactly one selection sticks per statement with
  no manual bookkeeping). `AnswerProperty`/`LabelProperty` are resolved via plain `Type.GetProperty`
  on `TItem` — no caching, matching the same un-cached pattern `RenderComplexItemRepeater` already
  uses for a list item's own `[Display(Name=...)]`. Validation needed zero changes, confirmed by
  test (#165) rather than just predicted: a matrix's `List<TItem>` is the identical data shape
  G.25's ordinary complex-item lists already validate.
- **Matrix "fails silently" fix + `[RequiredUnless]` (DESIGN-DISCUSSION.md section I items 8-9,
  shipped same day as #166) — a required-but-unanswered row blocked `Next`/Submit with zero visual
  signal as to which row.** `RenderMatrixGrid` now computes, per row: `isInvalid` from
  `_editContext.GetValidationMessages(new FieldIdentifier(item, matrix.AnswerProperty)).Any()` —
  the same check every built-in field already makes for its own invalid CSS class, applied to the
  `<tr>` (`wizard-matrix__row--invalid`) since a row has no single element to outline — and
  `isRequired`, which is no longer a fixed per-type fact: it's `true` unconditionally if
  `AnswerProperty` carries `[Required]`, or conditionally if it carries the new
  `Validators/RequiredUnlessAttribute.cs` (`[RequiredUnless(nameof(SkipFlag))]`) AND that specific
  row's own skip-flag property is currently `false`. `RequiredUnlessAttribute.IsValid` reflects
  `ValidationContext.ObjectInstance` for the named skip property — the exact mechanism `[Compare]`
  already uses to reach a sibling property off the whole model (H.29), here reaching a sibling off
  the *item* instead, since `Validator.TryValidateObject`'s per-item pass (G.25/section I item 5)
  already sets `ObjectInstance` to that item. Zero `WizardNavigator` changes — this is a new
  `ValidationAttribute`, not new validation plumbing, so it flows through the exact same
  zero-engine-code reuse path H.29's batch established for every other stock/custom attribute.
- **`[FormRatingScale]` (#167) is fully wired — attribute, dispatch, markup, and CSS all shipped.**
  Lands inside `TryRenderBuiltInScalar`'s existing `NativeNumberTypes` branch, restricted to
  `effectiveType == typeof(int)` specifically (not `long`/`decimal`/etc. — the attribute's shape
  only makes sense for a whole-number scale). `RenderRatingScale` builds a manually-bound radio
  row the same shape `RenderTypedTextInput`/`RenderTextArea` use (raw elements, not an
  `InputBase<TValue>` subclass) — clicking a point calls `target.SetValue(point)` directly (an
  `int`, no parsing needed) then `OnFieldChanged()`. The group `name` is synthesized from
  `target.Info.Name` + `target.Owner.GetHashCode()` so two different rating-scale fields (or two
  different list-item rows, if ever nested that way) never collide.
- **`[FormRadioList]` (#170) is fully wired — attribute, dispatch, markup, and CSS all shipped.**
  `RenderEnumRadioList`/`RenderNullableEnumRadioList` mirror `RenderEnumSelect`/
  `RenderNullableEnumSelect` one-for-one, just opening `InputRadioGroup<TEnum>` +
  `InputRadio<TEnum>` per member instead of `<option>` elements. Needs no manual `onchange`/`name`
  wiring at all — `InputRadioGroup<TValue>` is itself an `InputBase<TValue>` descendant and
  provides the shared grouping/selection state to its `InputRadio<TValue>` children via its own
  cascading value, the same reason this section-K feature was the simplest of the three to build.
  **Compiler gotcha hit while wiring the nullable variant's leading "-- none --" option:**
  `builder.AddAttribute(seq, "Value", null)` is ambiguous — `RenderTreeBuilder.AddAttribute` has
  overloads for `string?` and `MulticastDelegate?`, and a bare `null` literal can't pick between
  them. Fixed by casting explicitly: `AddAttribute(seq, "Value", (object?)null)`.

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
`WizardPropertySchema`). `[DependsOn]` itself gained nested/list-item reach (section M, #138) — this
top-level-only limit still applies to `[FormSelect]`/`[FormDynamicSelect]` specifically; extending
either of those to nested groups is a possible future enhancement, not attempted in v1.

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
- Nested-target `DependsOn` (section M, #138): `WizardNavigatorTests.cs` covers dotted-path
  resolution directly against `IsVisible` (matching, non-matching, and a `null` nested segment along
  the path — proving it degrades to "not visible" rather than throwing). `DynamicWizardTests.cs`
  covers the two render-level fixes end to end: a nested group member's own `[DependsOn]`
  hidden/shown via a full `Render<T>` + checkbox `.Change(true)`, and — the one that would actually
  catch a resolution-root regression — two list rows' sibling `[DependsOn]` toggled independently,
  asserting row 1's field count and `IsPrimary` value are both untouched by row 0's toggle.
