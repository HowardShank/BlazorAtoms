using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Buttons;

/// <summary>
/// A primary action seamed to an arrow that drops a menu — "Save" with a "Save as…" list beside it.
/// The action half is an <see cref="AtomButton"/>; the menu half is a native
/// <c>&lt;details&gt;/&lt;summary&gt;</c>, so open/close state, keyboard activation, and the
/// expanded/collapsed announcement all come from the platform with no JS and no C# state.
/// </summary>
/// <remarks>
/// <para>Two consequences of the JS-free choice, both deliberate:</para>
/// <list type="bullet">
/// <item><description><b>No collision flipping.</b> The panel always drops below, aligned per
/// <see cref="MenuAlign"/>. Choosing a side automatically means measuring the viewport, which needs
/// JS — that's <c>BlazorAtoms.Overlays.AtomDropdown</c> (Tier C, planned), which this component can
/// compose once it exists.</description></item>
/// <item><description><b>No click-outside close.</b> <c>&lt;details&gt;</c> closes on its own summary,
/// on <c>Esc</c> in browsers that support it, and on selecting an item if the caller's handler
/// navigates — but a click elsewhere on the page leaves it open, because detecting that needs a
/// document-level listener.</description></item>
/// </list>
/// </remarks>
public partial class AtomSplitButton : ButtonFamilyBase
{
    /// <summary>Primary label. Ignored when <see cref="ChildContent"/> is set.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Primary content. Wins over <see cref="Text"/>.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Optional glyph before the primary label.</summary>
    [Parameter] public RenderFragment? Icon { get; set; }

    /// <summary>The dropped panel's contents — typically a list of actions. Rendered inside a
    /// <c>role="menu"</c> container; supply the items' own roles/handlers.</summary>
    [Parameter] public RenderFragment? MenuContent { get; set; }

    /// <summary>Which edge the panel lines up with. Default
    /// <see cref="SplitMenuAlign.Start"/>.</summary>
    [Parameter] public SplitMenuAlign MenuAlign { get; set; } = SplitMenuAlign.Start;

    /// <summary>Panel width in px → <c>--btn-menu-width</c>. Null sizes it to its content, with a
    /// minimum matching the button.</summary>
    [Parameter] public double? MenuWidth { get; set; }

    /// <summary>Accessible name for the arrow half, which has no text of its own. Default
    /// <c>"More actions"</c>.</summary>
    [Parameter] public string ToggleAriaLabel { get; set; } = "More actions";

    // ---- derived render state ---------------------------------------------------------------

    private string MenuAlignAttr => Kebab(MenuAlign.ToString());

    /// <summary>The panel width rides in through the base's <c>Vars</c> hook, so it lands in the same
    /// style attribute as the shared tokens.</summary>
    protected override StyleVars Vars(StyleVars s) => s.Add("menu-width", MenuWidth);
}
