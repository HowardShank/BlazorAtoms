using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Inputs;

/// <summary>
/// Numeric field over a native <c>&lt;input type="number"&gt;</c> — free keyboard/spinner/step
/// support, no JS. <c>TValue</c> spans <c>int</c>, <c>long</c>, <c>short</c>, <c>float</c>,
/// <c>double</c>, <c>decimal</c> and their nullable variants (no constraint, same shape as Blazor's
/// own <c>InputNumber&lt;TValue&gt;</c>). With a nullable <c>TValue</c> an empty box is a real
/// <c>null</c> value; with a non-nullable one, clearing the box leaves <c>Value</c> untouched.
/// </summary>
public partial class AtomNumberField<TValue> : AtomInputBase<TValue>
{
    /// <summary>Native <c>min</c>. Null omits the attribute (no lower bound).</summary>
    /// <remarks>These bounds are <c>double?</c> rather than <c>TValue</c> — unlike
    /// <see cref="AtomRangeInput{TValue}"/>, where a slider must always have both ends, a number
    /// field's bounds are optional, and an unconstrained generic <c>TValue</c> has no way to express
    /// "unset" for a value type.</remarks>
    [Parameter] public double? Min { get; set; }

    /// <summary>Native <c>max</c>. Null omits the attribute (no upper bound).</summary>
    [Parameter] public double? Max { get; set; }

    /// <summary>Native <c>step</c> — the spinner increment and the validity grid. Null omits the
    /// attribute, which the browser treats as <c>1</c> (integers only). Pass <c>0.01</c> for
    /// currency, or <c>null</c>-with-a-decimal-<c>TValue</c> is a common mistake worth avoiding.</summary>
    [Parameter] public double? Step { get; set; }

    /// <summary>Placeholder shown while the field is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>When false, hides the browser's spinner arrows (the field still accepts arrow-key
    /// stepping). Default true.</summary>
    [Parameter] public bool ShowSpinners { get; set; } = true;

    /// <summary>Static text inside the frame before the input — a currency mark, a sign.</summary>
    [Parameter] public string? PrefixText { get; set; }

    /// <summary>Static text inside the frame after the input — a unit (<c>kg</c>, <c>%</c>).</summary>
    [Parameter] public string? SuffixText { get; set; }

    /// <summary>Which DOM event commits the value. Default
    /// <see cref="InputUpdateOn.Input"/> (every keystroke/spinner click).</summary>
    [Parameter] public InputUpdateOn UpdateOn { get; set; } = InputUpdateOn.Input;

    // ---- derived render state ---------------------------------------------------------------

    /// <summary>A number input honors the native <c>readonly</c> attribute.</summary>
    protected override bool SupportsNativeReadOnly => true;

    protected override string DefaultAriaLabel => "Number field";

    private string RootClass => "atom-number-field";

    private string? RootStyle => BuildRootStyle();

    private string FormattedValue => NumberConvert.Format(Value);

    private string? MinAttr => NumberConvert.FormatBound(Min);
    private string? MaxAttr => NumberConvert.FormatBound(Max);
    private string? StepAttr => NumberConvert.FormatBound(Step);

    /// <summary>Only emitted when spinners are suppressed, so the default costs no attribute.</summary>
    private string? SpinnersAttr => ShowSpinners ? null : "hidden";

    // ---- interaction --------------------------------------------------------------------------

    private Task OnInputEvent(ChangeEventArgs e) =>
        UpdateOn == InputUpdateOn.Input ? CommitAsync(e) : Task.CompletedTask;

    private Task OnChangeEvent(ChangeEventArgs e) =>
        UpdateOn == InputUpdateOn.Change ? CommitAsync(e) : Task.CompletedTask;

    /// <summary>Unparseable input is dropped rather than throwing or zeroing the value — the browser
    /// can hold text this <c>TValue</c> can't represent (e.g. <c>3.7</c> in an <c>int</c> field, or a
    /// cleared box with a non-nullable <c>TValue</c>).</summary>
    private Task CommitAsync(ChangeEventArgs e) =>
        NumberConvert.TryParse<TValue>((string?)e.Value, out var parsed)
            ? SetValueAsync(parsed)
            : Task.CompletedTask;
}
