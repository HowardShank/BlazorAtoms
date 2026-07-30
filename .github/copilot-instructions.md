# BlazorAtoms — Copilot Instructions

## Philosophy
- Family of small, standalone Razor class libraries (`BlazorAtoms.<Area>`). Each ships as its own NuGet package with ~0 third-party runtime dependencies.
- Components are drop-in: no DI registration, no global theme provider, no manual `<script>` tags.
- Prefer code-behind `.razor.cs` over `@code` blocks.
- Prefer SVG/CSS for visuals and animation. Use JS only where a browser primitive forces it (e.g., `<canvas>`); JS modules must self-import on first interactive render and scope to an element id/`ElementReference`.

## Repository Layout
- `src/BlazorAtoms.<Area>/` — library projects (`Microsoft.NET.Sdk.Razor`).
- `tests/BlazorAtoms.<Area>.Tests/` — one bUnit/xUnit project per library.
- `samples/Demos.Shared/` — shared playground pages (`*PlaygroundView.razor`) served by all demo hosts.
- `samples/BlazorWebApp{Svr,Wasm,Auto}Demo/` — thin wrapper pages for Server, standalone WASM, and Auto render modes.

## Project Conventions
- `Directory.Build.props` sets `net9.0`, `Nullable=enable`, `ImplicitUsings=enable`, `RepoRoot`, and `DisableFastUpToDateCheck` (Visual Studio's F5/Debug up-to-date heuristic has repeatedly served stale builds in this repo after a new `ProjectReference`, a new component type in a referenced project, or a new file from a custom MSBuild `Target` — this forces a real MSBuild evaluation every time instead).
- After adding a **new project to the solution** (not just a new file in an existing one), do one full "Rebuild Solution" in Visual Studio — `DisableFastUpToDateCheck` doesn't help here, since VS hasn't loaded the new project into its project system at all until a real solution-level build runs.
- Library csprojs import `build/Packable.props` (pack/NuGet settings) and `build/Shared.props` (compiles `BlazorAtoms.Shared` source in; no project reference).
- Each package references only `Microsoft.AspNetCore.Components.Web` (plus framework packages).
- Public component names are `Atom<Name>`; library root namespace is `BlazorAtoms.<Area>`.
- Shared base classes live in `BlazorAtoms.Shared`: `AtomComponentBase` (adds `CssClass`/`Style`), `StyleVars` (fluent CSS custom-property builder).

## Component Patterns
- Inputs: `[Parameter]`s only. Default theming through CSS custom properties exposed via `StyleVars`/inline `style`; use `ClassAttr()`/`StyleAttr()` helpers.
- `Disabled` means the canvas/control does not render or is non-interactive; `ReadOnly`/`Visible` follow library-specific definitions.
- JS-backed components guard against prerender/SSR with `RendererInfo.IsInteractive`, then lazy-import ` ./_content/BlazorAtoms.<Area>/<module>.js` via `IJSObjectReference`.
- Self-imported JS modules export lifecycle functions (e.g., `start`, `stop`, `dispose`) scoped to an element id or `ElementReference`.

## Adding a Component
1. Library: component `.razor` + code-behind `.razor.cs` under `src/BlazorAtoms.<Area>/`.
2. Shared playground: `samples/Demos.Shared/Playgrounds/<Name>PlaygroundView.razor` that wires every parameter and emits a `<CodeSnippetBox>` snippet.
3. Three thin wrapper pages: `samples/BlazorWebAppSvrDemo/Components/Pages/`, `samples/BlazorWebAppAutoDemo/BlazorWebAppAutoDemo.Client/Pages/`, `samples/BlazorWebAppWasmDemo/BlazorWebAppWasmDemo.Client/Pages/`.
4. Add NavMenu entries in all three demos and a `samples/Demos.Shared/Demo.razor` link.
5. Tests: `tests/BlazorAtoms.<Area>.Tests/<Name>Tests.cs` plus `Usings.cs` with `Bunit`, `Xunit`, `Microsoft.AspNetCore.Components`, and the library namespace.
6. Add the library project reference to `samples/Demos.Shared/Demos.Shared.csproj`.

## Testing Conventions
- Test classes inherit `TestContext` (bUnit).
- Set `JSInterop.Mode = JSRuntimeMode.Loose` when a component makes JS interop calls.
- For components checking `RendererInfo.IsInteractive`, call `SetRendererInfo(new RendererInfo("Server", isInteractive: true))` (or `"WebAssembly"`) in the test constructor.
- Verify self-imported modules with `JSInterop.SetupModule("./_content/BlazorAtoms.<Area>/<module>.js")` and assert via `module.VerifyInvoke("methodName")` or `module.Invocations`.
- JS-free tests assert rendered markup and attributes.

## Build / Test
- Do not build or test without express direction from the user. Use the following commands to build and test the entire solution:
```bash
dotnet restore
dotnet build BlazorAtoms.sln
dotnet test
```
Run a single library + tests:
```bash
dotnet build src/BlazorAtoms.Badges/BlazorAtoms.Badges.csproj
dotnet test tests/BlazorAtoms.Badges.Tests/BlazorAtoms.Badges.Tests.csproj
```

## Decision References
- New libraries and naming rules: `src/LIBRARY-CATALOG.md`
- JavaScript and graphics policy: `src/LIBRARY-CATALOG.md`
- Playground requirements: `samples/Demos.Shared/Playgrounds/README.md`
- Shared component styling helpers: `src/BlazorAtoms.Shared/`
- Example self-importing component: `src/BlazorAtoms.Screensavers/AtomScreensaverRain.razor.cs`
- Example bUnit JS-interop tests:
  - `tests/BlazorAtoms.Screensavers.Tests/AtomScreensaverRainTests.cs`
  - `tests/BlazorAtoms.Canvas.Tests/Canvas2DContextTests.cs`
  - `tests/BlazorAtoms.Highlights.Tests/AtomHighlighterTests.cs`

## Communication Style
- User prefers terse statements, no guessing, no assumptions, minimize token use where possible.

## Git Commands
- When undoing changes, do not use destructive git commands such as `git reset`, `git clean`, or any other command that removes uncommitted files or modifies repository state destructively without explicit approval.

## File and Tool Safety
- Deleting files via any tool, terminal, or script — including PowerShell, Bash, and file-system APIs — must be expressly identified and authorized before execution. Explain the risk and wait for explicit approval. This applies to uncommitted work and tracked files alike. No silent or assumed deletions.
- If an action is denied, stop immediately and ask for instructions. Never continue with an alternative solution without first requesting what the user wants done next.
