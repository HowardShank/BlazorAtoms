# Playground conventions

The playgrounds are the **living documentation** for every BlazorAtoms component. Each
`*PlaygroundView.razor` here has two non-negotiable jobs:

## Goal 1 — wire **every** parameter

A playground must expose a live control for **every public parameter** of the component(s) it
demonstrates — nothing hidden, nothing hard-coded. The playground is how a user discovers what the
component can do, so an unwired parameter is an undiscoverable feature.

- One control per parameter, using the file's existing control markup (`.tt-ctrl` inside
  `.tt-controls`, or the file's local equivalent).
- Pick the control type by parameter type: `bool` → checkbox, enum → `<select>` over
  `Enum.GetValues<T>()`, numeric → `<input type="number">`, color/string that must express "unset"
  → **text input** (not `<input type="color">`, which can't represent null), string → text input.
- "Unset" must be expressible. Route optional string params through a null-if-empty helper
  (`Blank(...)` / `Nz(...)`) so blank ⇒ `null` ⇒ the component's own default, matching real usage.
- This includes the inherited escape hatch on **every** component
  (`AtomComponentBase`): `CssClass`, `Style`, and the attribute splat (demoed with a `title` input).
  See the repo-wide styling notes for `ClassAttr`/`StyleAttr`/`AdditionalAttributes`.
- When a view demonstrates several components at once (e.g. Chip/Tag/Pill), shared controls drive all
  of them; component-unique params are labelled with the component they affect.

## Goal 2 — emit a valid copy/paste sample

Every playground ends with a `<CodeSnippetBox Code="@Snippet" FileName="...Example.razor" />`. The
`Snippet` getter must produce a **valid, runnable `.razor` fragment** the user can paste into their own
project as-is.

- Emit **only non-default** attributes (guard each against its default) so the snippet stays minimal
  yet reproduces exactly what the controls show.
- The snippet must compile against the component's real public API — same param names, same value
  syntax (`Variant="BadgeVariant.Info"`, `Size="40"`, quoted strings, etc. — note enum types are
  package-prefixed: `BadgeVariant`, `ButtonVariant`, `InputVariant`).
- `CodeSnippetBox` gives Copy-to-clipboard + Save-as-`.razor`.

## Adding / changing a component

When you add a public parameter to a component, you must in the same change:
1. add its control to the relevant playground view, and
2. extend that view's `Snippet` getter to emit it (guarded against default).

New components follow the full wiring checklist (view + 3 wrapper pages + 3 NavMenu entries + 3
`ActivityIndicators.razor` links); the two goals above are the acceptance bar for the view itself.
