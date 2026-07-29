using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Inputs;

/// <summary>
/// Dropdown over a native <c>&lt;select&gt;</c> — the platform's own popup list, which means correct
/// keyboard/type-ahead behavior and a native picker on mobile, with no JS and no positioning code.
/// Options come from a plain <see cref="Options"/> sequence; <see cref="ChildContent"/> is an escape
/// hatch for hand-written <c>&lt;option&gt;</c>/<c>&lt;optgroup&gt;</c> markup.
/// </summary>
/// <remarks>
/// Multi-select is deliberately not supported: <c>multiple</c> would need <c>TValue</c> to be a
/// collection and a different change-event shape, which is a separate component rather than a flag
/// (see DEVELOPMENT.md). A styled/searchable dropdown is likewise out of scope — that's
/// <c>BlazorAtoms.Overlays.AtomDropdown</c>, which needs positioning JS.
/// </remarks>
public partial class AtomSelect<TValue> : AtomInputBase<TValue>
{
    /// <summary>The selectable values, in render order.</summary>
    [Parameter] public IEnumerable<TValue>? Options { get; set; }

    /// <summary>Turns an option into its display text. Default is the value's <c>ToString()</c>.</summary>
    [Parameter] public Func<TValue, string>? OptionLabel { get; set; }

    /// <summary>Per-option predicate for greying out individual choices.</summary>
    [Parameter] public Func<TValue, bool>? OptionDisabled { get; set; }

    /// <summary>Text for a leading empty option ("Choose one…"). Omitted when null.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>When true, the placeholder option can be re-selected to clear the value — which only
    /// works for a <c>TValue</c> that can be empty (nullable or string). Default false, which renders
    /// it <c>disabled</c>: visible as a prompt but not choosable.</summary>
    [Parameter] public bool PlaceholderSelectable { get; set; }

    /// <summary>Raw <c>&lt;option&gt;</c>/<c>&lt;optgroup&gt;</c> markup appended after
    /// <see cref="Options"/>. Values selected here resolve by conversion rather than by matching an
    /// option object — see the parsing note in DEVELOPMENT.md.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    // ---- derived render state ---------------------------------------------------------------

    // SupportsNativeReadOnly stays false: the HTML spec has no `readonly` for <select>, so ReadOnly
    // renders as the native `disabled` attribute.

    protected override string DefaultAriaLabel => "Select";

    private string RootClass => "atom-select";

    private string? RootStyle => BuildRootStyle();

    private IEnumerable<TValue> EffectiveOptions => Options ?? [];

    private string OptionText(TValue option) =>
        OptionLabel is not null ? OptionLabel(option) : option?.ToString() ?? "";

    /// <summary>An option's <c>value</c> attribute — invariant culture so a <c>de-DE</c> user doesn't
    /// get <c>1,5</c> here and a failed round-trip on the way back.</summary>
    private string OptionKey(TValue option) => option switch
    {
        null => "",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => option.ToString() ?? "",
    };

    private string SelectedKey => OptionKey(Value);

    private bool IsOptionDisabled(TValue option) =>
        OptionDisabled is not null && OptionDisabled(option);

    // ---- interaction --------------------------------------------------------------------------

    private Task OnChangeEvent(ChangeEventArgs e) =>
        TryResolve((string?)e.Value, out var resolved) ? SetValueAsync(resolved) : Task.CompletedTask;

    /// <summary>Turns the browser's selected <c>value</c> string back into a <c>TValue</c>.
    /// <see cref="Options"/> is searched first so the exact option instance is returned — that keeps
    /// reference types and records working, which a pure string conversion could not. Only values
    /// from <see cref="ChildContent"/> (or an option list that changed since render) fall through to
    /// conversion.</summary>
    private bool TryResolve(string? raw, out TValue result)
    {
        foreach (var option in EffectiveOptions)
        {
            if (OptionKey(option) == raw)
            {
                result = option;
                return true;
            }
        }

        var target = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

        if (string.IsNullOrEmpty(raw))
        {
            // The empty placeholder. Only a TValue that can actually hold "nothing" may take it;
            // otherwise the selection is ignored rather than defaulted to 0/first-enum-member.
            result = default!;
            return Nullable.GetUnderlyingType(typeof(TValue)) is not null || !typeof(TValue).IsValueType;
        }

        try
        {
            result = target.IsEnum
                ? (TValue)Enum.Parse(target, raw, ignoreCase: true)
                : (TValue)Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidCastException or ArgumentException)
        {
            result = default!;
            return false;
        }
    }
}
