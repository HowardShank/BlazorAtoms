# BlazorAtoms.DynamicFormWizard — Flow Diagrams

Visual companion to `DESIGN-DISCUSSION.md`. Each diagram is captioned with the decision(s) it
documents — read the linked section there for the full rationale.

## 1. Package / architecture split

Documents `DESIGN-DISCUSSION.md` A.1–A.4: the headless core stays a 0-dep leaf; a future adapter
package is the only thing that ever depends on `BlazorAtoms.Inputs`. `FieldTemplate` and the
type-registry are both shown as seams on the core, not hard edges.

```mermaid
flowchart LR
    subgraph Core["BlazorAtoms.DynamicFormWizard (headless, 0-dep)"]
        Engine["Step / DependsOn engine\n(partial validation, dynamic step count)"]
        NativeRender["Native-input fallback renderer\n(bare CSS)"]
        Seam1["FieldTemplate seam\n(whole-form override)"]
        Seam2["Type-registry seam\n(Dictionary&lt;Type,Type&gt;, single-type override)"]
        Engine --> NativeRender
        Engine -.-> Seam1
        Engine -.-> Seam2
    end

    subgraph Adapter["Future: render-adapter package"]
        AtomRender["Atom*-styled field renderer"]
    end

    subgraph Inputs["BlazorAtoms.Inputs"]
        AtomTextField
        AtomSelect
    end

    subgraph ConsumerCode["Consumer's own code"]
        MoneyInput["e.g. MoneyInput.razor\n(see EXTENSIBILITY.md)"]
    end

    Adapter -- "ProjectReference" --> Core
    Adapter -- "ProjectReference" --> Inputs
    Seam1 -. "optional swap" .-> AtomRender
    Seam2 -. "optional swap, one type at a time" .-> MoneyInput
```

## 2. Engine navigation algorithm

Documents `DESIGN-DISCUSSION.md` C.8–C.9: `GoNext` (and `GoPrevious`, mirrored) walk the raw step
counter and skip any step number with zero currently-visible properties — this is the *entire*
mechanism behind both branching and rejoining. No graph structure exists anywhere in the engine.

```mermaid
flowchart TD
    Start(["User clicks Next"]) --> Validate{"Current step's\nvisible properties valid?"}
    Validate -- "No" --> ShowErrors["Show errors, stay on step\n(D.12: per-step, not whole-model)"]
    Validate -- "Yes" --> Advance["step = step + 1\n(raw FormStep int)"]
    Advance --> HasVisible{"Any properties visible\nfor this step number?\n(DependsOn evaluated live)"}
    HasVisible -- "No" --> AtMax{"step reached the highest\ndeclared FormStep number?"}
    AtMax -- "No" --> Advance
    AtMax -- "Yes" --> Land["Land here anyway\n(nothing further to skip to)"]
    HasVisible -- "Yes" --> Land2["Land on step, render it\n(C.9: label + position\nrecomputed, never the raw int)"]

    style ShowErrors fill:#5b2333,color:#fff
    style Land2 fill:#1f4d3d,color:#fff
```

## 3. Concrete branch/rejoin example — account-type wizard

Documents `DESIGN-DISCUSSION.md` scenario 2 and decision C.9 (dynamic step count differs per
branch — 3 for Personal, 4 for Manager — despite both sharing the same declared `[FormStep]`
numbers 1–4).

```mermaid
flowchart TD
    S1["Step 1: Account Type\n(unconditional)"]
    S2["Step 2: Customer Info\n(unconditional — shown to both paths)"]
    S3["Step 3: Manager Account fields\nDependsOn(AccountType, Manager)"]
    S4["Step 4: Validate + Submit\n(unconditional)"]

    S1 -->|"AccountType = Personal"| S2
    S1 -->|"AccountType = Manager"| S2
    S2 -->|"Personal: step 3 empty, skipped"| S4
    S2 -->|"Manager: step 3 has visible fields"| S3
    S3 --> S4

    NotePersonal["Personal path visible steps: {1,2,4}\nEffective count = 3\n'Step 2 of 3', 'Step 3 of 3' (never 'of 4')"]
    NoteManager["Manager path visible steps: {1,2,3,4}\nEffective count = 4\n'Step 3 of 4' shown correctly on step 3"]

    S4 -.-> NotePersonal
    S3 -.-> NoteManager

    style S3 fill:#3a2f5b,color:#fff
    style NotePersonal fill:#233b5b,color:#fff
    style NoteManager fill:#233b5b,color:#fff
```

## 4. Field-render dispatch priority

Documents `DESIGN-DISCUSSION.md` A.4 — the four tiers, checked in order for every property.

```mermaid
flowchart TD
    Prop(["Property to render"]) --> T1{"1. Consumer type-registry\nhas an entry for this exact type?"}
    T1 -- "Yes" --> UseRegistry["Open the registered component\n(e.g. MoneyInput for Money)\nsame OpenComponent pattern as built-ins"]
    T1 -- "No" --> T2{"2. Known built-in scalar type?\n(bool / enum / string / DateTime /\nint·decimal·double / file)"}
    T2 -- "Yes" --> UseBuiltIn["Render via the built-in\nnative-input branch"]
    T2 -- "No" --> T3{"3. Complex class with its own\npublic read/write properties?"}
    T3 -- "Yes" --> AutoExpand["Auto-expand: recurse into its\nproperties as a field group\n(TryValidateObject, per B.5)"]
    T3 -- "No" --> Fallback["4. Fallback: read-only ToString()\n+ dev-time warning\n(never silently disappears)"]

    style UseRegistry fill:#1f4d3d,color:#fff
    style Fallback fill:#5b2333,color:#fff
```
