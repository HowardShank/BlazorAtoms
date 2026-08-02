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
        _navigator = new WizardNavigator(WizardModelSchema.For<TModel>(), Model);
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

    private void HandlePrevious()
    {
        _navigator.GoPrevious();
        _editContext.NotifyValidationStateChanged();
    }

    private void HandleNext()
    {
        if (!_navigator.ValidateCurrentStep(_messageStore))
        {
            _editContext.NotifyValidationStateChanged();
            return;
        }
        _navigator.GoNext();
        _editContext.NotifyValidationStateChanged();
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
