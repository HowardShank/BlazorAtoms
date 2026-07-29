using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Buttons;

/// <summary>
/// Icon-only button — square padding, no label, and <see cref="ButtonShape.Circle"/> by default.
/// A thin wrapper over <see cref="AtomButton"/> (it renders one, rather than re-implementing the
/// chrome), so it inherits every appearance, size, and <see cref="ButtonEffect"/> unchanged.
/// </summary>
/// <remarks>
/// <see cref="ButtonFamilyBase.AriaLabel"/> is effectively required here: with no text content there
/// is nothing else to name the control, and a screen reader would announce only "button". It isn't
/// enforced at compile time — a caller may legitimately supply <c>aria-labelledby</c> or a
/// <c>title</c> through the attribute splat instead.
/// </remarks>
public partial class AtomIconButton : ButtonFamilyBase
{
    /// <summary>The glyph. An inline <c>&lt;svg&gt;</c> is sized to <c>1em</c> by the shared icon rule,
    /// so it tracks <see cref="ButtonFamilyBase.Size"/> automatically.</summary>
    [Parameter] public RenderFragment? Icon { get; set; }

    /// <summary>Circle by default — the conventional icon-button shape, and the pairing
    /// <c>[data-shape="circle"][data-icon-only]</c> needs to square the box.</summary>
    public AtomIconButton() => Shape = ButtonShape.Circle;

    /// <summary>Brands the inner button's root with this component's own class while keeping every
    /// <c>.atom-button</c> rule, then appends the caller's <see cref="AtomComponentBase.CssClass"/>.</summary>
    private string InnerCssClass =>
        string.IsNullOrEmpty(CssClass) ? "atom-icon-button" : $"atom-icon-button {CssClass}";
}
