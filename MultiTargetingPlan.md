# Multi-target BlazorAtoms libraries for net9.0 + net10.0

## Context

Goal: understand how standard NuGet packages support multiple .NET versions, and apply it so
the BlazorAtoms libraries support both **net9.0** and **net10.0**.

Current state (confirmed by exploration):
- Root `Directory.Build.props` hard-codes a single `<TargetFramework>net9.0</TargetFramework>`
  for every project in the repo (libraries, tests, samples all inherit it).
- `Directory.Packages.props` uses Central Package Management (CPM) with one version per
  package (`Microsoft.AspNetCore.Components.Web` pinned to `9.0.11`), not conditioned by TFM.
- `global.json` pins the SDK to `9.0.315`, `rollForward: latestMinor` (stays in the 9.x major).
- No library currently has any `#if NET9_0`/`NET10_0` conditional code — zero TFM-specific
  source branching anywhere.
- Locally installed SDKs already include `10.0.301` and `10.0.302`, so no SDK install is needed.
- CI (`.github/workflows/dotnet.yml`) installs `9.0.x` only.

Decisions:
- Libraries (`src/*`) and tests (`tests/*`) become **true multi-target**: one package/project
  produces both a `net9.0` and a `net10.0` build (`<TargetFrameworks>net9.0;net10.0</TargetFrameworks>`).
- Demo apps (`samples/*`) are **not** multi-targeted (an app only runs on one TFM at a time) —
  they move forward to `net10.0` only.
- CI and `global.json` get updated so builds actually have the right SDK/TFM support.

## How NuGet/.NET multi-targeting works (the general mechanism)

- A `.csproj` declares **one** of two mutually-exclusive properties:
  - `<TargetFramework>net10.0</TargetFramework>` — singular, one output.
  - `<TargetFrameworks>net9.0;net10.0</TargetFrameworks>` — plural, semicolon-separated list.
    Setting `TargetFrameworks` makes the SDK build the project **once per TFM** in the list,
    each as its own compilation pass with its own `$(TargetFramework)` value, output folder
    (`bin/Debug/net9.0/`, `bin/Debug/net10.0/`), and ref/implementation assemblies.
  - Never set both — the SDK errors (`NETSDK1013`) if a project has non-empty values for both.
- `dotnet pack` on a multi-targeted project produces **one `.nupkg`** with a `lib/net9.0/*.dll`
  and `lib/net10.0/*.dll` folder pair inside it. A consuming project's NuGet restore picks
  whichever folder matches its own TFM automatically — that's the entire point: one package,
  compiled per-target, and NuGet's asset-selection algorithm does the matching.
- Per-TFM differences are expressed with:
  - **Conditioned MSBuild properties/items**: `Condition="'$(TargetFramework)'=='net10.0'"` on
    any property or `<PackageReference>`/`<PackageVersion>` item — evaluated separately in each
    TFM's build pass.
  - **`#if NET9_0` / `#if NET10_0` / `#if NET9_0_OR_GREATER`** compiler directives in source,
    for actual code differences per TFM (BlazorAtoms doesn't need this today — no such branching
    exists — but it's the standard escape hatch when a newer TFM exposes a new API you want to
    use conditionally).
  - With **Central Package Management** (CPM, already in use here via `Directory.Packages.props`),
    a `<PackageVersion>` entry can also carry a `Condition` on `$(TargetFramework)`, so the same
    package id resolves to a different version per TFM pass (e.g. the ASP.NET Core components
    package needs to be `9.0.x` for the `net9.0` pass and `10.0.x` for the `net10.0` pass).
- The **SDK you build with** must recognize the highest TargetFramework requested (an older SDK
  doesn't know what `net10.0` means), but a newer SDK can still build older TFMs — SDKs are
  backward-compatible for authoring lower TargetFrameworks. So one modern SDK (10.x) is enough
  to build a project that multi-targets `net9.0;net10.0` — you don't need both SDKs installed,
  just the newest one covering the highest TFM in use (they happen to both be installed here
  already anyway).

## Concrete changes

### 1. `Directory.Build.props` — TFM by folder, not one global value

Replace the single hard-coded `<TargetFramework>net9.0</TargetFramework>` with a path-conditioned
split, so no individual `.csproj` needs to be touched:

```xml
<PropertyGroup Condition="$(MSBuildProjectFullPath.Contains('\src\')) OR $(MSBuildProjectFullPath.Contains('\tests\'))">
  <TargetFrameworks>net9.0;net10.0</TargetFrameworks>
</PropertyGroup>
<PropertyGroup Condition="$(MSBuildProjectFullPath.Contains('\samples\'))">
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>
```

Also **remove** the hard-coded `<RazorLangVersion>9.0</RazorLangVersion>`. Left in place, it
would freeze the `net10.0` build pass to the old Razor language version too. Removing it lets
the Razor SDK pick the version matching each TFM's build pass automatically (the standard/default
behavior every other property here already relies on).

