# Contributing

Thanks for your interest in BlazorAtoms.

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

Each library's `README.md` is usage-only (install, examples, params). Design/implementation
rationale for a library lives in that library's own `DEVELOPMENT.md`, if it has one — read it
before changing that library's internals.

## Build & test

```bash
dotnet restore
dotnet build BlazorAtoms.sln
dotnet test
```

Or a single library:

```bash
dotnet build src/BlazorAtoms.Badges/BlazorAtoms.Badges.csproj
dotnet test tests/BlazorAtoms.Badges.Tests/BlazorAtoms.Badges.Tests.csproj
```

Every library has a bUnit test project under `tests/`. Add or update tests for any behavior
change — a PR that changes a component's rendering or param handling without a test change will
be asked for one.

## Conventions

- Each component library has ~0 third-party dependencies and no shared runtime package — keep it
  that way. See [`src/LIBRARY-CATALOG.md`](src/LIBRARY-CATALOG.md) for the JS/graphics policy and
  naming conventions new components should follow.
- Components read look from CSS variables (no global theme provider) and inputs from
  `[Parameter]`s.
- Any component that ships JavaScript self-imports its own ES module in `OnAfterRenderAsync`
  (`firstRender`) — no `<script>` tag, no DI registration.
- Try the change in a live demo host (`BlazorWebAppSvrDemo`, `BlazorWebAppWasmDemo`, or
  `BlazorWebAppAutoDemo`) via its `/playground/<name>` page before opening a PR, if the change is
  visual or interactive.

## Submitting a change

1. Fork and branch from `main`.
2. Make the change, with tests.
3. `dotnet build BlazorAtoms.sln` and `dotnet test` clean.
4. Open a PR describing what changed and why.

For anything more than a small fix, open an issue first to discuss the approach.

## Reporting bugs / requesting features

Use [GitHub Issues](https://github.com/HowardShank/BlazorAtoms/issues).

## Code of Conduct

This project follows the [Code of Conduct](CODE_OF_CONDUCT.md).
