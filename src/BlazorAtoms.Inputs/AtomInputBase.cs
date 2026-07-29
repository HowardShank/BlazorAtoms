using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Inputs;

/// <summary>
/// Shared base for the standard form fields in this library (<see cref="AtomTextField"/>,
/// <see cref="AtomTextArea"/>, and their siblings): the <c>@bind-Value</c> pair, the hand-rolled
/// <see cref="EditContext"/> glue, the label/help/visibility surface, and the three styling axes
/// (<see cref="Variant"/>/<see cref="Size"/>/<see cref="Effect"/>) plus the <c>--field-*</c> custom
/// properties every derived field's CSS reads.
/// </summary>
/// <remarks>
/// <para><b>Why not <c>InputBase&lt;TValue&gt;</c>:</b> C# is single-inheritance, and every component
/// in this repo needs <see cref="AtomComponentBase"/>'s <c>CssClass</c>/<c>Style</c>/splat
/// convention, so the small amount of <see cref="EditContext"/> glue is hand-rolled here instead —
/// once, rather than per component. See DEVELOPMENT.md.</para>
/// <para><b>Derived components that override <see cref="OnParametersSet"/> must call
/// <c>base.OnParametersSet()</c></b> — that is where the <see cref="FieldIdentifier"/> is
/// (re)computed.</para>
/// <para><see cref="AtomRangeInput{TValue}"/> and <see cref="AtomCrtInput"/> predate this base and
/// deliberately keep their own copy of the glue; retrofitting them is tracked in DEVELOPMENT.md.</para>
/// </remarks>
public abstract class AtomInputBase<TValue> : AtomComponentBase, IDisposable
{
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    // ---- binding ----------------------------------------------------------------------------

    /// <summary>The current value. Bind with <c>@bind-Value</c>.</summary>
    [Parameter] public TValue Value { get; set; } = default!;

    /// <summary>Raised when the value changes. Backs <c>@bind-Value</c>; only needed directly when
    /// not using <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>Identifies the bound value. Populated automatically by <c>@bind-Value</c>; required
    /// if you use <see cref="ValueChanged"/> directly and want validation.</summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>Identifies the property used for form validation — wires this field to an ancestor
    /// <see cref="EditContext"/> (e.g. from <c>&lt;EditForm&gt;</c> +
    /// <c>&lt;DataAnnotationsValidator /&gt;</c>). Falls back to <see cref="ValueExpression"/> when
    /// not set, so a plain <c>@bind-Value</c> inside an <c>EditForm</c> still participates.</summary>
    [Parameter] public Expression<Func<TValue>>? ValidationFor { get; set; }

    // ---- structure --------------------------------------------------------------------------

    /// <summary>Form label shown above/beside the control.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Responsive classes for the label column. Default <c>clr-col-12 clr-col-md-2</c>.</summary>
    [Parameter] public string LabelCol { get; set; } = "clr-col-12 clr-col-md-2";

    /// <summary>Responsive classes for the control column. Default <c>clr-col-12 clr-col-md-10</c>.</summary>
    [Parameter] public string ControlCol { get; set; } = "clr-col-12 clr-col-md-10";

    /// <summary>Help text shown under the control. Replaced by the first validation message while
    /// the field is in error.</summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>Accessible label for the control itself. Falls back to <see cref="Label"/>, then to
    /// a per-component default. (The visible <c>&lt;label&gt;</c> carries no <c>for</c> — the
    /// control's own <c>aria-label</c> names it, which avoids minting ids that would differ between
    /// the prerender and interactive passes.)</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Marks the field required: native <c>required</c> on the control plus an asterisk
    /// after the label. Independent of <see cref="EditContext"/> validation.</summary>
    [Parameter] public bool Required { get; set; }

    // ---- state ------------------------------------------------------------------------------

    /// <summary>When true, greys out and blocks input (native <c>disabled</c>: not focusable, not
    /// submitted). Use <see cref="Visible"/> to show/hide instead.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>When true, blocks editing but — on controls where the platform supports it — keeps
    /// the value focusable, selectable, and submitted (native <c>readonly</c>). On controls with no
    /// native read-only state (checkbox, radio, switch, select) this falls back to
    /// <see cref="Disabled"/>; see <see cref="SupportsNativeReadOnly"/>.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>When false, hidden via CSS <c>display:none</c> (stays in the DOM, keeping its
    /// <see cref="EditContext"/> subscription). Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    // ---- styling axes -----------------------------------------------------------------------

    /// <summary>Frame treatment → <c>data-variant</c>. Default <see cref="InputVariant.Outline"/>.</summary>
    [Parameter] public InputVariant Variant { get; set; } = InputVariant.Outline;

    /// <summary>Density preset → <c>data-size</c>. Default <see cref="InputSize.Medium"/>.</summary>
    [Parameter] public InputSize Size { get; set; } = InputSize.Medium;

    /// <summary>Opt-in CSS motion → <c>data-effect</c>. Default <see cref="InputEffect.None"/>
    /// (no attribute emitted).</summary>
    [Parameter] public InputEffect Effect { get; set; } = InputEffect.None;

    // ---- theming (→ --field-* custom properties) --------------------------------------------

    /// <summary>Control width in px → <c>--field-width</c>. Default is 100% of the control column.</summary>
    [Parameter] public double? Width { get; set; }

    /// <summary>Text size in px → <c>--field-font-size</c>. Default inherits.</summary>
    [Parameter] public double? FontSize { get; set; }

    /// <summary>Corner radius in px → <c>--field-radius</c>.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Border thickness in px → <c>--field-border-width</c>. <c>0</c> removes the frame.</summary>
    [Parameter] public double? BorderWidth { get; set; }

