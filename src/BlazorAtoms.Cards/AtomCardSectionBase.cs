using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Cards;

/// <summary>
/// Shared surface for <see cref="AtomCard"/>'s three structural sections
/// (<see cref="AtomCardHeader"/>, <see cref="AtomCardBody"/>, <see cref="AtomCardFooter"/>): content,
/// per-section padding and background, and the divider rule — plus the fallback to an enclosing
/// card's <see cref="CardContext"/>.
/// </summary>
/// <remarks>
/// <para>Every inherited-from-the-card parameter is <b>nullable</b> on purpose. Null means "not set",
/// so the context is consulted; a non-null value wins outright. That makes the precedence a plain
/// <c>??</c> chain, with no need to detect whether a parameter was explicitly supplied (contrast
/// <c>ButtonFamilyBase</c>, which has non-nullable enum axes and so must inspect the
/// <c>ParameterView</c>).</para>
/// <para>These sections work standalone too — outside an <see cref="AtomCard"/> the context is simply
/// null and the CSS defaults apply.</para>
/// </remarks>
public abstract class AtomCardSectionBase : AtomComponentBase
{
    [CascadingParameter] protected CardContext? Card { get; set; }

    /// <summary>Section content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Inner padding in px → <c>--card-section-padding</c>. Null (default) inherits the
    /// enclosing card's padding, then the CSS default.</summary>
    [Parameter] public double? Padding { get; set; }

    /// <summary>Section background (any CSS color) → <c>--card-section-bg</c>. Null (default) is
    /// transparent, so the card's own background shows through.</summary>
    [Parameter] public string? BackgroundColor { get; set; }

    /// <summary>Padding actually applied: this section's, else the card's, else null (CSS default).</summary>
    protected double? EffectivePadding => Padding ?? Card?.Padding;

    /// <summary>Resolves a section's own <c>Divider</c> against the card's default: the section's value
    /// if set, else the card's, else true. Returns the <c>data-divider</c> attribute value, emitted only
    /// when the rule is on.</summary>
    /// <remarks>
    /// A helper here rather than a <c>Divider</c> parameter on this base, because
    /// <see cref="AtomCardBody"/> draws no rule of its own — it is what the other two separate. On the
    /// base it would be a parameter that silently does nothing on one of the three sections, the same
    /// reason <c>Radius</c> is not on <c>AtomProgressBase</c>.
    /// </remarks>
    protected string? DividerAttr(bool? divider) =>
        (divider ?? Card?.Divider ?? true) ? "true" : null;

    /// <summary>The section's own custom-property block. Derived components append their own in
    /// <paramref name="extra"/> (last, so it wins).</summary>
    protected string? BuildSectionStyle(string? extra = null)
    {
        var s = new StyleVars("card-section")
            .Add("padding", EffectivePadding)
            .Add("bg", BackgroundColor)
            .ToString() + extra;

        return string.IsNullOrEmpty(s) ? null : s;
    }
}
