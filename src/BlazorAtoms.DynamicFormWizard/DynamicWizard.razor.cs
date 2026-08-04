using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using BlazorAtoms.DynamicFormWizard.Attributes;
using BlazorAtoms.DynamicFormWizard.Navigation;
using BlazorAtoms.DynamicFormWizard.Rendering;
using BlazorAtoms.DynamicFormWizard.Schema;

namespace BlazorAtoms.DynamicFormWizard;

/// <summary>
/// Reflection/attribute-driven multi-step wizard engine (DESIGN-DISCUSSION.md). Decorate a POCO
/// with <c>FormStep</c>/<c>FormOrder</c>/<c>DependsOn</c>/<see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/>-derived
/// attributes and this component renders the whole flow: step navigation with branching and free
/// rejoining, dynamic step label/position (never the raw declared step number or a static total),
/// partial per-step validation, and a four-tier field-render dispatch (consumer type-registry -&gt;
/// built-in scalar -&gt; auto-expand complex types -&gt; ToString fallback).
///
/// Interactive-render-mode only (DESIGN-DISCUSSION.md F.18) -- reflection-driven rendering, the
/// EditContext-based validation/CSS-state wiring, and (in a later batch) file upload all require
/// an interactive render mode; unlike most BlazorAtoms components, this one does not work under
/// static SSR.
/// </summary>
public partial class DynamicWizard<TModel> where TModel : class, new()
{
    [Parameter, EditorRequired]
    public TModel Model { get; set; } = default!;

    /// <summary>Raised once the final step's validation passes and Submit is pressed.</summary>
    [Parameter]
    public EventCallback<TModel> OnWizardComplete { get; set; }

    /// <summary>Raised when Cancel is clicked (DESIGN-DISCUSSION.md G.26, #137). Fires immediately
    /// with no validation and no step/state mutation -- Cancel is meant to abandon the flow, not
    /// complete it, so unlike Next/Submit it never blocks on an invalid current step. No built-in
    /// confirmation dialog; a consumer wanting "are you sure?" implements that themselves before
    /// acting on this callback (this engine doesn't own any modal UI elsewhere either).</summary>
    [Parameter]
    public EventCallback<TModel> OnWizardCancel { get; set; }

    /// <summary>Shows the Cancel button in the nav row when true. Defaults to <c>false</c> --
    /// opt-in, so every existing consumer's nav row is unchanged unless they ask for this.</summary>
    [Parameter]
    public bool ShowCancelButton { get; set; }

    /// <summary>Whole-form render override (DESIGN-DISCUSSION.md A.2) -- when set, called instead
    /// of this component's own dispatch for *every* field. Leave null for the native/bare-CSS
    /// fallback rendering.</summary>
    [Parameter]
    public RenderFragment<WizardFieldContext>? FieldTemplate { get; set; }

    /// <summary>Per-type render override (DESIGN-DISCUSSION.md A.3/A.4 tier 1) -- a property whose
    /// type matches a key here opens that component (same Value/ValueChanged contract as every
    /// built-in field) instead of falling through to the built-in switch, auto-expansion, or the
    /// ToString fallback. See EXTENSIBILITY.md for a full worked example.</summary>
    [Parameter]
    public IReadOnlyDictionary<Type, Type>? FieldRenderers { get; set; }

    /// <summary>Whole-form default for where a field's label renders (DESIGN-DISCUSSION.md H.31,
    /// #142) -- a property's own <c>[FormLabel]</c> attribute overrides this, same
    /// attribute-wins-over-default pattern as everywhere else in this engine.</summary>
    [Parameter]
    public LabelPosition DefaultLabelPosition { get; set; } = LabelPosition.Above;

    /// <summary>Extra HTML attributes to splat onto one field's rendered input, keyed by
    /// top-level property name (DESIGN-DISCUSSION.md H.31, #143 -- same top-level-only reach as
    /// <c>[DependsOn]</c>/<c>[FormSelect]</c>, B.6). Does not reach a tier-1 <see cref="FieldRenderers"/>
    /// component, since an arbitrary consumer component has no guaranteed attribute to receive
    /// it (see the doc comment on <c>RenderRegisteredComponent</c> in <c>DynamicWizard.Fields.cs</c>).</summary>
    [Parameter]
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>? FieldAttributes { get; set; }

    /// <summary>Resumes at a previously-saved step instead of always starting at the first
    /// declared step (DESIGN-DISCUSSION.md G.23, #134) -- pairs with <see cref="CurrentStep"/> and
    /// <see cref="OnStepChanged"/> for a consumer's own draft-save/resume: <c>Model</c> and the
    /// step number are both plain JSON-serializable state (including any
    /// <see cref="Files.WizardFileAttachment"/> already held as <c>byte[]</c>), so this engine
    /// doesn't need to own any storage itself -- save them wherever, restore into a fresh
    /// component instance later via <c>Model</c> + this parameter. Falls back to the first
    /// declared step if it isn't a real one for this schema (see <see cref="WizardNavigator"/>'s
    /// constructor).</summary>
    [Parameter]
    public int? InitialStep { get; set; }

