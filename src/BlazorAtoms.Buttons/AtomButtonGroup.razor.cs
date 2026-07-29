using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Buttons;

/// <summary>
/// Lays out several buttons as one control — a seamed row/column by default — and cascades the shared
/// styling axes so they're set once for the set instead of repeated per button.
/// </summary>
/// <remarks>
/// <para>Layout and cascade only: the group holds no selected value. A segmented single-select would
/// mean a generic <c>AtomButtonGroup&lt;TValue&gt;</c> with selection semantics overlapping
/// <c>BlazorAtoms.Inputs.AtomRadioGroup</c>; use <see cref="AtomToggleButton"/>'s own
/// <c>@bind-Value</c> per button instead.</para>
/// <para>Not a <see cref="ButtonFamilyBase"/>: it has no click, no loading state, and no href, so it
/// would inherit a surface it can't honor.</para>
/// </remarks>
public partial class AtomButtonGroup : AtomComponentBase
{
    /// <summary>The buttons. Any <see cref="ButtonFamilyBase"/>-derived component picks up the axes
    /// below; plain markup is laid out but not restyled.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Color scheme cascaded to children that don't set their own.</summary>
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Default;

    /// <summary>Fill treatment cascaded to children that don't set their own.</summary>
    [Parameter] public ButtonAppearance Appearance { get; set; } = ButtonAppearance.Solid;

    /// <summary>Density preset cascaded to children that don't set their own.</summary>
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Medium;

    /// <summary>Corner treatment cascaded to children that don't set their own. The group flattens the
    /// inner corners itself, so this only shapes the two outer ends.</summary>
    [Parameter] public ButtonShape Shape { get; set; } = ButtonShape.Rounded;

    /// <summary>Layout axis. Default <see cref="ButtonGroupOrientation.Horizontal"/>.</summary>
    [Parameter] public ButtonGroupOrientation Orientation { get; set; } = ButtonGroupOrientation.Horizontal;

    /// <summary>When true (default), the buttons touch: inner radii are flattened and the doubled
    /// border between neighbours is pulled into a single seam. False spaces them by
    /// <see cref="Gap"/>, keeping every button's own shape.</summary>
    [Parameter] public bool Attached { get; set; } = true;

    /// <summary>Space between buttons in px when <see cref="Attached"/> is false →
    /// <c>--btn-group-gap</c>.</summary>
    [Parameter] public double? Gap { get; set; }

    /// <summary>Stretches the group across its container and divides the width evenly between the
    /// buttons.</summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>When false, hidden via CSS <c>display:none</c> (stays in the DOM). Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>Accessible name for the set — what <c>role="group"</c> announces (e.g. "Text
    /// alignment").</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // ---- derived render state ---------------------------------------------------------------

    /// <summary>Rebuilt each render, so a changed axis flows to the children. <c>IsFixed</c> is
    /// deliberately false on the cascade for the same reason.</summary>
    private ButtonGroupContext Context => new()
    {
        Variant = Variant,
        Appearance = Appearance,
        Size = Size,
        Shape = Shape,
    };

    private string OrientationAttr => ButtonFamilyBase.Kebab(Orientation.ToString());

    private string AttachedAttr => Attached ? "true" : "false";

    private string? FullWidthAttr => FullWidth ? "true" : null;

    private string? RootStyle
    {
        get
        {
            var vars = new StyleVars("btn-group").Add("gap", Gap).ToString();
            var s = (Visible ? "" : "display:none;") + vars;
            return string.IsNullOrEmpty(s) ? null : s;
        }
    }
}
