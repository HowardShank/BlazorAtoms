# BlazorAtoms.DynamicFormWizard — Design Discussion

A living log of the architecture decisions behind this package, captured while pressure-testing
the concept against real scenarios — *before* any engine code exists. Update this doc as future
sessions extend or revise the design; don't let decisions live only in chat history.

## How to use this doc

- **`Ideas.md`** (this folder) is the source material — 7 chat-generated design iterations that
  first sketched a reflection/attribute-driven wizard engine. It was always a *starting point for
  discussion*, not a spec. Several of its approaches were revised or replaced below; where that
  happened, it's called out explicitly.
- This doc is the **decision log**: what was decided, and *why* (the scenario or repo convention
  that forced it). `FLOW.md` visualizes the mechanics. `EXTENSIBILITY.md` has the full worked
  code example for the extensibility seams.
- Section letters (A–G) and item numbers below are stable identifiers — reference them (e.g.
  "per B.5") rather than re-explaining a decision when adding to this doc later.

## Why this package doesn't fit the rest of the family

Every other BlazorAtoms library is hand-composed markup (`<AtomTextField>`, `<AtomSelect>`) — a
consumer places components explicitly. `Ideas.md`'s premise is the opposite: decorate a POCO with
attributes, and an engine reflects over it to auto-generate the entire multi-step form. The
family's pitch ("every library is a small, standalone, ~0-dep atom — take one, take several, never
inherit a framework") doesn't map cleanly onto an engine whose entire job is generating UI from
metadata. Section A works through how much of that pitch this package can actually keep.

## Decisions locked

### A. Package architecture