    /// <summary>Raised after Next/Back actually moves to a different step (DESIGN-DISCUSSION.md
    /// G.23, #134) -- not on every field edit, which is deliberately out of scope: a consumer
    /// wanting keystroke-level autosave already owns <c>Model</c> and can serialize it on their
    /// own cadence (a timer, page-unload, etc.) without needing a callback for every edit.</summary>
    [Parameter]
    public EventCallback<int> OnStepChanged { get; set; }

    /// <summary>The wizard's current raw declared step number -- read via <c>@ref</c> any time to
    /// build a draft-save snapshot together with <c>Model</c> (DESIGN-DISCUSSION.md G.23,
    /// #134).</summary>
    public int CurrentStep => _navigator.CurrentStep;

    private EditContext _editContext = default!;
    private ValidationMessageStore _messageStore = default!;
    private WizardNavigator _navigator = default!;
    private int _lastAnnouncedStep = -1;
    private ElementReference _stepHeadingRef;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(Model);
        _editContext.SetFieldCssClassProvider(new WizardFieldCssClassProvider());
        _messageStore = new ValidationMessageStore(_editContext);
        _navigator = new WizardNavigator(WizardModelSchema.For<TModel>(), Model, InitialStep);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_navigator.CurrentStep == _lastAnnouncedStep)
        {
            return;
        }
        _lastAnnouncedStep = _navigator.CurrentStep;

        // Focus the new step's heading so screen-reader/keyboard users land somewhere meaningful
        // on advance/back (DESIGN-DISCUSSION.md F.17) -- guarded like every other interop call
        // site in this repo, since a torn-down circuit/disposed component must not throw into the
        // UI (bUnit's mock also bypasses this, so the guard is inspection-only there).
        try
        {
            await _stepHeadingRef.FocusAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException)
        {
            // Circuit gone or component disposed mid-focus -- nothing to recover, nothing to show.
        }
    }

    private string StepAnnouncement
    {
        get
        {
            var (position, count) = _navigator.DisplayPosition();
            return $"{_navigator.DisplayTitle()}. Step {position} of {count}.";
        }
    }

    private IReadOnlyList<WizardPropertySchema> VisibleFields => _navigator.VisiblePropertiesForCurrentStep();

    private bool IsFinalStep => _navigator.IsFinalStep();

    /// <summary>Bare-CSS re-derivation of Ideas.md iteration 4's Bootstrap column span
    /// (DESIGN-DISCUSSION.md F.21) -- an untagged field gets no inline style at all and falls
    /// back to the CSS default (full row, `span 12`), so nothing changes for a model that never
    /// uses <c>[FormLayout]</c>.</summary>
    private static string ColumnSpanStyle(WizardPropertySchema property) =>
        property.Layout is { } layout ? $"--wizard-column-span: {layout.Span};" : string.Empty;

    /// <summary>[FormLabel] on the property wins over the wizard-level default (DESIGN-DISCUSSION.md
    /// H.31).</summary>
    private LabelPosition EffectiveLabelPosition(WizardPropertySchema property) =>
        property.LabelPositionOverride ?? DefaultLabelPosition;

    /// <summary>Above/Left keep a real, visible &lt;label&gt; element -- Left just lays it out
    /// beside the input instead of above it. Inline/Hidden render no visible label element at all;
    /// the label text moves onto the input itself instead (see <c>BuildExtraAttributes</c> in
    /// <c>DynamicWizard.Fields.cs</c>).</summary>
    private static bool ShowsLabelElement(LabelPosition position) =>
        position is LabelPosition.Above or LabelPosition.Left;

    private static string FieldRowClass(LabelPosition position) =>
        position == LabelPosition.Left ? "wizard__field-row wizard__field-row--label-left" : "wizard__field-row";

    private async Task HandlePrevious()
    {
        var before = _navigator.CurrentStep;
        _navigator.GoPrevious();
        _editContext.NotifyValidationStateChanged();
        if (_navigator.CurrentStep != before)
        {
            await OnStepChanged.InvokeAsync(_navigator.CurrentStep);
        }
    }

    private async Task HandleNext()
    {
        if (!_navigator.ValidateCurrentStep(_messageStore))
        {
            _editContext.NotifyValidationStateChanged();
            return;
        }
        var before = _navigator.CurrentStep;
        _navigator.GoNext();
        _editContext.NotifyValidationStateChanged();
        if (_navigator.CurrentStep != before)
        {
            await OnStepChanged.InvokeAsync(_navigator.CurrentStep);
        }
    }

    private async Task HandleCancel()
    {
        await OnWizardCancel.InvokeAsync(Model);
    }

    private async Task HandleSubmit()
    {
        if (!_navigator.ValidateCurrentStep(_messageStore))
        {
            _editContext.NotifyValidationStateChanged();
            return;
        }
        await OnWizardComplete.InvokeAsync(Model);
    }

    /// <summary>Live per-keystroke revalidation of the *current* step only (never the whole
    /// model), so <see cref="WizardFieldCssClassProvider"/> reflects state immediately without
    /// waiting for Next (DESIGN-DISCUSSION.md D.12).</summary>
    private void OnFieldChanged()
    {
        _navigator.ValidateCurrentStep(_messageStore);
        _editContext.NotifyValidationStateChanged();
        StateHasChanged();
    }
}
