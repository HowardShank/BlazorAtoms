using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Buttons;

/// <summary>
/// A button that stays in. <c>@bind-Value</c> holds the pressed state, which is reported as
/// <c>aria-pressed</c> — the correct role for a toolbar toggle (bold, mute, pin), as opposed to
/// <c>BlazorAtoms.Inputs.AtomSwitch</c>, which is a form field with a value to submit.
/// </summary>
/// <remarks>
/// No <c>Href</c>: a link that toggles state is a contradiction, so the parameter is inherited but
/// deliberately not forwarded to the inner <see cref="AtomButton"/>.
/// </remarks>
public partial class AtomToggleButton : ButtonFamilyBase
{
    /// <summary>Pressed state. Bind with <c>@bind-Value</c>.</summary>
    [Parameter] public bool Value { get; set; }

    /// <summary>Raised when the state flips. Backs <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<bool> ValueChanged { get; set; }

    /// <summary>Label shown while off. Also the fallback while on, unless
    /// <see cref="PressedText"/> is set.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Label shown while on — for a toggle whose wording changes ("Follow" → "Following").
    /// Null keeps <see cref="Text"/> in both states.</summary>
    [Parameter] public string? PressedText { get; set; }

    /// <summary>Content shown while off. Wins over <see cref="Text"/>.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Content shown while on. Null keeps <see cref="ChildContent"/> in both states.</summary>
    [Parameter] public RenderFragment? PressedContent { get; set; }

    /// <summary>Optional glyph before the label, in the shared icon slot.</summary>
    [Parameter] public RenderFragment? Icon { get; set; }

    /// <summary>Squares the padding for a glyph-only toggle (a toolbar bold/italic button).</summary>
    [Parameter] public bool IconOnly { get; set; }

    // ---- derived render state ---------------------------------------------------------------

    /// <summary>State-appropriate content: the pressed slot when on and supplied, else the base one.</summary>
    private RenderFragment? ActiveContent => Value ? PressedContent ?? ChildContent : ChildContent;

    /// <summary>State-appropriate label. Ignored when <see cref="ActiveContent"/> is non-null.</summary>
    private string? ActiveText => Value ? PressedText ?? Text : Text;

    private string InnerCssClass =>
        string.IsNullOrEmpty(CssClass) ? "atom-toggle-button" : $"atom-toggle-button {CssClass}";

    // ---- interaction --------------------------------------------------------------------------

    /// <summary>Flips the state, then raises the caller's <see cref="ButtonFamilyBase.OnClick"/> — in
    /// that order, so a handler reading <see cref="Value"/> sees the new state.</summary>
    private async Task ToggleAsync(MouseEventArgs e)
    {
        Value = !Value;
        await ValueChanged.InvokeAsync(Value);
        await OnClick.InvokeAsync(e);
    }
}
