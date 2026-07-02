# Marks

Drop the generated family-set `.razor` files here.

**Source:** `assets/logo-playground.html` → **Download family set (26)** button. Produces:

- `AtomMark.razor` — the shared render engine (SVG builder + all parameters).
- `AtomsMark.razor` + `Atom<Lib>Mark.razor` ×24 — one brand mark per library, thin wrappers
  over the engine (baked payload: symbol / number / label / colors; chrome inherited).

Keep all 26 together — the wrappers need `AtomMark` in scope. They inherit this folder's
namespace `BlazorAtoms.Branding.Marks` (already `@using`-ed in `_Imports.razor`).

The marks are standalone `ComponentBase` (zero-dep, portable) **by design** — a deliberate
exception to the "everything derives `AtomComponentBase`" realm rule, since brand marks share
no behavior. This keeps the in-repo files byte-identical to the playground export.

Usage from a demo (after the `ProjectReference` to `BlazorAtoms.Branding`):

```razor
@using BlazorAtoms.Branding.Marks

<AtomBarcodesMark />
<AtomBarcodesMark Layout="AtomMark.MarkLayout.Lockup" Size="64" />
<AtomsMark Layout="AtomMark.MarkLayout.Stacked" Size="96" />
```
