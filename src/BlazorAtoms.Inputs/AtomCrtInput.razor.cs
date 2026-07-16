using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Inputs;

/// <summary>
/// A flexible CRT-terminal-styled text input — phosphor color, glow, scanlines, bezel, and blinking
/// caret, over a plain native <c>&lt;textarea&gt;</c> (or <c>&lt;input type="text"&gt;</c> when
/// <see cref="Multiline"/> is false). No JS. Same <see cref="EditContext"/>-aware validation
/// contract as <see cref="AtomRangeInput{TValue}"/>.
/// </summary>
public partial class AtomCrtInput : AtomComponentBase, IDisposable
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // ---- form-integration -------------------------------------------------------------------

    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    /// <summary>Current text value.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Callback when the text changes (invoked on every input event).</summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Populated automatically by <c>@bind-Value</c>; identifies the bound field for
    /// <see cref="EditContext"/> participation.</summary>
    [Parameter] public Expression<Func<string?>>? ValueExpression { get; set; }

    /// <summary>Overrides <see cref="ValueExpression"/> when explicitly wiring validation.</summary>
    [Parameter] public Expression<Func<string?>>? ValidationFor { get; set; }

    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    // ---- structure --------------------------------------------------------------------------

    /// <summary>Form label.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Responsive classes for the label column. Default <c>clr-col-12 clr-col-md-2</c>.</summary>
    [Parameter] public string LabelCol { get; set; } = "clr-col-12 clr-col-md-2";

    /// <summary>Responsive classes for the control column. Default <c>clr-col-12 clr-col-md-10</c>.</summary>
    [Parameter] public string ControlCol { get; set; } = "clr-col-12 clr-col-md-10";

    /// <summary>Help text shown under the control. Replaced by the first validation message on error.</summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>Placeholder text.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Accessible label; falls back to <see cref="Label"/> when null.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // ---- state ------------------------------------------------------------------------------

    /// <summary>When true, greys out and blocks input.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Alias of <see cref="Disabled"/>.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>When false, hidden via CSS <c>display:none</c> (stays in the DOM). Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    // ---- layout -----------------------------------------------------------------------------

    /// <summary>When true (default), renders a <c>&lt;textarea&gt;</c>; when false, a single-line
    /// <c>&lt;input type="text"&gt;</c>. Same phosphor/glow/font styling either way.</summary>
    [Parameter] public bool Multiline { get; set; } = true;

    /// <summary>Rows for the textarea (multiline only). Default 4.</summary>
    [Parameter] public int Rows { get; set; } = 4;

    /// <summary>Column hint (multiline only, ignored when <see cref="Width"/> is set).</summary>
    [Parameter] public int? Cols { get; set; }

    /// <summary>Explicit width in px. Overrides <see cref="Cols"/>. Maps to <c>--crt-width</c>.</summary>
    [Parameter] public double? Width { get; set; }

    /// <summary>Explicit height in px (multiline only, overrides <see cref="Rows"/>). Maps to
    /// <c>--crt-height</c>.</summary>
    [Parameter] public double? Height { get; set; }

    /// <summary>Font size in px. Maps to <c>--crt-font-size</c>.</summary>
    [Parameter] public double? FontSize { get; set; }

    // ---- CRT appearance ---------------------------------------------------------------------

    /// <summary>Phosphor color of the text/glow/caret. Default <see cref="CrtPhosphor.Green"/>.
    /// Overridden by an explicit <see cref="Color"/> when set.</summary>
    [Parameter] public CrtPhosphor Phosphor { get; set; } = CrtPhosphor.Green;

    /// <summary>Explicit text/glow/caret color (any CSS color: hex, rgb, named). Overrides the
    /// <see cref="Phosphor"/> preset when set. Maps to <c>--crt-color</c>.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>Explicit screen background color (any CSS color). Overrides the phosphor's default
    /// tinted background. Maps to <c>--crt-bg</c>.</summary>
    [Parameter] public string? BackgroundColor { get; set; }

    /// <summary>Font. <see cref="CrtFont.System"/> (default) uses a monospace stack;
    /// <see cref="CrtFont.Vt323"/>/<see cref="CrtFont.PressStart2P"/> require the matching
    /// <c>.woff2</c> file bundled in the library's <c>wwwroot/fonts/</c> folder.</summary>
    [Parameter] public CrtFont Font { get; set; } = CrtFont.System;

    /// <summary>Phosphor glow via <c>text-shadow</c>. Default true.</summary>
    [Parameter] public bool Glow { get; set; } = true;

    /// <summary>Faint horizontal scanline overlay. Default true.</summary>
    [Parameter] public bool Scanlines { get; set; } = true;

    /// <summary>Rounded metallic monitor-bezel frame around the input. Default true.</summary>
    [Parameter] public bool Bezel { get; set; } = true;

    /// <summary>Phosphor-colored blinking caret. Default true. When false, the caret is hidden
    /// (<c>caret-color: transparent</c>).</summary>
    [Parameter] public bool CursorBlink { get; set; } = true;

    // ---- EditContext wiring -----------------------------------------------------------------

    protected override void OnInitialized()
    {
        if (CascadedEditContext is not null)
            CascadedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
    }

    protected override void OnParametersSet()
    {
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

    private string RootClass => "atom-crt-input";

    private bool IsDisabled => Disabled || ReadOnly;

    private string? State => HasError ? "error" : (IsDisabled ? "disabled" : null);

    private string EffectiveAriaLabel => AriaLabel ?? Label ?? "Terminal input";

    private string PhosphorAttr => Phosphor.ToString().ToLowerInvariant();

    private string FontAttr => Font switch
    {
        CrtFont.Vt323 => "vt323",
        CrtFont.PressStart2P => "press-start-2p",
        _ => "system",
    };

    private string? RootStyle
    {
        get
        {
            var sb = new StringBuilder();
            if (!Visible) sb.Append("display:none;");
            if (Width is not null) sb.Append($"--crt-width:{Width.Value.ToString(Inv)}px;");
            if (Height is not null) sb.Append($"--crt-height:{Height.Value.ToString(Inv)}px;");
            if (FontSize is not null) sb.Append($"--crt-font-size:{FontSize.Value.ToString(Inv)}px;");
            if (!string.IsNullOrEmpty(Color)) sb.Append($"--crt-color:{Color};");
            if (!string.IsNullOrEmpty(BackgroundColor)) sb.Append($"--crt-bg:{BackgroundColor};");
            return sb.Length == 0 ? null : sb.ToString();
        }
    }

    // ---- interaction ------------------------------------------------------------------------

    private async Task OnInput(ChangeEventArgs e)
    {
        if (IsDisabled) return;

        var newValue = e.Value as string;
        if (string.Equals(newValue, Value, StringComparison.Ordinal)) return;

        Value = newValue;
        await ValueChanged.InvokeAsync(newValue);
        if (_hasFieldIdentifier)
            CascadedEditContext?.NotifyFieldChanged(_fieldIdentifier);
    }
}
