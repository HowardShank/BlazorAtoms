using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Inputs;

/// <summary>
/// Single-choice group of native <c>&lt;input type="radio"&gt;</c>s sharing one <c>name</c>, which is
/// what buys free mutual exclusivity and arrow-key navigation from the platform. Options come from a
/// plain <see cref="Options"/> sequence — labelled via <see cref="OptionLabel"/> or rendered by
/// <see cref="OptionTemplate"/> — so no child component or cascading context is needed. No JS.
/// </summary>
public partial class AtomRadioGroup<TValue> : AtomInputBase<TValue>
{
    /// <summary>Generated once per instance so two groups on a page can't share a native
    /// <c>name</c> (which would make them mutually exclusive). Same approach as Blazor's own
    /// <c>InputRadioGroup</c>. Set <see cref="Name"/> to control it.</summary>
    private readonly string _generatedName = Guid.NewGuid().ToString("N");

    /// <summary>The selectable values, in render order. Null or empty renders an empty group.</summary>
    [Parameter] public IEnumerable<TValue>? Options { get; set; }

    /// <summary>Turns an option into its caption. Default is the value's <c>ToString()</c>
    /// (<c>""</c> for null).</summary>
    [Parameter] public Func<TValue, string>? OptionLabel { get; set; }

    /// <summary>Renders an option's caption as markup instead of text — an icon plus a description,
    /// a swatch. Wins over <see cref="OptionLabel"/> when both are set.</summary>
    [Parameter] public RenderFragment<TValue>? OptionTemplate { get; set; }

    /// <summary>Per-option predicate for disabling individual choices while the rest stay live.
    /// The whole-control <see cref="AtomInputBase{TValue}.Disabled"/> still disables everything.</summary>
    [Parameter] public Func<TValue, bool>? OptionDisabled { get; set; }

    /// <summary>Native <c>name</c> shared by the radios. Defaults to a per-instance generated value;
    /// set it only when the markup needs a stable, predictable name (e.g. a non-Blazor form post).</summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>Option layout axis. Default <see cref="Inputs.Orientation.Vertical"/> (stacked) —
    /// the conventional shape for a radio list, and the opposite of
    /// <see cref="AtomRangeInput{TValue}"/>'s default.</summary>
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>Which side of each radio its caption sits on. Default
    /// <see cref="LabelPlacement.End"/>.</summary>
    [Parameter] public LabelPlacement TextPlacement { get; set; } = LabelPlacement.End;

    /// <summary>Radio mark diameter in px → <c>--field-control-size</c>. Defaults to the
    /// <see cref="AtomInputBase{TValue}.Size"/> preset.</summary>
    [Parameter] public double? MarkSize { get; set; }

    // ---- derived render state ---------------------------------------------------------------

    // SupportsNativeReadOnly stays false: `readonly` is ignored on a radio, so ReadOnly renders as
    // the native `disabled` attribute on every option.

    protected override string DefaultAriaLabel => "Options";

    private string RootClass => "atom-radio-group";

    private string? RootStyle => BuildRootStyle(new StyleVars("field").Add("control-size", MarkSize).ToString());

    private string OrientationAttr => Kebab(Orientation.ToString());

    private string PlacementAttr => Kebab(TextPlacement.ToString());

    private string EffectiveName => string.IsNullOrEmpty(Name) ? _generatedName : Name;

    private IEnumerable<TValue> EffectiveOptions => Options ?? [];

    private string OptionText(TValue option) =>
        OptionLabel is not null ? OptionLabel(option) : option?.ToString() ?? "";

    /// <summary>The native <c>value</c> attribute. Informational only — selection is matched on the
    /// <c>TValue</c> itself in <see cref="SelectAsync"/>, never by parsing this back, so options whose
    /// <c>ToString()</c> collides still behave correctly.</summary>
    private string OptionKey(TValue option) => option?.ToString() ?? "";

    private bool IsSelected(TValue option) => EqualityComparer<TValue>.Default.Equals(option, Value);

    private bool IsOptionDisabled(TValue option) =>
        IsDisabled || (OptionDisabled is not null && OptionDisabled(option));

    // ---- interaction --------------------------------------------------------------------------

    /// <summary>The handler closes over the option, so the selected value round-trips as
    /// <c>TValue</c> and never through a string — which is why this works for any type, including
    /// records and enums.</summary>
    private Task SelectAsync(TValue option) => SetValueAsync(option);
}
