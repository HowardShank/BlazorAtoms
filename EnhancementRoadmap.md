# Library and Component Enhancement Roadmap

## General Enhancements
- Add ARIA attributes for better accessibility.

## Activity Indicator Components

## Avatar Components
- Add support for custom shapes (e.g., square, rounded, circular).
- Add skeleton loading state for avatars to improve perceived performance.

## Badge Components
- N/A

## Barcodes Components
- N/A

## Breadcrumb Components

## Canvas Components
- N/A

## Clocks Components
- N/A

## Data Components
- N/A

## Drag and Drop Components
- N/A

## Highlights Components
- N/A

## Inputs Components

* ### Range Input Components
* Enhance the range input component to support custom styling and theming options.
* Add tick marks
* Add optional handle tooltips to display the current value.

## Navigation Components
- Add Scroll To component.
- Added Blazor Breadcrumb component.

## Progress Components
- **`AtomProgressSteps` wizard-integration sample: rewire to a `Controller`-shaped parameter, not a
  concrete wizard type.** `BlazorComposites.DynamicFormWizard`'s README/DEVELOPMENT.md show an
  illustrative (non-compiled) `AtomProgressSteps` wiring sample against a raw `_wizardRef!` field.
  That repo has since added `IWizardController` (non-generic nav/step-progress interface,
  2026-08-09) and rewired its OWN external-nav playground's step row/footer onto it
  (`WizardControllerNavStrip.razor`, param'd `IWizardController Controller`) specifically so a
  reusable component never has to be generic over the wizard's model type. Do the matching update
  here: change the illustrative sample (and `StatusFor` bridge snippet) to read `Controller.___`
  instead of `_wizardRef!.___`, so this package's own docs model the same non-generic-consumer
  pattern rather than a one-off field reference. Docs/sample only — no `ProjectReference` to
  `BlazorComposites.DynamicFormWizard` should be added; `IWizardController` isn't a real dependency
  of this package, just the shape the sample now illustrates wiring against.
  `AtomProgressSteps` still has no native per-step "visited" concept of its own (only
  `index < Current` inference) — `StatusFor` remains the bridge for that; unrelated to this task,
  logged separately as this component's own future enhancement if a native visited/completed input
  is ever wanted.
- **`AtomProgressSteps`/`OnStepClick` can't disable itself while a wizard transition is pending —
  known gap on the `BlazorComposites.DynamicFormWizard` side, tracked here since it constrains this
  component's wiring.** That package's `IWizardController` (the interface a step-progress component
  wires against, per the task above) does not expose `IsTransitioning` — deliberately scoped that
  way when the interface was extracted, per that repo's own notes. Consequence for
  `AtomProgressSteps`: `OnStepClick` has no way to know a step-transition is in flight (e.g. an
  `OnStepChanging` handler awaiting an external call) and so cannot disable/gray out its own markers
  during that window, unlike the wizard's own built-in Back/Next row, which does. Nothing to build
  in THIS package today — no wiring exists yet either way — but worth remembering when the
  wizard-integration sample above (or a real bridge component, if one is ever built) is written: it
  will inherit this same blind spot until `IWizardController` widens, which is that repo's call, not
  this one's.
- **Real `IWizardController` wiring into `AtomProgressSteps` — a genuine integration, not the
  docs-only sample rewire above.** The task above only changes an illustrative, non-compiled `<pre>`
  code block; this is different in kind: making `AtomProgressSteps` (or a small bridge component
  alongside it) actually **take** an `IWizardController`-typed parameter and compile against it for
  real. That collides directly with a constraint already on record for the sample task: no
  `ProjectReference` from this package to `BlazorComposites.DynamicFormWizard` should be added,
  since `IWizardController` isn't a real dependency of this package today. Real wiring can't avoid
  that question — it has to be answered one of two ways before this is buildable, not silently
  assumed:
  1. **Take the dependency.** Add a `ProjectReference` (or a new, tiny adapter package depending on
     both) and let a bridge component reference `IWizardController` directly. Simplest code, but
     turns a one-way "the sample shows how to wire us together" relationship into a real coupling
     between two otherwise-independent repos — `BlazorComposites` would become something
     `BlazorAtoms` (or a new adapter package) must track/rebuild against.
  2. **Duck-type instead of reference.** Define this package's OWN minimal interface here (e.g. just
     `StepSummaries()`-shaped data plus a `GoToStep(int)`-shaped delegate) that a consumer's
     `DynamicWizard<TModel>` happens to satisfy structurally without either package referencing the
     other. Avoids the cross-repo dependency, at the cost of maintaining a second, parallel
     "step-progress controller" shape that has to be kept conceptually in sync with
     `IWizardController` by hand, in two repos, by whoever remembers to.
  Filed as a placeholder pending that decision — not started, no code written either way. Also
  inherits both gaps already logged above: no native "visited" concept (works around via
  `StatusFor`, per the sample task) and no `IsTransitioning` on `IWizardController` (can't disable
  `OnStepClick` during a pending transition).
- **`AtomProgressBar`/`AtomProgressRing` wizard-integration sample — a NEW sample, not a rewire; none
  exists today.** Confirmed by search: unlike `AtomProgressSteps`, neither component has any
  illustrative wizard-wiring sample anywhere in `BlazorComposites.DynamicFormWizard`'s docs today,
  and neither has a `TModel`-genericity problem to solve (they're plain `Value`/`Min`/`Max`
  indicators, no wizard type reference of their own) — so there's no existing code to "rewire" onto
  `Controller`/`IWizardController` the way the `AtomProgressSteps` task above does. If this is
  wanted, it's new content: a sample showing a wizard's overall completion fed as a percentage —
  e.g. `Value="@(controller.StepSummaries().Count(s => s.IsVisited) * 100.0 / controller.StepSummaries().Count)"`
  wired to `AtomProgressBar` or `AtomProgressRing`'s `Value`, using `IWizardController.StepSummaries()`
  same as the `AtomProgressSteps` sample does, added to whichever repo's docs make sense (probably
  `BlazorComposites.DynamicFormWizard`'s README, mirroring where the `AtomProgressSteps` sample
  already lives, with this package's real parameter names shown illustratively same as that one).
  Filed here as a placeholder since it touches this package's parameter names either way — scope and
  target repo not yet decided.

## Ratings Components
- N/A

## Screensavers Components
- N/A

## Tooltips Components
- N/A

