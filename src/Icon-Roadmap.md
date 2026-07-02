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

| Library | Symbol | Z | Label | Real element? |
|---|---|---|---|---|
| Inputs | In | 49 | INPUTS | Indium ✓ |
| Cards | Cd | 48 | CARDS | Cadmium ✓ |
| Tabs | Tb | 65 | TABS | Terbium ✓ |
| Panels | Pa | 91 | PANELS | Protactinium ✓ |
| Alerts | Al | 13 | ALERTS | Aluminium ✓ |
| Ratings | Ra | 88 | RATINGS | Radium ✓ |
| Layout | La | 57 | LAYOUT | Lanthanum ✓ |
| Tables | Ta | 73 | TABLES | Tantalum ✓ |
| Progress | Pr | 59 | PROGRESS | Praseodymium ✓ |
| BusyIndicators | Bi | 83 | BUSY | Bismuth ✓ |
| Behaviors | Bh | 107 | BEHAVIOR | Bohrium ✓ |
| Transitions | Ts | 117 | MOTION | Tennessine ✓ (or "Tr", coined) |
| Barcodes | Bc | 56 | BARCODES | coined · Z from Ba (Barium) |
| Skeletons | Sk | 21 | SKELETON | coined · Z from Sc (Scandium) |
| Charts | Ch | 6 | CHARTS | coined · Z from C (Carbon) |
| Badges | Bg | 97 | BADGES | coined · Z from Bk (Berkelium) |
| Avatars | Av | 47 | AVATARS | coined · Z from Ag (Silver) |
| Buttons | Bt | 5 | BUTTONS | coined · Z from B (Boron) |
| Tooltips | Tt | 81 | TOOLTIPS | coined · Z from Tl (Thallium) |
| Icons | Ic | 53 | ICONS | coined · Z from I (Iodine) |
| Typography | Ty | 22 | TYPE | coined · Z from Ti (Titanium) |
| Overlays | Ov | 8 | OVERLAYS | coined · Z from O (Oxygen) |
| Pickers | Pk | 78 | PICKERS | coined · Z from Pt (Platinum) |
| Navigation | Nv | 11 | NAV | coined · Z from Na (Sodium) |

**Z (atomic number):** real-element symbols use the element's own number; coined symbols borrow the
nearest real element's Z — the alt where one fits, else the closest symbol match. No Z collides.
**Label** is the short tile-bottom text (≤~8 chars, so it fits the tile width); the full library name
stays the symbol's element name / wordmark. `assets/logo-playground.html`'s element presets mirror
this table exactly (`[library, symbol, Z, label]`) — **keep the two in sync**.

Story: **the shape is the format, the color is the group, the symbol is the element.**

## Production workflow

1. Design the **one master tile** (fixed chrome + slots) in `assets/logo-playground.html`.
2. For each library, swap the variables (symbol, name, color, optional glyph) → keeps tiles
   pixel-consistent.
3. Save the vector master: `assets/icons/blazoratoms-<lib>-tile.svg` (see **File naming convention** below).
4. Export a **128×128 PNG**: `assets/icons/blazoratoms-<lib>-tile-128x128.png` — the one sanctioned
   raster (NuGet requires PNG/JPG for `PackageIcon`), consistent with the Graphics policy.
5. Packaging: either override `<PackageIcon>` + the icon `None` include per library csproj, **or**
   parameterize `build/Packable.props` to derive the icon filename from `$(PackageId)` (shared
   default, per-library override). Prefer the parameterized approach so new libraries pick up their
   icon by convention.

## File naming convention

All per-library icon assets follow:

```
blazoratoms-<lib>-<variant>[-<WxH>].<ext>
```

- **`<lib>`** — the PackageId minus the `BlazorAtoms.` prefix, lowercased
  (`BlazorAtoms.Barcodes` → `barcodes`). Filenames key on the **library name, not the element
  symbol** — symbols aren't locked yet, aren't self-descriptive, may collide across libraries, and
  would force a lib→symbol map in MSBuild (defeating the `$(PackageId)`-derived packaging in step 5).
  The element symbol lives *inside* the SVG payload, never in the filename.
- **`<variant>`** — which rendition (table below).
- **`-<WxH>`** — only on raster exports (e.g. `-128x128`). SVG masters carry no size token.

| Variant | File (Barcodes example) | Purpose |
|---|---|---|
| `tile` | `blazoratoms-barcodes-tile.svg` | the periodic-table tile — vector master, **canonical** |
| `tile` (raster) | `blazoratoms-barcodes-tile-128x128.png` | sanctioned 128×128 PNG → `PackageIcon` |
| `mark` | `blazoratoms-barcodes-mark.svg` | symbol-only glyph (favicon / app / avatar use) |
| `lockup` | `blazoratoms-barcodes-lockup.svg` | horizontal: mark + wordmark |
| `stacked` | `blazoratoms-barcodes-stacked.svg` | vertical: mark over wordmark |

Rules:
- **`tile` is the only required variant** (it is the `PackageIcon`); `mark` / `lockup` / `stacked`
  are optional — add each when a consumer actually needs it.
- **SVG-first** (Graphics policy): only `tile` gets a PNG, because NuGet requires raster. Don't
  pre-render `mark` / `lockup` / `stacked` to PNG until something demands it.
- All per-library assets live under `assets/icons/`. The umbrella/org icon stays at
  `assets/blazoratoms-icon-128x128.png` (unchanged).
- Packaging derives the icon by convention: `blazoratoms-$(lib)-tile-128x128.png`, so
  `$(PackageId)` alone selects it with no per-library config.

## Open decisions

- [ ] Finalize the category palette (hex values) in the playground.
- [ ] Confirm each element symbol (lock the real-element ones; decide coined vs alt for the rest).
- [ ] Include a per-library glyph, or keep symbol-only?
- [x] Atomic number = real element Z (coined symbols borrow the nearest real element's Z; see the
      table). Still open: swap to a build-sequence numbering instead, if preferred.
- [ ] `Packable.props` mechanism: per-csproj override vs `$(PackageId)`-derived.
- [ ] Keep the current `assets/blazoratoms-icon-128x128.png` as the umbrella/org icon; per-library
      tiles for each package.

> Status: design roadmap — not yet built. The current packages still use the single shared
> `blazoratoms-icon-128x128.png` until per-library tiles are produced.