1. **Headless core vs. render adapter, two packages.** `BlazorAtoms.DynamicFormWizard` stays a
   0-dep leaf: attributes, the step/`DependsOn` engine, partial per-step validation, dynamic
   effective-step-count, plus a native-input/bare-CSS fallback renderer — fully usable standalone.
   A *separate*, later package hard-depends on both this package and `BlazorAtoms.Inputs` (+
   optionally `BlazorAtoms.Progress` for a step tracker) to swap in Atom*-styled rendering.
   **Why:** `ProjectReference` is compile-time/all-or-nothing in .NET — there's no clean "use
   Inputs if present." Hard-depending on `Inputs` unconditionally would force every consumer who
   installs `DynamicFormWizard` alone (including external NuGet consumers) to pull in a package
   they may not want, breaking the family's "take one, never inherit a framework" promise. Two
   packages is the honest resolution, not a forced or hacky dependency inside one. Mirrors a
   headless-core/UI-adapter split (e.g. React Hook Form's headless engine + UI bindings).
2. **`FieldTemplate` seam — whole-form override.** The wizard calls a per-field renderer through
   an overridable `RenderFragment`-shaped parameter rather than a hardcoded switch, so any
   consumer (including the future render-adapter package) can swap rendering for **every** field
   at once without the core engine depending on `Inputs`.
   **Why:** lets the Atom*-styled look exist without a hard dependency — dogfooding happens at the
   call site (or in the adapter package), not baked into the core.
3. **Type-to-component registry — single-type override.** A `Dictionary<Type, Type>` the engine
   checks before its own built-in switch, for a consumer type that needs its own specialized
   widget without expanding into multiple fields. Distinct from `FieldTemplate`: the registry
   swaps rendering for **one type**, leaving every other property on defaults.
   **Why:** `FieldTemplate` is all-or-nothing (whole form). A consumer with 95% ordinary fields and
   one exotic type (e.g. `Money`) shouldn't have to reimplement the whole form just to special-case
   one property. Full worked example (a `Money` struct) in `EXTENSIBILITY.md`.
4. **Field-render dispatch, five tiers in priority order:**
   1. Consumer's type-registry match for this exact property type (explicit opt-in always wins).
   2. Native built-in scalar types — the ones Blazor's own typed `Input*` components handle
      directly: `bool`, `enum`, `DateTime`/`DateOnly`/`TimeOnly`, `int`/`long`/`short`/`float`/
      `decimal`/`double`, `string`, file.
   2b. **Generic `IParsable<T>` tier** — any value type implementing `IParsable<T>` that tier 2
      didn't already claim: `byte`, `sbyte`, `ushort`, `uint`, `ulong`, `char`, `nint`, `nuint`,
      `Guid`, `TimeSpan`, or a *consumer's own* custom struct that implements `IParsable<T>`.
      Rendered by `WizardParsableInput<TValue>` (`Rendering/WizardParsableInput.cs`) — a single
      generic `InputBase<TValue>` subclass, not one branch per type, so it also auto-covers any
      future BCL or consumer type that adopts the interface, with no registration needed.
   3. Auto-expand — the property's type is itself a complex class with its own public
      read/write properties, so it recurses and renders *those* as a field group (see B.5).
      **Collection types are explicitly excluded**, even though they're classes: anything
      implementing non-generic `IEnumerable` fails this tier by design.
      `WizardTypeInspection.IsComplexType` checks for it directly, because `List<T>`'s only
      public read/write, non-indexer property is `Capacity` (an `int`) — without the exclusion, a
      `List<string>` property would auto-expand into a field group showing only a bogus
      "Capacity" number input, hiding every actual list item, and nothing about the rendered UI
      would signal anything was wrong. Real `List<T>`/collection editing support (a scalar-list
      editor vs. full repeating groups of complex types vs. consumer-registered-only) is a
      separate, deferred decision — see G.25.
   4. Fallback — read-only `ToString()` rendering plus a dev-time warning, so an unhandled type
      never silently disappears from the form. Genuinely reached only by a type that is neither a
      known scalar, `IParsable<T>`, nor a complex class (e.g. a bare struct with public fields but
      no properties and no `IParsable` implementation, or any collection type per tier 3 above).
   **Why:** each tier solves a distinct problem (explicit override, common case, exhaustive native
   coverage, structural grouping, safety net) — collapsing any two would either lose the escape
   hatch or lose the safe fallback for genuinely unknown types. Tier 2b exists because the goal is
   an engine that handles anything provided for built-in/native data — custom types go through the
   type-registry (tier 1) by design, but every native integral/BCL scalar type needs to work out
   of the box, not just the handful Blazor ships dedicated components for.
4a. **`Nullable<T>` is dispatched by its underlying type, at every tier that needs it.**
   `Nullable<T>` can never itself satisfy an interface constraint like `IParsable<T>` — a C#
   language rule (confirmed by the compiler: "Nullable types can not satisfy any interface
   constraints"), not a gap fixable by wrapping. So `int?`/`byte?`/`DateTime?`/`Guid?`/`bool?`/
   nullable-enum/etc. are handled explicitly:
   - Native numeric (`InputNumber<TValue>`) and date (`InputDate<TValue>`) components already
     support their `T?` forms directly — the dispatch just needs to route the *full* declared
     type (nullable wrapper included) to them, not strip it first.
   - Nullable enums get their own `RenderNullableEnumSelect`, adding a leading `-- none --`
     option, since an unset nullable enum means "no selection," not "default to the first member."
   - Everything else that's `IParsable<T>` at tier 2b (including `bool?`, since there's no native
     tri-state checkbox) routes through `WizardNullableParsableInput<TValue>`
     (`where TValue : struct, IParsable<TValue>`, deriving `InputBase<TValue?>`) — an empty string
     parses to `null`, not a validation error, since a cleared field means "no value."
   **Why:** a prior bug (fixed the same session it was introduced) had `RenderDispatched` already
   stripping `Nullable<T>` to its underlying type *before* tier 2 ever ran, so every nullable
   branch inside the dispatch was unreachable dead code — proven by a test that rendered every
   nullable native type at once and got an `Expression<Func<T>>`/`Expression<Func<T?>>` cast
   crash. The fix: only tier 1's registry lookup and tier 3's complex-type check use the
   unwrapped type; tier 2 receives the full declared type so its own nullable-aware branches
   actually run.

### B. Data model

5. **One POCO drives everything — no second workflow/JSON object.** Top level stays **flat** for
   step/`DependsOn` purposes, exactly as proven against every scenario traced this session:
   `[FormStep(step, order, title)]` + `[DependsOn]` on top-level properties, targeted by plain
   property-name strings (already fully reachable — no nesting problem exists at this level). A
   top-level property whose *own type* is a complex class **auto-expands** (dispatch tier 3 above)
   into a field group within its one step — e.g. `[FormStep(2)] public CustomerInfo CustomerInfo
   { get; set; }` renders `CustomerInfo`'s own properties as multiple inputs, all inside step 2,
   validated recursively via `Validator.TryValidateObject` (the standard, already-solved .NET
   nested-validation pattern — not a new mechanism).
   **Why:** the user's real goal was "one attributed POCO drives steps, validation, and generation
   — no parallel schema to maintain." Flat-plus-auto-expand achieves that with *less* machinery
   than a fully nested/grouped model would need (no path-based `DependsOn` targeting, no recursive
   step-graph), while still giving natural grouping (the `CustomerInfo`/`ManagerAccount` intuition
   from the account-type scenario) for free.
6. **Known limitation, explicit, deferred (G.27):** `DependsOn` only targets top-level property
   names — it cannot reach into a nested group's own fields (e.g. "hide something based on
   `CustomerInfo.Country`"). No scenario traced this session needed that; every `DependsOn` so far
   only ever depended on a top-level answer (account type, chosen strategy).
7. **Mutability constraint, stated explicitly:** `TModel : class, new()`; only properties with
   both `CanRead` and `CanWrite` participate. Records and init-only properties are **not**
   supported in v1 — reflection `SetValue` requires a settable property. A known limitation, not a
   silent gap.

### C. Step model

8. **Steps are implicit, not an explicit graph.** `[FormStep(int)]`'s int is a bare authoring key;
   `MaxSteps` is never a fixed constant — the engine walks forward/back, skipping any step number
   with zero currently-visible properties. Branching is just per-property `[DependsOn]`;
   **rejoining is free** — a step whose properties carry no `[DependsOn]` shows for every branch
   that reaches it, no special "merge" construct needed.
   **Why/proof:** traced against two concrete scenarios (see "Scenarios walked through" below) —
   both a true A/B fork converging on a shared step, and a simpler "unconditional → optional
   middle step → unconditional" flow. Both worked with zero extra mechanism beyond "omit
   `DependsOn` on the shared step."
8a. **`[FormPathEnd(targetProperty, expectedValue)]` — an authoritative end marker, stackable
    (AND-combined, same shape as `[DependsOn]`).** When every stacked condition on a property
    currently matches, the step it belongs to is treated as final — navigation stops there and
    every later declared step is excluded from `EffectiveStepNumbers()`/`DisplayPosition()`,
    *regardless* of whether a later property happens to be (perhaps mistakenly) visible.
    **Why — reversed from an earlier call in this doc:** derived-only "final" (C.8, "nothing
    declared after this step is currently visible") works *only* if every later-branch field is
    correctly gated. It fails silently the moment one isn't: a missing `[DependsOn]` on a later
    field means "no condition," which already means "always visible," so a branch meant to end
    earlier walks straight into it. As branch count grows, so does the number of `[DependsOn]`s a
    consumer must get right on every later step — multiplying the odds of exactly this mistake.
    One `[FormPathEnd]` per branch's true termination point is authoritative regardless of how
    later steps are (mis)configured — **fewer** attributes overall, not more, and safe by
    construction rather than by careful bookkeeping. Verified: a model with an intentionally
    unguarded "accidentally unconditional" later step still correctly excludes it for the marked
    branch, while an *unrelated* branch with no marker of its own still surfaces the original
    mistake — the fix is targeted, not a blanket safety net masking real authoring errors.
9. **Step display always computed, never raw — two independent fixes, both required together:**
   - *Label* → `FormStepAttribute(int stepNumber, string? title = null)`. Order-within-step is a
     *separate* `[FormOrder(int)]` attribute (C.10), not a parameter on `FormStep` — the two stack
     independently. The `stepNumber` int is the internal key only (`DependsOn` targets, nav,
     skip-logic) — never shown to the user. Title-resolution rule: first non-null `Title` among
     that step's *currently visible* properties wins; if none set anywhere, fall back to
     `"Step {position}"` using the recomputed ordinal below — never the raw attribute int.
     **Collision rule, resolved (not left undefined):** "first" means first in the *same*
     `(FormOrder, encounter-order)` sort already used for field render order (C.10) — i.e.
     whichever titled property would render first in that step wins, silently, no error. Two
     properties in one step declaring different non-null titles is deterministic by construction,
     not a special case needing its own tie-break rule.
   - *Count/position* → recomputed live from the currently-visible step-number list (distinct
     `[FormStep(N)]` values with ≥1 visible property right now), not a static
     `properties.Max(step)` snapshot taken once at init.
   **Why:** a raw declared step count is misleading the moment branches have different true
   lengths. Traced example: an account-type wizard where the Personal path only ever shows 3
   real screens and the Manager path shows 4, despite the model declaring 4 step numbers overall
   — showing "Step 2 of 4" to a Personal-path user (who will only ever see 3) is wrong even though
   navigation itself is correct.
10. **Field order within a step: separate `[FormOrder(int)]` attribute**, not folded into
    `FormStep`. **Why:** raw reflection property enumeration is **not** guaranteed stable across an
    inheritance hierarchy — a real, documented .NET gotcha, not hypothetical. Locked as a separate
    attribute (rather than a `FormStep(step, order)` tuple) per explicit user choice.
11. **`[DependsOn]` gained a `ComparisonOperator` (G.28, shipped).** `Equals` (default, backward
    compatible), `NotEquals`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`.
    Combining stays AND-only, stackable, exactly as before — **no OR was added.** A range condition
    (e.g. age 18–65) is expressed by stacking two conditions on the *same* property
    (`GreaterThanOrEqual 18` + `LessThanOrEqual 65`), reusing the existing AND-combine rule rather
    than inventing a new construct. Ordering operators require the target property's actual value
    to implement `IComparable` and be comparable against `ExpectedValue`'s runtime type (the same
    requirement C#'s own comparison operators have) — `WizardNavigator`'s private `Matches` helper
    throws a clear `InvalidOperationException` naming the offending type if not. OR-combination
    remains deliberately out of scope; no scenario has needed it yet.

### D. Validation

12. **`EditForm`/`EditContext` plumbing, clarified precisely:** the wizard uses a real
    `EditContext` + cascading value + custom `FieldCssClassProvider` for native focus/CSS-state
    integration, but validation *execution* is manual and per-step (`Validator.TryValidateValue`
    for scalars, `Validator.TryValidateObject` for auto-expanded nested groups) — **not** the
    automatic whole-object `<DataAnnotationsValidator />` behavior.
    **Why:** easy to conflate "uses EditContext" with "uses automatic DataAnnotations validation."
    They're independent: this package wants native focus/ARIA/CSS-state wiring from `EditContext`
    while keeping validation scoped to only the currently-visible step's properties (so hidden,
    not-yet-relevant steps never block navigation on their own rules).
13. **Custom `ValidationAttribute` subclasses are the extension point for any specialized rule**
    (regex pattern, date range, file count/size/extension) — one consistent mechanism, not a
    special case per rule type. Because validation runs through `Validator.TryValidateValue`/
    `TryValidateObject`, any `ValidationAttribute` subclass is picked up automatically — zero
    engine changes needed to add a new rule.

### E. File uploads

14. **Own render branch**, not a variant of the shared `Value`/`ValueChanged`/`ValueExpression`
    path every scalar shares. **Why:** native `InputFile` has no such contract, just `OnChange`
    with `InputFileChangeEventArgs` — it doesn't fit the shared attribute-dictionary pattern every
    other type uses.
15. **Storage: engine copies bytes to a wizard-owned DTO itself, immediately on selection** —
    filename/size/content-type + byte[] or temp path — not raw `IBrowserFile` handles left for the
    consumer to manage. **Why:** `IBrowserFile`'s stream is tied to the current circuit/render and
    can't be held indefinitely; a consumer holding a raw reference risks reading from a dead
    stream later. Locked as the engine's responsibility, not the consumer's.
16. **The one genuinely-`async` single-field handler** in an otherwise fully-synchronous
    property-set pipeline. **Why:** reading a browser file stream is unavoidably `Task`-returning
    (`OpenReadStream().CopyToAsync(...)`); must be awaited *before* the property is considered
    "set," so partial-step validation (D.12) sees the real value rather than an empty collection.

### F. Cross-cutting requirements (repo-convention-driven — stated as requirements, not open questions)

17. **Accessibility is first-class, not an afterthought.** Every other BlazorAtoms component ships
    full ARIA (roles, live regions, focus management). This wizard needs step-change announcements
    (`aria-live`) and focus movement to the new step's heading/first field on advance, matching
    family convention. Not discussed in any `Ideas.md` iteration — added because the rest of the
    family treats this as non-negotiable.
18. **Interactive-render-mode-only, documented explicitly.** Reflection-driven rendering +
    `EditContext` + file upload all require an interactive render mode — unlike most Atom
    components (which mostly work under static SSR), this one essentially never does. State this
    plainly in the package README, the way `AtomTabs` documents its own render-mode limits.
19. **Reflection/metadata caching.** Every `Ideas.md` iteration re-reflects (`GetProperties`,
    `GetCustomAttribute`) on *every render*, including every keystroke. A per-`TModel` schema
    (steps, order, titles, dependencies, validators — all attribute data) must be computed once
    and memoized (e.g. a static cache keyed by `Type`), not re-walked per render. A real perf smell
    in the original iterations, not present in any of them.
20. **`[FormDynamicSelect]`'s lookup-service interface lives in the core package** (no third-party
    dependency introduced) but is a stated consumer setup requirement — register it in DI or
    dynamic dropdowns don't render. Documented explicitly rather than left implicit, since it's
    easy to silently forget.
21. **`[FormLayout]`'s column span is re-derived in bare-CSS terms**, replacing `Ideas.md`
    iteration 4's Bootstrap `col-md-*` classes entirely: a CSS Grid `grid-column: span N` driven
    off a `--wizard-column-span` custom property, consistent with the bare-CSS decision already
    locked for this package (no framework classes anywhere, CSS-variable overrides for consumers
    regardless of which CSS approach they use).

### G. Deferred / future enhancements (explicitly out of scope for v1 — not gaps in the reasoning)

22. **Step-transition async action hook** (e.g. submit-and-wait mid-wizard — the "financial
    application → step 5 displays the response from an external finance system" scenario). Needs
    pending/error UI states and a "mid-flow submission" distinct from "final wizard completion" —
    genuinely new primitive, none of which exists in any `Ideas.md` iteration. Explicitly deferred
    by the user as a future enhancement.
23. **Draft-save/resume — shipped, see H.35 (#134).** `Model` + file DTOs mid-wizard were already
    plain-JSON-serializable; the missing piece was resuming at a saved step, now `InitialStep`/
    `CurrentStep`/`OnStepChanged`.
24. **i18n** of labels/messages — currently hardcoded English throughout `Ideas.md` (both
    `[Display(Name=...)]` text and custom-validator error messages).
25. **Repeating/collection steps (shipped) — `List<T>` support, scoped to exactly `List<TItem>`.**
    See A.4 tier 1b and `DynamicWizard.Lists.cs`. Two shapes depending on `TItem`:
    - **Scalar item** (`List<string>`, `List<int>`, `List<Guid>`, etc.): a repeating row of
      single-value inputs, each reusing tier 2/2b's existing dispatch unchanged. Getting a
      `ValueExpression` for `list[i]` the "obvious" way (an `IndexExpression`) was tried first and
      failed at runtime — `FieldIdentifier.Create` explicitly rejects index expressions ("only
      supports simple member accessors"). The fix: a one-property `ListItemBox<TItem>` wrapper
      whose own `Value` property *is* a simple member accessor and whose setter writes through to
      the real list slot. Boxes are cached per (list, index) across renders (evicted on any
      add/remove) because `EditContext` tracks modified/invalid state by `FieldIdentifier`
      equality, which compares the owner object — a fresh box every render would silently forget
      that state on the next render.
    - **Complex item** (`List<Beneficiary>`, the actual "add N beneficiaries" case): every item's
      own sub-form renders as its own `fieldset`, stacked in one step (not paginated one-per-screen
      — a deliberate scope choice to avoid inventing a wizard-within-a-wizard navigation concept).
      Each item's fields are ordinary property-owned targets (owner = the item instance itself,
      which is already reference-stable since it *is* the real list element) — full validation
      support within the item, same as an existing nested group.
    - `[MinItemCount(n)]`/`[MaxItemCountAttribute(n)]` validate the list property itself (mirroring
      `MaxFileCountAttribute`'s shape); each complex item is *additionally* validated individually
      via `Validator.TryValidateObject`, with errors stored against the item instance so they
      resolve to the same `FieldIdentifier` its own rendered fields already use.
    - **Still out of scope:** `DependsOn` can't target a field inside a list item (same limitation
      as G.27 for ordinary nested groups), and a list item's own fields can't depend on each other
      either. Only `List<T>` is supported — not `IList<T>`/`ICollection<T>`/arrays/other
      collections, kept deliberately narrow (see `WizardTypeInspection.TryGetListItemType`).
26. **A cancel/close affordance distinct from Back — shipped, see H.34.**
27. **Nested-target `DependsOn`** (path-based targeting into a group's own fields — see B.6). Also
    now the reason a repeating list item's own fields can't depend on each other (see G.25).
28. **Richer `DependsOn` operators — shipped, see C.11.** OR-combination specifically remains
    deferred; comparison operators beyond equality are done.

*(A hard "force-stop here" override was considered deferred at one point in this doc's history —
reconsidered and built once a concrete failure mode was raised; see C.8a, no longer deferred.)*

### H. Standard DataAnnotations attribute coverage & field-level rendering hooks (#141/#142/#143, shipped)

29. **`[Compare]` — and every other stock `ValidationAttribute` never specifically coded for —
    already worked with *zero* engine changes, proven by test rather than assumed.** D.12/D.13
    already explain why validation runs through `Validator.TryValidateValue`/`TryValidateObject`
    against whatever attributes are actually present; the detail worth stating explicitly is
    *why* a cross-property attribute like `[Compare(nameof(Other))]` also works: `ValidateCurrentStep`'s
    scalar branch builds its `ValidationContext` as `new ValidationContext(_model) { MemberName =
    ... }` — `ObjectInstance` is the *whole model*, not just the one value being checked — so
    `CompareAttribute`'s own reflection lookup of the sibling property off
    `ValidationContext.ObjectInstance`/`ObjectType` finds it correctly, with no wizard-specific
    plumbing at all. Also proven this pass: `CreditCard`/`Phone`/`Url`/`RegularExpression`/
    `StringLength`/`Length`/`MinLength`/`MaxLength`/`AllowedValues`/`DeniedValues`/`EnumDataType`/
    `Base64String`/`CustomValidation` — see `WizardNavigatorTests.cs`.
30. **`[DataType]`/`[DisplayFormat]`/`[Editable]`/`[ScaffoldColumn]` — new rendering-level
    attribute support (distinct from D.12/D.13's validation-only coverage, since these change what
    gets *rendered*, not what gets *validated*).**
    - `[DataType]` on a `string` property maps a small, deliberately-narrow set of well-known
      shapes (`Password`, `EmailAddress`, `PhoneNumber`, `Url`, `MultilineText`) to a real HTML5
      `input type="..."` or a `<textarea>`. Native `InputText` can't be reused for this: it
      hardcodes `type="text"` itself, written to the render tree *after* `AdditionalAttributes`,
      so a "type" attribute passed through `AdditionalAttributes` can never win. The fix is a raw,
      manually-bound `<input>`/`<textarea>` (same shape as the file-upload branch's manual
      binding, E.14) instead of trying to fight `InputText`'s own attribute. Other `DataType`
      members (`Currency`, `PostalCode`, `CreditCard`, etc.) are left as plain text pending a real
      formatting/masking need — not every member has an obvious single-input mapping.
    - `[DisplayFormat(DataFormatString=..., NullDisplayText=...)]` formats *read-only* display only
      — the tier-4 fallback (an unhandled type) and `[Editable(false)]`'s read-only span both
      route through one shared `FormatDisplayValue` helper. Deliberately **not** applied to any
      editable tier: `DataFormatString` is a display-mode format (`string.Format`), not an input
      mask — applying it to a live input's bound value would need parsing the formatted string
      back out on every keystroke, which no built-in `Input*` component does.
    - `[Editable(false)]` forces a read-only render regardless of what tier the type would
      otherwise dispatch to — checked first in `RenderDispatched`, ahead of even the tier-1
      consumer type-registry, since an explicit "don't let this be edited" is a stronger signal
      than any renderer's opinion about how the type would otherwise render.
    - `[ScaffoldColumn(false)]` excludes a property entirely — never rendered, never validated,
      never counted toward a step's visibility. Filtered out at the same three points that already
      enumerate a type's properties (`WizardModelSchema.Build` for top-level, `RenderExpandedGroup`
      for a nested group, `RenderComplexItemRepeater` for a repeating list's item type), so the
      property never becomes a `WizardPropertySchema`/render target at all — consistent with its
      EF/scaffolding intent of "this doesn't exist for generated UI purposes," rather than a
      render-only hide that would still validate underneath.
    - All four are read directly off the reflected `PropertyInfo` at the point of use
      (`target.Info.GetCustomAttribute<...>()`), the same non-cached pattern `RenderExpandedGroup`
      already uses for a nested group member's `[Display(Name=...)]` label — not threaded through
      `WizardPropertySchema`'s cache, since that cache only covers *top-level* model properties
      today. **Known gap, not fixed this pass:** a scalar list row's `FieldTarget` targets its
      `ListItemBox<TItem>.Value` wrapper property, not the original `List<T>` property's own
      `PropertyInfo` — so `[DataType]`/`[Editable]` declared on a `List<string>` property itself
      does not propagate to each repeated row. Complex list items are unaffected (their fields are
      ordinary property-owned targets on the real item instance).
31. **`[FormLabel(LabelPosition)]` (label position) and `FieldAttributes` (arbitrary HTML splat) —
    shipped together as one batch, since they share one plumbing mechanism (#142/#143).**
    - `LabelPosition` (`Above`/`Left`/`Inline`/`Hidden`), overridable per-property via
      `[FormLabel]` against a wizard-level `DefaultLabelPosition` default (attribute wins, same
      pattern as every other per-field override here). `Above`/`Left` keep a real, visible
      `<label>` — `Left` only changes layout (CSS grid instead of flex, so the row's error message
      can still span the full width below both). `Inline`/`Hidden` render no `<label>` element at
      all: dropping it for `Hidden` would leave the input with no accessible name, so the label
      text moves onto the input itself instead (`placeholder`/`aria-label` respectively).
    - `FieldAttributes` (`Dictionary<string, IReadOnlyDictionary<string, object>>`, top-level
      property name → attribute bag) splats arbitrary extra HTML onto one named field — same
      top-level-only reach `[DependsOn]`/`[FormSelect]` already have (B.6), not an oversight.
    - Both funnel through one new `FieldTarget.ExtraAttributes` field, merged once per top-level
      field (a consumer-supplied `aria-label`/`placeholder` always wins over one `LabelPosition`
      would otherwise synthesize). **Hit and worth remembering:** splatting onto a built-in
      `InputBase<TValue>`/`InputFile` component must use `builder.AddMultipleAttributes(seq,
      dict)`, never `builder.AddAttribute(seq, "AdditionalAttributes", dict)` — the latter throws
      at runtime ("cannot be set explicitly when also used to capture unmatched values") because
      those components declare `AdditionalAttributes` with `CaptureUnmatchedValues = true`, and
      Blazor's parameter binder already auto-captures any unmatched attribute name into it; naming
      the parameter directly conflicts with that auto-capture. **Known scope limit:** does not
      reach a nested group member, a repeating list row (both construct `FieldTarget` via a 3-arg
      overload that leaves `ExtraAttributes` null), or a consumer's own `FieldRenderers`-registered
      component (tier 1) — an arbitrary consumer component has no guaranteed attribute to receive
      it, and would throw the same way a built-in component throws for an attribute it doesn't
      declare and doesn't capture.
32. **`[Display(Prompt = "...")]` sets the placeholder — reused from stock DataAnnotations rather
    than inventing a new attribute (same-day follow-up to item 31, #142).** `DisplayAttribute`
    already has a `Prompt` field built for exactly this (HTML placeholder/watermark text) —
    consistent with this engine's habit of reaching for a stock attribute first (`Display.Name` →
    label, `DataType` → input type, `DisplayFormat` → read-only format) before inventing a
    BlazorAtoms-specific one. Cached on `WizardPropertySchema.Placeholder`, read off the same
    `display` variable `WizardModelSchema.Build` already fetches for `Label` — zero extra
    reflection. Applies **regardless of `LabelPosition`**, unlike the `Inline`-position label-text
    fallback: a visible label above a field and a placeholder hint inside it are not mutually
    exclusive (e.g. label "Email" + placeholder "e.g. jane@example.com"). Precedence, via three
    `Dictionary.TryAdd` calls in order inside `BuildExtraAttributes`: consumer `FieldAttributes`
    beats `Prompt` beats the `Inline` label-text fallback. Same known scope limit as item 31 (no
    reach into nested groups/list rows/tier-1 registered components).
33. **`[FormOrder(int)]` duplicates `[Display(Order=N)]` — a genuine miss caught by the user, fixed
    with a non-breaking fallback, `[FormOrder]` marked for future removal.** C.10 explains why field
    order needs to be an explicit, separate concept from `[FormStep]` at all (reflection property
    enumeration isn't stable across an inheritance hierarchy) — it does not explain why a *new*
    attribute was invented instead of reusing `DisplayAttribute.Order`, which already exists for
    exactly this purpose. This predates the "reuse stock DataAnnotations first" pattern items
    29–32 above established; it should have followed the same pattern from the start.
    - Fix: `WizardModelSchema.Build` now reads `formOrder?.Order ?? display?.GetOrder() ??
      int.MaxValue` — `[FormOrder]` still wins when present, but `[Display(Order=N)]` alone now
      works with no `[FormOrder]` needed. Non-breaking: every existing `[FormOrder(N)]` usage
      across this package's own playgrounds is unaffected.
    - **Gotcha confirmed by a throwaway script before relying on it, not assumed:**
      `DisplayAttribute.Order`'s getter throws `InvalidOperationException` ("The Order property has
      not been set. Use the GetOrder method...") when never explicitly set on that attribute
      instance — `GetOrder()` (a method, returning `int?`) is the null-safe way to read it. `Label`/
      `Placeholder` don't have this hazard (`display?.Name`/`display?.Prompt` are plain `string?`),
      which is why `Order` alone needed the different accessor.
    - `[FormOrder]` is deliberately *not* removed this pass — kept for backward compatibility with
      existing consumers, its doc comment now states it's a candidate for removal in a future major
      version. Don't delete it without a separately-scoped decision to do so.
34. **Cancel/close affordance — shipped (G.26, #137): `ShowCancelButton` + `OnWizardCancel`.**
    `ShowCancelButton` (bool, default `false`, opt-in — no existing consumer's nav row changes) and
    `OnWizardCancel` (`EventCallback<TModel>`), both plain component parameters on
    `DynamicWizard<TModel>`, not attributes (there's no per-property meaning for "cancel"). Clicking
    Cancel invokes the callback with `Model` immediately — **no call into
    `WizardNavigator.ValidateCurrentStep`**, unlike Next/Submit, since Cancel is meant to abandon
    the flow rather than complete it: it must work even when the current step is invalid. No
    built-in confirmation dialog, consistent with this engine never owning modal UI elsewhere
    (`[FormDynamicSelect]`'s pending state is a disabled placeholder, not a spinner/dialog) — a
    consumer wanting "are you sure?" wraps their own confirm around the callback. Rendered inside a
    new `.wizard__nav-group` wrapper alongside Back (not as a bare third child of `.wizard__nav`),
    since that container's `justify-content: space-between` is built for exactly two things to space
    apart; a bare third child would land in a visually wrong middle position instead of beside Back.
35. **Draft-save/resume — shipped (G.23, #134): `InitialStep`/`CurrentStep`/`OnStepChanged`, no
    storage owned by this engine.** `Model` was already the consumer's own object reference and
    plain-JSON-serializable end to end (`WizardFileAttachment` holds `byte[]`, not a stream) — the
    only state missing to resume mid-flow was *which step to land on*, since
    `WizardNavigator`'s constructor always started at the first declared step. Fix: a 3rd,
    optional ctor param `int? initialStep` — used when it's a real declared step number for the
    schema, falling back to the existing default otherwise (a stale snapshot from a since-changed
    schema degrades to "start over," not a crash or a nonexistent step). `DynamicWizard<TModel>`
    exposes this as `InitialStep` (a `[Parameter]`), plus a get-only `CurrentStep` property (not a
    parameter — a consumer reads it via `@ref`) and an `OnStepChanged` callback (`EventCallback<int>`,
    fires only when `HandleNext`/`HandlePrevious` actually change the step, not on every call —
    both boundary no-ops and validation-blocked `Next` must not fire it).
    **Deliberately not built:** any actual storage I/O, and any field-level/keystroke autosave
    hook — this engine stays a 0-dep leaf (A.1); a consumer wanting that granularity already owns
    `Model` and can serialize it on their own cadence without a per-edit callback from here.

## Scenarios walked through (reference — the concrete cases that drove the decisions above)

1. **Two-branch fork converging on a shared step.** Step 1 = choice A/B; step 2 fields depend on
   A; step 3 fields depend on B; step 4 fields are unconditional. Traced both paths (1→2→4 for A,
   1→3→4 for B) including back-navigation correctly skipping the untaken branch. Confirmed C.8.
2. **Account-type wizard — unconditional/conditional/unconditional.** Step 1 = account type;
   step 2 = customer info (unconditional, shown to all); step 3 = manager-account fields
   (conditional on account type = Manager); step 4 = validate/submit (unconditional). Personal
   path's true length is 3 steps, Manager path's is 4 — different lengths, both correct under the
   dynamic-recompute rule (C.9). Diagrammed in `FLOW.md` diagram 3.
3. **Financial application → async response.** Step 4 collects a financial application; step 5
   is meant to display a response from an external finance system before a final submit. Surfaced
   the need for a step-transition async action hook (G.22) — explicitly deferred, not designed.
4. **File upload as a field type.** "0 or more files" per step. Worked through the binding-contract
   mismatch (E.14), the storage decision (E.15), and the one async field-handler exception (E.16).
5. **`Money` custom-type registry.** A value type needing one specialized widget, not
   auto-expansion into two fields. Drove the A.3/A.4 registry design; full worked example lives in
   `EXTENSIBILITY.md`.
6. **A branch missing a `DependsOn` by mistake.** Model: step 1 picks Branch A/B; step 2 is Branch
   A's field (also marked `[FormPathEnd]`); step 3 is Branch B's field; step 4 has **no
   `DependsOn` at all** — simulating a consumer forgetting to gate a field meant only for Branch B.
   Without a marker, Branch A would silently walk into step 4 (no condition = always visible).
   With `[FormPathEnd(nameof(Selection), Branch.A)]` on step 2, Branch A correctly stops there —
   `EffectiveStepNumbers()` returns `[1, 2]`, never `[1, 2, 4]` — while Branch B (which declares no
   marker of its own) still reaches the accidentally-unconditional step 4, proving the fix is
   targeted at the branch that declares it, not a blanket patch over every mistake. Drove C.8a.
7. **A skipped `[FormStep]` number.** A model declaring only `[FormStep(1)]` and `[FormStep(3)]`
   (no property anywhere uses `2`). Confirmed inert: `schema.Steps` simply never contains an entry
   for `2`; navigation walks the *declared* numbers directly rather than incrementing blindly, so
   it lands on step 3 straight from step 1; display position/count never references the gap
   ("Step 2 of 2," never "3 of 2"). No different from renumbering to be contiguous.
8. **Two properties sharing one `[FormStep]` number with conflicting titles.** Confirmed this is
   the normal multi-field-step mechanism (not an error case), and that a title collision resolves
   deterministically via the same `(FormOrder, encounter-order)` tie-break as field render order —
   see decision 9's collision-rule note.

## Not yet decided (pick up here in a future session)

- Whether the future render-adapter package (A.1) is a single package or split further by target
  UI kit (e.g. a `BlazorAtoms.DynamicFormWizard.Atoms` adapter vs. others).
- Exact shape of the async step-transition action hook (G.22) once it's actually designed — this
  doc only records that it's needed and why, not its API.
