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
4. **Field-render dispatch, four tiers in priority order:**
   1. Consumer's type-registry match for this exact property type (explicit opt-in always wins).
   2. Built-in scalar types (bool/enum/string/DateTime/int·decimal·double/file).
   3. Auto-expand — the property's type is itself a complex class with its own public
      read/write properties, so it recurses and renders *those* as a field group (see B.5).
   4. Fallback — read-only `ToString()` rendering plus a dev-time warning, so an unhandled type
      never silently disappears from the form.
   **Why:** each tier solves a distinct problem (explicit override, common case, structural
   grouping, safety net) — collapsing any two would either lose the escape hatch or lose the safe
   fallback for genuinely unknown types.

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
11. **`[DependsOn]` stays equality-only, AND-combined, stackable, for v1.** No OR/NotEquals/range
    yet. **Why:** deliberately deferred (G.28) until the user designs real test scenarios of their
    own — not a gap in the reasoning, a conscious "not yet."

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
23. **Draft-save/resume** (serializing `Model` + file DTOs mid-wizard, so a user can leave and come
    back later).
24. **i18n** of labels/messages — currently hardcoded English throughout `Ideas.md` (both
    `[Display(Name=...)]` text and custom-validator error messages).
25. **Repeating/collection steps** (e.g. "add N beneficiaries," a variable-length list of
    sub-objects within one step) — no `Ideas.md` iteration touches this at all.
26. **A cancel/close affordance** distinct from Back — no iteration has one.
27. **Nested-target `DependsOn`** (path-based targeting into a group's own fields — see B.6).
28. **Richer `DependsOn` operators** (OR, NotEquals, range/comparison — see C.11).

*(A hard "force-stop here" override was considered deferred at one point in this doc's history —
reconsidered and built once a concrete failure mode was raised; see C.8a, no longer deferred.)*

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

- Exact `[DependsOn]` operator set beyond equality (G.28) — waiting on the user's own test
  scenarios.
- Whether the future render-adapter package (A.1) is a single package or split further by target
  UI kit (e.g. a `BlazorAtoms.DynamicFormWizard.Atoms` adapter vs. others).
- Exact shape of the async step-transition action hook (G.22) once it's actually designed — this
  doc only records that it's needed and why, not its API.
