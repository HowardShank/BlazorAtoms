using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Inputs;

/// <summary>
/// Multi-line text field over a native <c>&lt;textarea&gt;</c>. Shares the whole
/// <see cref="AtomInputBase{TValue}"/> surface with <see cref="AtomTextField"/> (label, help text,
/// <c>EditContext</c>-aware error state, <c>Variant</c>/<c>Size</c>/<c>Effect</c>, <c>--field-*</c>
/// theming) and adds sizing (<see cref="Rows"/>/<see cref="Height"/>), the native resize grip, and
/// an optional character counter. No JS — auto-grow is deliberately left out for that reason
/// (see DEVELOPMENT.md).
/// </summary>
public partial class AtomTextArea : AtomInputBase<string?>
{
    /// <summary>Placeholder shown while the field is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Visible text rows. Default 4. Ignored when <see cref="Height"/> is set.</summary>
    [Parameter] public int Rows { get; set; } = 4;

    /// <summary>Explicit height in px → <c>--field-height</c>. Overrides <see cref="Rows"/>.</summary>
    [Parameter] public double? Height { get; set; }

    /// <summary>Native <c>maxlength</c> — hard cap on typed characters. Also the denominator of the
    /// counter when <see cref="ShowCounter"/> is on. Null = no limit.</summary>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>Which axes the user may drag the resize grip along. Default
    /// <see cref="TextAreaResize.Vertical"/>.</summary>
    [Parameter] public TextAreaResize Resize { get; set; } = TextAreaResize.Vertical;

    /// <summary>Native <c>spellcheck</c>. Null omits the attribute (browser default).</summary>
    [Parameter] public bool? Spellcheck { get; set; }

    /// <summary>Which DOM event commits the value. Default
    /// <see cref="InputUpdateOn.Input"/> (every keystroke).</summary>
    [Parameter] public InputUpdateOn UpdateOn { get; set; } = InputUpdateOn.Input;

    /// <summary>Shows a character count under the field — <c>12 / 200</c> when
    /// <see cref="MaxLength"/> is set, otherwise just the count.</summary>
    [Parameter] public bool ShowCounter { get; set; }

    /// <summary>Fraction of <see cref="MaxLength"/> at which the counter switches to
    /// <c>data-state="near"</c>. Default 0.9. Ignored when <see cref="MaxLength"/> is null.</summary>
    [Parameter] public double CounterWarnAt { get; set; } = 0.9;

    // ---- derived render state ---------------------------------------------------------------

    /// <summary>A textarea honors the native <c>readonly</c> attribute, so read-only is a real state
    /// here rather than an alias of disabled.</summary>
    protected override bool SupportsNativeReadOnly => true;

    protected override string DefaultAriaLabel => "Text area";

    private string RootClass => "atom-text-area";

    /// <summary>Height is the one field-specific var, so it rides in on the base's <c>extra</c>
    /// slot rather than getting its own StyleVars pass.</summary>
    private string? RootStyle => BuildRootStyle(new StyleVars("field").Add("height", Height).ToString());

    private string ResizeAttr => Kebab(Resize.ToString());

    private string? SpellcheckAttr => Spellcheck is null ? null : Spellcheck.Value ? "true" : "false";

    private int Length => Value?.Length ?? 0;

    private string CounterText => MaxLength is null ? Length.ToString() : $"{Length} / {MaxLength}";

    /// <summary>"near" once the value crosses <see cref="CounterWarnAt"/> of the cap; null otherwise.
    /// There is no "over" state — the native <c>maxlength</c> makes it unreachable by typing.</summary>
    private string? CounterState =>
        MaxLength is > 0 && Length >= MaxLength.Value * CounterWarnAt ? "near" : null;

    // ---- interaction --------------------------------------------------------------------------

    private Task OnInputEvent(ChangeEventArgs e) =>
        UpdateOn == InputUpdateOn.Input ? SetValueAsync((string?)e.Value) : Task.CompletedTask;

    private Task OnChangeEvent(ChangeEventArgs e) =>
        UpdateOn == InputUpdateOn.Change ? SetValueAsync((string?)e.Value) : Task.CompletedTask;
}
