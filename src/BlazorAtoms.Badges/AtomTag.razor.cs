using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Badges;

/// <summary>
/// A display-oriented categorization label (a GitHub-style "label"): an optional leading icon, a
/// label and an optional trailing remove button, in a rounded rectangle. Painted by a color
/// <see cref="Variant"/> in a Solid / Soft / Outline <see cref="Appearance"/>. Not clickable or
/// selectable — reach for <see cref="AtomChip"/> when you need interaction. Pure CSS + SVG.
/// Common styling knobs (colors, size, height, font) come from <see cref="ChipFamilyBase"/>.
/// </summary>
public partial class AtomTag : ChipFamilyBase
{
    protected override string Prefix => "tag";

    /// <summary>Label content. Overrides <see cref="Text"/> when both are set.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Label text (used when <see cref="ChildContent"/> is null).</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Optional leading slot — an icon or status dot rendered before the label.</summary>
    [Parameter] public RenderFragment? Icon { get; set; }

    /// <summary>Color scheme (overridden by explicit color params).</summary>
    [Parameter] public Variant Variant { get; set; } = Variant.Default;

    /// <summary>Fill treatment: Solid (default) / Soft / Outline.</summary>
    [Parameter] public Appearance Appearance { get; set; } = Appearance.Solid;

    /// <summary>Show a trailing remove (×) button.</summary>
    [Parameter] public bool Removable { get; set; }

    /// <summary>Invoked when the remove (×) button is clicked.</summary>
    [Parameter] public EventCallback OnRemove { get; set; }

    /// <summary>Accessible label for the remove button. Default: "Remove".</summary>
    [Parameter] public string RemoveLabel { get; set; } = "Remove";

    /// <summary>Corner radius in px. Sets <c>--tag-radius</c>. Default is a small rounded rectangle.</summary>
    [Parameter] public double? Radius { get; set; }

    protected override StyleVars Vars(StyleVars s) => s.Add("radius", Radius);

    private RenderFragment Label => ChildContent ?? (builder => builder.AddContent(0, Text));

    private async Task HandleRemove() => await OnRemove.InvokeAsync();

    private string VariantValue => Variant switch
    {
        Variant.Info => "info",
        Variant.Success => "success",
        Variant.Warning => "warning",
        Variant.Danger => "danger",
        _ => "default",
    };

    private string AppearanceValue => Appearance switch
    {
        Appearance.Soft => "soft",
        Appearance.Outline => "outline",
        _ => "solid",
    };
}
