using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Inputs;

/// <summary>
/// On/off toggle switch. Structurally the same trick as <see cref="AtomCheckbox"/> — a native
/// <c>&lt;input type="checkbox" role="switch"&gt;</c> for semantics with painted track/thumb spans on
/// top — so it keeps free keyboard and form support with no JS. Reads as a switch to assistive tech
/// via <c>role="switch"</c>, which is the only difference in the accessibility contract.
/// </summary>
public partial class AtomSwitch : AtomInputBase<bool>
{
    /// <summary>Caption rendered beside the switch, inside the same <c>&lt;label&gt;</c> — clicking it
    /// toggles. Independent of the column-layout <see cref="AtomInputBase{TValue}.Label"/>.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Short text shown inside the track while on (e.g. <c>ON</c>, <c>1</c>). Together with
    /// <see cref="OffText"/>; leave both null for a plain track.</summary>
    [Parameter] public string? OnText { get; set; }

    /// <summary>Short text shown inside the track while off.</summary>
    [Parameter] public string? OffText { get; set; }

    /// <summary>Content drawn inside the thumb — an icon, a glyph. Rendered in its own
    /// <c>.atom-switch-thumb</c> slot and inherits the thumb's color.</summary>
    [Parameter] public RenderFragment? ThumbContent { get; set; }

    /// <summary>Track width in px → <c>--field-track-width</c>. Defaults to the
    /// <see cref="AtomInputBase{TValue}.Size"/> preset.</summary>
    [Parameter] public double? TrackWidth { get; set; }

    /// <summary>Track height in px → <c>--field-track-height</c>. The thumb is sized and travels
    /// from this, so a taller track gives a bigger thumb automatically.</summary>
    [Parameter] public double? TrackHeight { get; set; }

    /// <summary>Which side of the switch <see cref="Text"/> sits on. Default
    /// <see cref="LabelPlacement.End"/>.</summary>
    [Parameter] public LabelPlacement TextPlacement { get; set; } = LabelPlacement.End;

    // ---- derived render state ---------------------------------------------------------------

    // SupportsNativeReadOnly stays false: `readonly` is ignored on a checkbox, so ReadOnly renders
    // as the native `disabled` attribute.

    protected override string DefaultAriaLabel => "Switch";

    private string RootClass => "atom-switch";

    private string? RootStyle => BuildRootStyle(new StyleVars("field")
        .Add("track-width", TrackWidth)
        .Add("track-height", TrackHeight)
        .ToString());

    private string PlacementAttr => Kebab(TextPlacement.ToString());

    // ---- interaction --------------------------------------------------------------------------

    private Task OnChangeEvent(ChangeEventArgs e) =>
        e.Value is bool b ? SetValueAsync(b) : Task.CompletedTask;
}