Everything else in the file (`Nullable`, `ImplicitUsings`, `RepoRoot`, `Authors`, etc.) stays as-is
— those aren't TFM-specific.

### 2. `Directory.Packages.props` — per-TFM package versions

`Microsoft.AspNetCore.Components.Web` needs a version aligned to each TFM's own ASP.NET Core
release. Split the single unconditioned entry into two conditioned ones (confirmed current
stable versions via NuGet: `9.0.18` and `10.0.10`):

```xml
<PackageVersion Include="Microsoft.AspNetCore.Components.Web" Version="9.0.18" Condition="'$(TargetFramework)'=='net9.0'" />
<PackageVersion Include="Microsoft.AspNetCore.Components.Web" Version="10.0.10" Condition="'$(TargetFramework)'=='net10.0'" />
```

`Microsoft.AspNetCore.Components.WebAssembly` and `Microsoft.AspNetCore.Components.WebAssembly.Server`
are only consumed by the `samples/*` hosts, which are moving to `net10.0`-only — bump those two
straight to `10.0.10` (no conditioning needed, single TFM consumer).

`ZXing.Net` and `SixLabors.ImageSharp` are TFM-agnostic third-party packages — leave unconditioned.
Test packages (`Microsoft.NET.Test.Sdk`, `bunit`, `xunit`, `xunit.runner.visualstudio`) are also
TFM-agnostic — leave as-is; bUnit/xUnit run the same way against both TFM builds of a test project.

### 3. `global.json` — move the SDK pin to .NET 10

```json
{
  "sdk": {
    "version": "10.0.301",
    "rollForward": "latestMinor"
  }
}
```

This is safe for the `net9.0` build passes too — the .NET 10 SDK builds `net9.0` projects fine
(it's the standard "build older TFMs with the newest SDK" pattern). `rollForward: latestMinor`
lets it move to future 10.x minors (and picks up the already-installed `10.0.302` transparently)
without jumping to a future major.

### 4. CI — `.github/workflows/dotnet.yml`

Bump the `actions/setup-dotnet@v5` step's `dotnet-version` from `9.0.x` to `10.0.x`. One modern
SDK is enough to build both TFM passes for the same reason as #3 — no need to install two SDKs
in CI.

### 5. `build/Packable.props` — stale comment

The comment "net9 SDK bundles SourceLink in-box" is no longer precisely accurate framing once
the repo targets net10 too (still true, just outdated wording) — reword to drop the net9-specific
callout since SourceLink bundling isn't actually TFM-conditioned behavior.

### 6. Library `.csproj` files — no changes needed

Because TFM selection moves to the path-conditioned root props (#1), none of the 15 library
`.csproj` files, `build/Shared.props`, or the loose `src/BlazorAtoms.Shared/*.cs` files need any
edits — they all inherit `TargetFrameworks` from the root and recompile per TFM pass automatically.
This was confirmed safe because there is zero existing `#if NET*` branching to reconcile.

## Verification (run by the repo owner — build/test/pack are not run automatically)

1. `dotnet build BlazorAtoms.sln` — confirms both TFM passes compile cleanly for every library
   and test project, and the three demo apps build against `net10.0`.
2. `dotnet pack src/BlazorAtoms.Badges/BlazorAtoms.Badges.csproj -o out` then inspect the produced
   `.nupkg` (it's a zip) for both `lib/net9.0/BlazorAtoms.Badges.dll` and
   `lib/net10.0/BlazorAtoms.Badges.dll`.
3. `dotnet test tests/BlazorAtoms.Badges.Tests/BlazorAtoms.Badges.Tests.csproj` — runs against
   both TFMs by default; add `--framework net9.0` / `--framework net10.0` to isolate one.
4. Spot-check one of the three demo hosts still runs (`dotnet run` from its folder) now that it's
   single-targeted at `net10.0`.
