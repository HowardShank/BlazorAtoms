# BlazorAtoms

[![.NET](https://github.com/HowardShank/BlazorAtoms/actions/workflows/dotnet.yml/badge.svg)](https://github.com/HowardShank/BlazorAtoms/actions/workflows/dotnet.yml)

A family of small, self-contained Blazor component libraries — **Server, WebAssembly, or Auto** —
each shipped as its own standalone NuGet package with ~0 third-party dependencies.

> **Lightweight, drop-in, no lock-in.** Add just the one library you need and it works: no shared
> runtime dependency, no umbrella package, no design-system framework to buy into, no global
> setup/theme provider, no `builder.Services.Add…()` registration, and no JS bundle to wire up by
> hand. Every component reads its inputs from `[Parameter]`s and its look from CSS variables, so it
> intermixes freely with any application — greenfield, legacy, or alongside a heavier component
> suite (MudBlazor, Radzen, Fluent, Telerik) — without conflict.

## Libraries

| Package | What it is |
|---|---|
| [`BlazorAtoms.ActivityIndicators`](src/BlazorAtoms.ActivityIndicators/README.md) | Animated "working…" loaders — 7 round SVG indicators + 2 linear sliding bars. Pure CSS, no JS. |
| [`BlazorAtoms.Avatars`](src/BlazorAtoms.Avatars/README.md) | Person/entity avatars — image or silhouette, initials fallback, overlapping group stack. |
| [`BlazorAtoms.Badges`](src/BlazorAtoms.Badges/README.md) | `AtomBadge` (count/status badge, many shapes, optional motion) + the chip family: `AtomChip` / `AtomTag` / `AtomPill`. |
| [`BlazorAtoms.Barcodes`](src/BlazorAtoms.Barcodes/README.md) | `AtomBarcode` (1D) and `AtomQrCode` (2D) — generation only, our own C# encoder → SVG, no third-party codec. |
| [`BlazorAtoms.Canvas`](src/BlazorAtoms.Canvas/README.md) | `AtomCanvas` (declarative shape model over `<canvas>`), `AtomSignaturePad`, and `AtomCanvasStudio` (extensible drawing workbench). |
| [`BlazorAtoms.Clocks`](src/BlazorAtoms.Clocks/README.md) | Live clocks (digital/analog), multi-zone strips, a world timezone map, and a searchable timezone picker. |
| [`BlazorAtoms.Highlights`](src/BlazorAtoms.Highlights/README.md) | Keyword/text highlighters: zero-JS for plain text or a trusted HTML string, or a JS-assisted one that works through arbitrarily nested child components. |
| [`BlazorAtoms.Ratings`](src/BlazorAtoms.Ratings/README.md) | `AtomRating` — one component for both a read-only fractional-fill display and an interactive star/heart/etc. picker. |
| [`BlazorAtoms.Tooltips`](src/BlazorAtoms.Tooltips/README.md) | Three tooltip components — pure-CSS bubble, SVG-outlined shape, and SVG gradient-painted shape. |

Each package's own `README.md` (linked above) is its usage documentation — install steps, examples,
and parameter reference. Internal design notes and implementation rationale for a given library live
in that library's own `DEVELOPMENT.md`, if it has one.

For the full roadmap — including libraries not yet built, the JS/graphics policy, and naming
conventions — see [`src/LIBRARY-CATALOG.md`](src/LIBRARY-CATALOG.md).

## Install

Each library is an independent package; take only what you need.

```xml
<PackageReference Include="BlazorAtoms.Badges" Version="0.1.0" />
```

Or, from a checkout of this repo:

```xml
<ProjectReference Include="..\BlazorAtoms\src\BlazorAtoms.Badges\BlazorAtoms.Badges.csproj" />
```

Then add the namespace where you use it:

```razor
@using BlazorAtoms.Badges
```

Most libraries are pure CSS/SVG (nothing else to configure). The few that ship JavaScript
self-import their own module on first use — no `<script>` tag and no DI registration needed. See
the library's own README for anything render-mode-specific.

## Repository layout

```
src/                     the libraries themselves (one folder per BlazorAtoms.<Area> package)
tests/                   one bUnit test project per library
samples/Demos.Shared/    shared playground pages, rendered by all three demo hosts below
BlazorWebAppSvrDemo/     demo host — Blazor Server
BlazorWebAppWasmDemo/    demo host — Blazor WebAssembly (standalone)
BlazorWebAppAutoDemo/    demo host — Blazor Auto (Server + WASM)
branding/                the BlazorAtoms brand marks (per-library icons used in the demo nav)
```

## Build & test

```bash
dotnet restore
dotnet build BlazorAtoms.sln
dotnet test
```

Or build/test a single library:

```bash
dotnet build src/BlazorAtoms.Badges/BlazorAtoms.Badges.csproj
dotnet test tests/BlazorAtoms.Badges.Tests/BlazorAtoms.Badges.Tests.csproj
```

## Demos

Every component has a live, interactive playground (parameters wired up, with a copy-pasteable
code snippet) shared across all three demo hosts. Run whichever hosting model you want to try —
`BlazorWebAppSvrDemo`, `BlazorWebAppWasmDemo`, or `BlazorWebAppAutoDemo` — and open `/playground/<name>`
(e.g. `/playground/badge`), or start from `/demo` for a list of every playground.

## License

[MIT](LICENSE.txt)
