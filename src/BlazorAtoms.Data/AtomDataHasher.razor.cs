using System.Linq.Expressions;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Data;

/// <summary>
/// Live hash / checksum panel. Type into the input, pick an algorithm from the CRC vs Cryptographic
/// groups, watch the hex digest update on every keystroke. <see cref="EditContext"/>-aware so it
/// can drop into an <c>EditForm</c> for validation-message + error-state rendering — same wiring
/// contract used by <c>AtomRangeInput</c> / <c>AtomCrtInput</c>.
/// </summary>
public partial class AtomDataHasher : AtomComponentBase, IDisposable
{
    private readonly string _inputId = $"atom-data-hasher-in-{Guid.NewGuid():N}";
    private readonly string _selectId = $"atom-data-hasher-alg-{Guid.NewGuid():N}";

    // ---- form-integration -------------------------------------------------------------------

    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    /// <summary>Text to hash.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Fires on every input event with the new text.</summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Wired automatically by <c>@bind-Value</c>.</summary>
    [Parameter] public Expression<Func<string?>>? ValueExpression { get; set; }

    /// <summary>Overrides <see cref="ValueExpression"/> when explicitly wiring validation.</summary>
    [Parameter] public Expression<Func<string?>>? ValidationFor { get; set; }

    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    // ---- algorithm --------------------------------------------------------------------------

    /// <summary>Which hash / checksum engine to compute. Default <see cref="HashAlgorithmKind.Crc32"/>.</summary>
    [Parameter] public HashAlgorithmKind Algorithm { get; set; } = HashAlgorithmKind.Crc32;

    /// <summary>Fires when the algorithm changes via the built-in picker. Two-way-binds
    /// <see cref="Algorithm"/> with <c>@bind-Algorithm</c>.</summary>
    [Parameter] public EventCallback<HashAlgorithmKind> AlgorithmChanged { get; set; }

    /// <summary>Encoding used to turn the input string into bytes before hashing. Default UTF-8.</summary>
    [Parameter] public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>When true (default), shows the built-in algorithm dropdown. Turn off if the host
    /// owns algorithm selection and drives <see cref="Algorithm"/> externally.</summary>
    [Parameter] public bool ShowAlgorithmPicker { get; set; } = true;

    /// <summary>Label rendered above the algorithm picker. Default <c>"Algorithm"</c>.</summary>
    [Parameter] public string AlgorithmLabel { get; set; } = "Algorithm";

    /// <summary>Label rendered above the result panel. Default <c>"Result"</c>.</summary>
    [Parameter] public string ResultLabel { get; set; } = "Result";

    // ---- structure --------------------------------------------------------------------------

    /// <summary>Form label.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Responsive classes for the label column. Default <c>clr-col-12 clr-col-md-2</c>.</summary>
    [Parameter] public string LabelCol { get; set; } = "clr-col-12 clr-col-md-2";

    /// <summary>Responsive classes for the control column. Default <c>clr-col-12 clr-col-md-10</c>.</summary>
    [Parameter] public string ControlCol { get; set; } = "clr-col-12 clr-col-md-10";

    /// <summary>Help text shown under the control. Replaced by the first validation message on error.</summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>Placeholder text on the input.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Accessible label; falls back to <see cref="Label"/> when null.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // ---- state ------------------------------------------------------------------------------

    /// <summary>When true, greys out and blocks input.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Alias of <see cref="Disabled"/>.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>When false, hidden via CSS <c>display:none</c>. Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    // ---- layout -----------------------------------------------------------------------------

    /// <summary>When true (default), renders a <c>&lt;textarea&gt;</c>; when false, a single-line
    /// <c>&lt;input type="text"&gt;</c>.</summary>
    [Parameter] public bool Multiline { get; set; } = true;

    /// <summary>Rows for the textarea (multiline only). Default 5 — matches the sample.</summary>
    [Parameter] public int Rows { get; set; } = 5;

    /// <summary>Optional explicit width in px. Maps to CSS custom property <c>--hasher-width</c>.</summary>
    [Parameter] public double? Width { get; set; }

    // ---- appearance -------------------------------------------------------------------------

    /// <summary>Text color for the result digest (any CSS color). Maps to <c>--hasher-result-color</c>.</summary>
    [Parameter] public string? ResultColor { get; set; }

    /// <summary>Background color for the result panel. Maps to <c>--hasher-result-bg</c>.</summary>
    [Parameter] public string? ResultBackgroundColor { get; set; }

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

    private string RootClass => "atom-data-hasher";

    private bool IsDisabled => Disabled || ReadOnly;

    private string? State => HasError ? "error" : (IsDisabled ? "disabled" : null);

    private string EffectiveAriaLabel => AriaLabel ?? Label ?? "Data hasher input";

    private string AlgorithmAttr => Algorithm switch
    {
        HashAlgorithmKind.Crc32 => "crc32",
        HashAlgorithmKind.Crc64 => "crc64",
        HashAlgorithmKind.Md5 => "md5",
        HashAlgorithmKind.Sha256 => "sha256",
        HashAlgorithmKind.Sha512 => "sha512",
        _ => "unknown",
    };

    private string AlgorithmDisplay => Algorithm switch
    {
        HashAlgorithmKind.Crc32 => "CRC-32",
        HashAlgorithmKind.Crc64 => "CRC-64",
        HashAlgorithmKind.Md5 => "MD5",
        HashAlgorithmKind.Sha256 => "SHA-256",
        HashAlgorithmKind.Sha512 => "SHA-512",
        _ => Algorithm.ToString(),
    };

    /// <summary>Public convenience: current digest for the current value + algorithm. Same string
    /// that renders inside the panel. Empty when the value is empty.</summary>
    public string ResultText
    {
        get
        {
            if (string.IsNullOrEmpty(Value)) return string.Empty;
            try { return HashComputer.Compute(Algorithm, Value, Encoding); }
            catch (Exception ex) { return $"Error: {ex.Message}"; }
        }
    }

    private string? RootStyle
    {
        get
        {
            var sb = new StringBuilder();
            if (!Visible) sb.Append("display:none;");
            if (Width is not null) sb.Append($"--hasher-width:{Width.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}px;");
            if (!string.IsNullOrEmpty(ResultColor)) sb.Append($"--hasher-result-color:{ResultColor};");
            if (!string.IsNullOrEmpty(ResultBackgroundColor)) sb.Append($"--hasher-result-bg:{ResultBackgroundColor};");
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

    private async Task OnAlgorithmChanged(ChangeEventArgs e)
    {
        if (IsDisabled) return;

        var raw = e.Value?.ToString();
        if (!Enum.TryParse<HashAlgorithmKind>(raw, ignoreCase: true, out var parsed)) return;
        if (parsed == Algorithm) return;

        Algorithm = parsed;
        await AlgorithmChanged.InvokeAsync(parsed);
    }
}
