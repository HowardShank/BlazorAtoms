using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Inputs;

/// <summary>
/// Single-line text field over a native <c>&lt;input&gt;</c> — the plain workhorse of the family.
/// Inherits the whole shared surface from <see cref="AtomInputBase{TValue}"/> (label, help text,
/// <c>EditContext</c>-aware error state, <c>Variant</c>/<c>Size</c>/<c>Effect</c>, the
/// <c>--field-*</c> theming hooks) and adds only what a text box needs on top: the input
/// <see cref="Type"/>, placeholder/length/autocomplete, optional prefix and suffix slots, and an
/// optional clear button. No JS.
/// </summary>
public partial class AtomTextField : AtomInputBase<string?>
{
    /// <summary>Which text-like native input type to render. Default
    /// <see cref="TextFieldType.Text"/>. Changes browser affordances only — the value is always a
    /// string.</summary>
    [Parameter] public TextFieldType Type { get; set; } = TextFieldType.Text;

    /// <summary>Placeholder shown while the field is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Native <c>maxlength</c> — hard cap on typed characters. Null = no limit.</summary>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>Native <c>autocomplete</c> token (e.g. <c>email</c>, <c>new-password</c>,
    /// <c>off</c>). Null omits the attribute, leaving the browser default.</summary>
    [Parameter] public string? Autocomplete { get; set; }

    /// <summary>Native <c>inputmode</c> override (e.g. <c>numeric</c>, <c>decimal</c>) for the
    /// on-screen keyboard. Null lets <see cref="Type"/> decide.</summary>
    [Parameter] public string? InputMode { get; set; }

    /// <summary>Native <c>spellcheck</c>. Null omits the attribute (browser default).</summary>
    [Parameter] public bool? Spellcheck { get; set; }

    /// <summary>Which DOM event commits the value. Default
    /// <see cref="InputUpdateOn.Input"/> (every keystroke).</summary>
    [Parameter] public InputUpdateOn UpdateOn { get; set; } = InputUpdateOn.Input;

    /// <summary>When true, shows a clear (×) button while the field has a value and accepts input.
    /// Clearing commits <c>null</c> through the normal value path.</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>Content pinned inside the frame before the input — a currency mark, an icon, a
    /// fixed prefix. Rendered in its own <c>.atom-text-field-prefix</c> slot.</summary>
    [Parameter] public RenderFragment? PrefixContent { get; set; }

    /// <summary>Content pinned inside the frame after the input — a unit, an icon, an action.
    /// Rendered in its own <c>.atom-text-field-suffix</c> slot.</summary>
    [Parameter] public RenderFragment? SuffixContent { get; set; }

    // ---- derived render state ---------------------------------------------------------------

    /// <summary>Text-like inputs honor the native <c>readonly</c> attribute, so read-only is a real
    /// state here rather than an alias of disabled.</summary>
    protected override bool SupportsNativeReadOnly => true;

    protected override string DefaultAriaLabel => "Text field";

    private string RootClass => "atom-text-field";

    private string? RootStyle => BuildRootStyle();

    private string TypeAttr => Kebab(Type.ToString());

    /// <summary>Rendered as the string the attribute expects; null omits it entirely.</summary>
    private string? SpellcheckAttr => Spellcheck is null ? null : Spellcheck.Value ? "true" : "false";

    private bool ShowClearButton => Clearable && !BlocksInput && !string.IsNullOrEmpty(Value);

    // ---- interaction --------------------------------------------------------------------------

    /// <summary>Both DOM events are always wired and each one checks <see cref="UpdateOn"/>, rather
    /// than conditionally attaching a handler — a changed handler set would re-render the input and
    /// can cost a keystroke.</summary>
    private Task OnInputEvent(ChangeEventArgs e) =>
        UpdateOn == InputUpdateOn.Input ? SetValueAsync((string?)e.Value) : Task.CompletedTask;

    private Task OnChangeEvent(ChangeEventArgs e) =>
        UpdateOn == InputUpdateOn.Change ? SetValueAsync((string?)e.Value) : Task.CompletedTask;

    private Task ClearAsync() => SetValueAsync(null);
}
