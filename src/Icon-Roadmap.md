# BlazorAtoms — Icon System Roadmap

How package icons work across the family. Goal: **per-library icons that are visually distinct
(good NuGet discovery) yet unmistakably one family** — a "periodic table of Blazor atoms."

Authoring tool: **`assets/logo-playground.html`** — design and tune each tile SVG there, then export.

---

## Decision: per-library icons, not a single shared one

On nuget.org, icons render tiny and side-by-side; one generic "Atoms" icon makes every package look
identical in search results. The periodic-table tile is already a design system, so per-library
tiles give us distinct + unified at once. Each library is rendered as an **element**.

## The system: freeze the chrome, vary the payload

**Constant across every tile (the family signature):**
- Same tile silhouette, corner radius, stroke weight, inner padding, layout grid.
- Same slots in the same positions: atomic number (top-left), big element symbol (center),
  library name (bottom).
- Same font and weights.
- A shared brand stamp — the flame mark from the master logo in a corner, or the master icon as a
  faint nucleus watermark. (The master orange/flame identity is reserved for the umbrella
  "BlazorAtoms" tile; per-library tiles use category hues below.)

**Variable per library:**
- **Symbol** — a 1–2 letter "element symbol" (table below).
- **Color** — the accent (tile fill / glow / border), driven by category (below).
- Optional small **glyph** of what the library does (barcode stripes, spinner, bars), in a fixed spot.

## What makes it a *family* beyond the shape

**1. Color encodes category** (like real periodic groups) — hue is assigned by function, not at random:

| Category | Libraries |
|---|---|
| Feedback & status | BusyIndicators, Progress, Skeletons, Alerts |
| Data & viz | Charts, Barcodes, Ratings |
| Forms & input | Inputs, Buttons, Pickers |
| Layout & containers | Layout, Cards, Tabs, Panels, Tables |
| Content & identity | Typography, Icons, Avatars, Badges |
| Overlay & behavior | Overlays, Tooltips, Navigation, Behaviors, Transitions |

(Exact palette TBD in the playground; keep the master flame/orange for the umbrella tile.)

**2. Element symbols — prefer real periodic symbols where they fit.** This is the brand hook.

| Library | Symbol | Real element? |
|---|---|---|
| Inputs | In | Indium ✓ |
| Cards | Cd | Cadmium ✓ |
| Tabs | Tb | Terbium ✓ |
| Panels | Pa | Protactinium ✓ |
| Alerts | Al | Aluminium ✓ |
| Ratings | Ra | Radium ✓ |
| Layout | La | Lanthanum ✓ |
| Tables | Ta | Tantalum ✓ |
| Progress | Pr | Praseodymium ✓ |
| BusyIndicators | Bi | Bismuth ✓ |
| Behaviors | Bh | Bohrium ✓ |
| Transitions | Ts | Tennessine ✓ (or "Tr", coined) |
| Barcodes | Bc | coined (Ba = Barium as an alt) |
| Skeletons | Sk | coined |
| Charts | Ch | coined (C = Carbon) |
| Badges | Bg | coined |
| Avatars | Av | coined |
| Buttons | Bt | coined (B = Boron) |
| Tooltips | Tt | coined (Tl = Thallium as an alt) |
| Icons | Ic | coined (I = Iodine) |
| Typography | Ty | coined |
| Overlays | Ov | coined (O = Oxygen) |
| Pickers | Pk | coined |
| Navigation | Nv | coined (Na = Sodium as an alt) |

Story: **the shape is the format, the color is the group, the symbol is the element.**

## Production workflow

1. Design the **one master tile** (fixed chrome + slots) in `assets/logo-playground.html`.
2. For each library, swap the variables (symbol, name, color, optional glyph) → keeps tiles
   pixel-consistent.
3. Save the vector master: `assets/icons/blazoratoms-<lib>.svg`.
4. Export a **128×128 PNG**: `assets/icons/blazoratoms-<lib>-128x128.png` — the one sanctioned
   raster (NuGet requires PNG/JPG for `PackageIcon`), consistent with the Graphics policy.
5. Packaging: either override `<PackageIcon>` + the icon `None` include per library csproj, **or**
   parameterize `build/Packable.props` to derive the icon filename from `$(PackageId)` (shared
   default, per-library override). Prefer the parameterized approach so new libraries pick up their
   icon by convention.

## Open decisions

- [ ] Finalize the category palette (hex values) in the playground.
- [ ] Confirm each element symbol (lock the real-element ones; decide coined vs alt for the rest).
- [ ] Include a per-library glyph, or keep symbol-only?
- [ ] Meaning of the atomic number — sequence/build order, or decorative/omit?
- [ ] `Packable.props` mechanism: per-csproj override vs `$(PackageId)`-derived.
- [ ] Keep the current `assets/blazoratoms-icon-128x128.png` as the umbrella/org icon; per-library
      tiles for each package.

> Status: design roadmap — not yet built. The current packages still use the single shared
> `blazoratoms-icon-128x128.png` until per-library tiles are produced.
