using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Inputs;

/// <summary>
/// A labeled slider/range input with help text, min/max/step, and an <see cref="EditContext"/>-aware
/// error state — the first form/validation-integrated component in BlazorAtoms. Renders a native
/// <c>&lt;input type="range"&gt;</c> (free pointer/touch/keyboard support, no JS) styled via CSS
/// custom properties. <see cref="Disabled"/> means "don't render at all"; <see cref="ReadOnly"/>
/// means "render greyed out, no input" — these are deliberately different (see DEVELOPMENT.md).
/// </summary>
public partial class AtomRangeInput<TValue> : AtomComponentBase, IDisposable
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    /// <summary>The current value. Bind with <c>@bind-Value</c>.</summary>
    [Parameter] public TValue Value { get; set; } = default!;

    /// <summary>Raised when the value changes. Backs <c>@bind-Value</c>; only needed directly when
    /// not using <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>Identifies the bound value. Populated automatically by <c>@bind-Value</c>; required
    /// if you use <see cref="ValueChanged"/> directly instead.</summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>Minimum value (inclusive). Must be less than <see cref="Max"/>.</summary>
    [Parameter] public TValue Min { get; set; } = default!;

    /// <summary>Maximum value (inclusive). Must be greater than or equal to <see cref="Min"/>.</summary>
    [Parameter] public TValue Max { get; set; } = default!;

    /// <summary>Amount the value changes per tick.</summary>
    [Parameter] public TValue Step { get; set; } = default!;

    /// <summary>Form label.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Responsive classes for the label column. Default <c>clr-col-12 clr-col-md-2</c>.</summary>
    [Parameter] public string LabelCol { get; set; } = "clr-col-12 clr-col-md-2";

    /// <summary>Responsive classes for the control column. Default <c>clr-col-12 clr-col-md-10</c>.</summary>
    [Parameter] public string ControlCol { get; set; } = "clr-col-12 clr-col-md-10";

    /// <summary>Help text shown under the control when there is no validation error.</summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>Identifies the property used for form validation — wires this component to an
    /// ancestor <see cref="EditContext"/> (e.g. from <c>&lt;EditForm&gt;</c> +
    /// <c>&lt;DataAnnotationsValidator /&gt;</c>). Falls back to <see cref="ValueExpression"/> when
    /// not set, so a plain <c>@bind-Value</c> inside an <c>EditForm</c> still participates.</summary>
    [Parameter] public Expression<Func<TValue>>? ValidationFor { get; set; }

    /// <summary>When true, the component renders nothing at all — distinct from
    /// <see cref="ReadOnly"/>.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>When true, the component still renders (greyed out) but blocks input.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Shape of the drag handle. Default <see cref="HandleShape.Round"/>.</summary>
    [Parameter] public HandleShape HandleShape { get; set; } = HandleShape.Round;

    /// <summary>Track width in px. Maps to <c>--range-track-width</c>.</summary>
    [Parameter] public double? TrackWidth { get; set; }

    /// <summary>Track height in px. Maps to <c>--range-track-height</c>.</summary>
    [Parameter] public double? TrackHeight { get; set; }

    /// <summary>Handle size in px (width = height). Maps to <c>--range-handle-size</c>.</summary>
    [Parameter] public double? HandleSize { get; set; }

    /// <summary>Accessible label for the slider. Falls back to <see cref="Label"/> when null.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // ---- EditContext wiring -------------------------------------------------------------------

    protected override void OnInitialized()
    {
        if (CascadedEditContext is not null)
            CascadedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
    }

    protected override void OnParametersSet()
    {
        if (Comparer<TValue>.Default.Compare(Min, Max) >= 0)
            throw new ArgumentException($"{nameof(Min)} ({Min}) must be less than {nameof(Max)} ({Max}).");

        var expr = ValidationFor ?? ValueExpression;
        _hasFieldIdentifier = expr is not null;
        _fieldIdentifier = expr is not null ? FieldIdentifier.Create(expr) : default;
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) =>
        StateHasChanged();

    public void Dispose()
    {
        if (CascadedEditContext is not null)
            CascadedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
    }

    // ---- derived render state ---------------------------------------------------------------

    private bool HasError =>
        _hasFieldIdentifier && CascadedEditContext is not null &&
        CascadedEditContext.GetValidationMessages(_fieldIdentifier).Any();

    private string? ErrorMessage =>
        HasError ? CascadedEditContext!.GetValidationMessages(_fieldIdentifier).FirstOrDefault() : null;

    private string? DisplayText => HasError ? ErrorMessage : HelpText;

    private string RootClass => "atom-range-input";

    private string? State => HasError ? "error" : (ReadOnly ? "readonly" : null);

    private double ValueDouble => RangeConvert.ToDouble(Value);
    private double MinDouble => RangeConvert.ToDouble(Min);
    private double MaxDouble => RangeConvert.ToDouble(Max);
    private double StepDouble => RangeConvert.ToDouble(Step);

    private string EffectiveAriaLabel => AriaLabel ?? Label ?? "Range";

    private string HandleShapeAttr => HandleShape.ToString().ToLowerInvariant();

    private string? RootStyle
    {
        get
        {
            var s = new StyleVars("range")
                .Add("track-width", TrackWidth)
                .Add("track-height", TrackHeight)
                .Add("handle-size", HandleSize)
                .ToString();
            return string.IsNullOrEmpty(s) ? null : s;
        }
    }

    // ---- interaction --------------------------------------------------------------------------

    // Native <input type="range"> ignores the HTML `readonly` attribute per spec (only text-like
    // inputs support it) — ReadOnly is enforced via the native `disabled` attribute on the input
    // itself instead, while the component's own root/label/help-text still render normally.
    private async Task OnInput(ChangeEventArgs e)
    {
        if (Disabled || ReadOnly) return;
        if (!double.TryParse((string?)e.Value, NumberStyles.Float, Inv, out var d)) return;

        var newValue = RangeConvert.FromDouble<TValue>(d);
        if (Equals(newValue, Value)) return;

        Value = newValue;
        await ValueChanged.InvokeAsync(newValue);
        if (_hasFieldIdentifier)
            CascadedEditContext?.NotifyFieldChanged(_fieldIdentifier);
    }
}
