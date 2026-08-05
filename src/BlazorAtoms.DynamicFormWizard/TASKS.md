# BlazorAtoms.DynamicFormWizard — Task ↔ Design-Doc Correlation

The `#N` identifiers below are the task-tracker IDs used in conversation while building this
package. They exist **only** in the tracker (and in chat history) — nothing on disk cross-referenced
them until this file. This is the missing mapping: task # → status → which `DESIGN-DISCUSSION.md`
section/item explains the *design* behind it. Update this table whenever a task's status changes or
a new task is added — it's the single searchable source for "what is #167" or "where is G.30
documented," so it needs to stay accurate, not just written once.

Deferred/design-only work is tracked under its **G.NN** label first (assigned when the idea was
raised), then split into implementation sub-tasks (`#163`–`#172` etc.) once someone starts building
it. Not every G.NN has sub-tasks yet — some are still just a paragraph in section G waiting for that
split to happen.

## Status legend

- **done** — shipped, merged into the engine, covered by tests.
- **pending** — not started.
- **design-only** — fully speced in `DESIGN-DISCUSSION.md`, explicitly not to be implemented until
  told to proceed.

## Engine build-out (#121–#131)

| # | Status | What | Design ref |
|---|--------|------|-------------|
| 121 | done | Core attributes, enums, custom validators | — |
| 122 | done | `WizardModelSchema` reflection/caching engine | F.19 |
| 123 | done | Navigation + partial-validation engine | D.12–13 |
| 124 | done | `DynamicWizard<TModel>` component | — |
| 125 | done | File upload support | E.14–16 |
| 126 | done | `FormSelect`/`FormDynamicSelect` dropdown rendering | F.20 |
| 127 | done | `FormLayout` bare-CSS grid | F.21 |
| 128 | done | bUnit test suite | — |
| 129 | done | Sample model + playground + docs/catalog wiring | — |
| 130 | done | 4 new playgrounds (types/custom/multipath/file) | — |
| 131 | done | Visually verify 4 new playgrounds in browser | — |

## Deferred/future-enhancement tasks (section G)

| # | Status | G-label | What | Design ref |
|---|--------|---------|------|-------------|
| 132 | pending | — | Render-adapter package (Inputs-styled fields) | A.1 |
| 133 | pending | G.22 | Step-transition async action hook | G.22 |
| 134 | done | G.23 | Draft-save/resume (`InitialStep`/`CurrentStep`/`OnStepChanged`) | H.35 |
| 135 | pending | G.24 | i18n of labels/messages | G.24 |
| 136 | done | G.25 | Repeating/collection steps (`List<T>`) | G.25, A.4 tier 1b |
| 137 | done | G.26 | Cancel/close affordance | H.34 |
| 138 | done | G.27 | Nested-target `DependsOn` | Section M |
| 139 | done | G.28 | Richer `DependsOn` comparison operators | C.11 |
| 140 | pending | — | Full ARIA/a11y audit pass | F.17 |

## DataAnnotations + rendering-hook batch (section H, #141–#162)

| # | Status | What | Design ref |
|---|--------|------|-------------|
| 141 | done | Honor standard display-formatting attributes | H.30 |
| 142 | done | Label position indicator (`[FormLabel]`) | H.31 |
| 143 | done | Splat arbitrary HTML attributes onto rendered fields | H.31 |
| 144 | done | `IParsable<T>` generic scalar tier | A.4 tier 2b |
| 145 | done | AllTypes playground — full C# type set | — |
| 146 | done | Tests + docs for `IParsable` scalar tier | A.4 tier 2b |
| 147 | done | Handle `Nullable<T>` value types | A.4a |
| 148 | done | Fix indexer-property crash in auto-expand/`IsComplexType` | A.4 tier 3 |
| 149 | done | Exclude collection types from `IsComplexType` | A.4 tier 3 |
| 150 | done | `ComparisonOperator` enum + extend `[DependsOn]` | C.11 |
| 151 | done | List-index `FieldTarget` + `MinItemCount`/`MaxItemCount` validators | G.25 |
| 152 | done | Scalar-list repeater (`List<T>` where `T` scalar) | G.25 |
| 153 | done | Complex-item repeater group (`List<ComplexType>`) | G.25 |
| 154 | done | Playground demo + docs + full solution verify (#136/#139) | G.25, C.11 |
| 155 | done | Prove `[Compare]` already works via test | H.29 |
| 156 | done | `[DataType]`-driven input rendering | H.30 |
| 157 | done | `[DisplayFormat]` honoring in fallback render | H.30 |
| 158 | done | `[Editable(false)]` read-only rendering | H.30 |
| 159 | done | `[ScaffoldColumn(false)]` exclusion | H.30 |
| 160 | done | Proof tests for already-working stock validation attrs | H.29 |
| 161 | done | Docs + playground for DataAnnotations batch | — |
| 162 | done | Build + full test suite verify DataAnnotations batch | — |

## New field types — G.29/30/31, all shipped end to end

| # | Status | G-label | What | Design ref |
|---|--------|---------|------|-------------|
| 163 | done | G.29 | `[FormMatrix]` attribute + schema wiring | Section I, items 1–2 |
| 164 | done | G.29 | `RenderMatrixGrid` dispatch + table markup | Section I, items 3–4 |
| 165 | done | G.29 | Survey matrix tests (render/interaction/validation) | Section I, item 5 |
| 166 | done | G.29 | Survey matrix docs + playground + NavMenu wiring | Section I, item 7 |
| 167 | done | G.30 | `[FormRatingScale]` attribute + tier-2 render hook | Section J, items 2–4 |
| 168 | done | G.30 | Rating-scale tests (render/interaction/validation) | Section J, item 7 |
| 169 | done | G.30 | Rating-scale docs + playground wiring | Section J, item 7 |
| 170 | done | G.31 | `[FormRadioList]` attribute + `InputRadioGroup` render hook | Section K, items 2–4 |
| 171 | done | G.31 | Radio-list tests (render/interaction) | Section K, item 5a, item 7 |
| 172 | done | G.31 | Radio-list docs + playground wiring | Section K, item 7 |

## Decisions with no task # yet

Not every design-doc entry has a tracker task — some are pure decisions with nothing left to build,
or execution work nobody has scoped into tasks yet.

- **Section L — `BlazorComposites` naming/extraction decision.** Naming is decided; actual repo
  extraction is unscoped future work with no task # assigned. Add tasks here once someone commits to
  a timeline for the move.
- **G.22 comparison note, C.8a, C.11's OR-combination gap** — recorded in `DESIGN-DISCUSSION.md` as
  known limitations, not open work items; no task # expected unless someone decides to build them.
- **Matrix "fails silently" fix + `[RequiredUnless]` (section I items 8-9)** — a follow-on bug fix
  and small feature addition to #163-166, done the same day as a direct user report rather than as
  a separately numbered task. `Validators/RequiredUnlessAttribute.cs` is new, reusable outside
  `[FormMatrix]` too.

## Maintenance note

When a task's status changes (started, shipped, or newly split out of a G-label), update its row
here in the same edit that updates the tracker — this file drifting out of sync with the tracker is
exactly the problem it was created to solve.
