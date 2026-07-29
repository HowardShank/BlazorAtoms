using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Buttons;

/// <summary>
/// The family's workhorse: a native <c>&lt;button&gt;</c> — or an <c>&lt;a href&gt;</c> when
/// <see cref="ButtonFamilyBase.Href"/> is set — carrying the shared color/appearance/size/shape axes,
/// icon slots, a <see cref="ButtonFamilyBase.Loading"/> state, and the opt-in
/// <see cref="ButtonEffect"/> decorations. No JS: every effect is CSS, and even the click ripple gets
/// its origin from <see cref="MouseEventArgs"/>.
/// </summary>
/// <remarks>
/// The rest of this package renders <b>through</b> this component rather than re-implementing the
/// chrome (<see cref="AtomIconButton"/> and <see cref="AtomToggleButton"/> forward to it;
/// <see cref="AtomSplitButton"/> hosts two of them). That keeps the whole visual contract — variants,
/// sizes, shapes, all seven effects — in this one component's scoped CSS instead of copied five ways.
/// </remarks>
public partial class AtomButton : ButtonFamilyBase
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Button content. Wins over <see cref="Text"/> when both are set.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Plain-text label — the shorthand for a content-only <c>ChildContent</c>.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Content before the label, in its own <c>.atom-button-icon-start</c> slot.</summary>
    [Parameter] public RenderFragment? StartIcon { get; set; }

    /// <summary>Content after the label, in its own <c>.atom-button-icon-end</c> slot.</summary>
    [Parameter] public RenderFragment? EndIcon { get; set; }

    /// <summary>Squares the padding for a content-only-icon button and lets
    /// <see cref="ButtonShape.Circle"/> track height. Set for you by
    /// <see cref="AtomIconButton"/>.</summary>
    [Parameter] public bool IconOnly { get; set; }

    /// <summary>Toggle state. Non-null turns this into a toggle: emits <c>aria-pressed</c> and
    /// <c>data-pressed</c>. Left null (the default) so a plain button never claims toggle semantics.
    /// Set for you by <see cref="AtomToggleButton"/>.</summary>
    [Parameter] public bool? Pressed { get; set; }

    // ---- ripple state -------------------------------------------------------------------------

    /// <summary>Click counter. Doubles as the render key that restarts the ripple keyframe and as the
    /// "has ever been clicked" flag that keeps the span out of the DOM until it's needed.</summary>
    private int RippleKey { get; set; }

    private double _rippleX;
    private double _rippleY;

    private string RippleStyle =>
        $"--btn-ripple-x:{_rippleX.ToString("0.##", Inv)}px;--btn-ripple-y:{_rippleY.ToString("0.##", Inv)}px;";

    // ---- derived render state ---------------------------------------------------------------

    /// <summary>Virtual so <see cref="AtomIconButton"/>/<see cref="AtomToggleButton"/> can brand their
    /// own root while reusing every rule in this component's stylesheet.</summary>
    protected virtual string RootClass => "atom-button";

    /// <summary>Only emitted when true, so the common case carries no attribute.</summary>
    private string? IconOnlyAttr => IconOnly ? "true" : null;

    private string? FullWidthAttr => FullWidth ? "true" : null;

    /// <summary>Doubles as the <c>aria-pressed</c> value; null omits both attributes.</summary>
    private string? PressedAttr => Pressed is null ? null : Pressed.Value ? "true" : "false";

    // ---- interaction --------------------------------------------------------------------------

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        // A native disabled <button> never fires click, but a blocked link still can (and Loading is
        // not a native state at all), so the guard has to live here too.
        if (IsBlocked) return;

        if (Effect == ButtonEffect.ClickRipple)
        {
            // Offset* is relative to the target's padding box — exactly the ripple origin, and it comes
            // free with the event args rather than needing a JS measurement.
            _rippleX = e.OffsetX;
            _rippleY = e.OffsetY;
            RippleKey++;
        }

        await OnClick.InvokeAsync(e);
    }
}