    /// <summary>Text color (any CSS color) → <c>--field-text-color</c>.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Control background → <c>--field-bg</c>.</summary>
    [Parameter] public string? BackgroundColor { get; set; }

    /// <summary>Idle border color → <c>--field-border-color</c>.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Accent used by checked/filled states and the focus ring's default →
    /// <c>--field-accent</c>.</summary>
    [Parameter] public string? AccentColor { get; set; }

    /// <summary>Focus border/ring color → <c>--field-focus-color</c>. Defaults to
    /// <see cref="AccentColor"/>.</summary>
    [Parameter] public string? FocusColor { get; set; }

    /// <summary>Error border + subtext color → <c>--field-error-color</c>.</summary>
    [Parameter] public string? ErrorColor { get; set; }

    // ---- EditContext wiring ------------------------------------------------------------------

    protected override void OnInitialized()
    {
        if (CascadedEditContext is not null)
            CascadedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
    }

    /// <summary>Recomputes the <see cref="FieldIdentifier"/> from
    /// <see cref="ValidationFor"/>/<see cref="ValueExpression"/>. Derived overrides must call this.</summary>
    protected override void OnParametersSet()
    {
        var expr = ValidationFor ?? ValueExpression;
        _hasFieldIdentifier = expr is not null;
        _fieldIdentifier = expr is not null ? FieldIdentifier.Create(expr) : default;
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) =>
        StateHasChanged();

    public virtual void Dispose()
    {
        if (CascadedEditContext is not null)
            CascadedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
    }

    /// <summary>Commits a new value: updates <see cref="Value"/>, raises
    /// <see cref="ValueChanged"/>, and notifies the <see cref="EditContext"/> so validation reruns.
    /// No-ops when the value is unchanged or input is blocked.</summary>
    protected async Task SetValueAsync(TValue newValue)
    {
        if (BlocksInput) return;
        if (EqualityComparer<TValue>.Default.Equals(newValue, Value)) return;

        Value = newValue;
        await ValueChanged.InvokeAsync(newValue);
        if (_hasFieldIdentifier)
            CascadedEditContext?.NotifyFieldChanged(_fieldIdentifier);
    }

    // ---- derived render state ---------------------------------------------------------------

    /// <summary>Whether this control has a meaningful native <c>readonly</c> state. Text-like
    /// inputs and textareas do; checkbox/radio/select/range do not (the HTML spec ignores
    /// <c>readonly</c> there), so those keep the default and fold
    /// <see cref="ReadOnly"/> into <see cref="Disabled"/>.</summary>
    protected virtual bool SupportsNativeReadOnly => false;

    /// <summary>Accessible name fallback when neither <see cref="AriaLabel"/> nor
    /// <see cref="Label"/> is set.</summary>
    protected abstract string DefaultAriaLabel { get; }

    /// <summary>True when the native <c>disabled</c> attribute should render.</summary>
    protected bool IsDisabled => Disabled || (ReadOnly && !SupportsNativeReadOnly);

    /// <summary>True when the native <c>readonly</c> attribute should render.</summary>
    protected bool IsReadOnly => ReadOnly && SupportsNativeReadOnly;

    /// <summary>True when the control must not accept input, whichever attribute achieved it.</summary>
    protected bool BlocksInput => Disabled || ReadOnly;

    protected bool HasError =>
        _hasFieldIdentifier && CascadedEditContext is not null &&
        CascadedEditContext.GetValidationMessages(_fieldIdentifier).Any();

    protected string? ErrorMessage =>
        HasError ? CascadedEditContext!.GetValidationMessages(_fieldIdentifier).FirstOrDefault() : null;

    /// <summary>Subtext content: the first validation message while in error, otherwise
    /// <see cref="HelpText"/>.</summary>
    protected string? DisplayText => HasError ? ErrorMessage : HelpText;

    /// <summary>Value for <c>data-state</c> on the root and subtext. Error wins over disabled,
    /// which wins over read-only; null (no attribute) in the normal state.</summary>
    protected string? State =>
        HasError ? "error"
        : Disabled ? "disabled"
        : ReadOnly ? "readonly"
        : null;

    protected string EffectiveAriaLabel => AriaLabel ?? Label ?? DefaultAriaLabel;

    protected string VariantAttr => Kebab(Variant.ToString());

    protected string SizeAttr => Kebab(Size.ToString());

    /// <summary>Null for <see cref="InputEffect.None"/> so the default emits no attribute at all.</summary>
    protected string? EffectAttr => Effect == InputEffect.None ? null : Kebab(Effect.ToString());

    /// <summary>Shared <c>--field-*</c> block plus the visibility toggle. Derived components pass
    /// their own extra declarations in <paramref name="extra"/> (appended last, so they win).</summary>
    protected string? BuildRootStyle(string? extra = null)
    {
        var vars = new StyleVars("field")
            .Add("width", Width)
            .Add("font-size", FontSize)
            .Add("radius", Radius)
            .Add("border-width", BorderWidth)
            .Add("text-color", TextColor)
            .Add("bg", BackgroundColor)
            .Add("border-color", BorderColor)
            .Add("accent", AccentColor)
            .Add("focus-color", FocusColor)
            .Add("error-color", ErrorColor)
            .ToString();

        var s = (Visible ? "" : "display:none;") + vars + extra;
        return string.IsNullOrEmpty(s) ? null : s;
    }

    /// <summary>PascalCase enum name → kebab-case attribute value (<c>ShakeOnError</c> →
    /// <c>shake-on-error</c>), so multi-word members read as normal CSS attribute selectors.</summary>
    internal static string Kebab(string pascal)
    {
        var sb = new System.Text.StringBuilder(pascal.Length + 4);
        for (var i = 0; i < pascal.Length; i++)
        {
            var c = pascal[i];
            if (char.IsUpper(c) && i > 0) sb.Append('-');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
