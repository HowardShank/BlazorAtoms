using System.Globalization;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Badges;

/// <summary>
/// A display-oriented categorization label (a GitHub-style "label"): an optional leading icon, a
/// label and an optional trailing remove button, in a rounded rectangle. Painted by a color
/// <see cref="Variant"/> in a Solid / Soft / Outline <see cref="Appearance"/>. Not clickable or
/// selectable — reach for <see cref="AtomChip"/> when you need interaction. Pure CSS + SVG.
/// </summary>
public partial class AtomTag : AtomComponentBase
{
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

    /// <summary>Tag height in px (drives font-size and padding). Sets <c>--tag-size</c>.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Corner radius in px. Sets <c>--tag-radius</c>. Default is a small rounded rectangle.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Background / accent override. Sets <c>--tag-bg</c>. Null = variant default.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Text color override. Sets <c>--tag-color</c>. Null = variant default.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Border color override. Sets <c>--tag-border</c>. Null = variant/appearance default.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Accessible label for the tag. Falls back to the visible label text.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private RenderFragment Label => ChildContent ?? (builder => builder.AddContent(0, Text));

    private async Task HandleRemove() => await OnRemove.InvokeAsync();

    private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);

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

    private string RootStyle => string.Concat(
        Background is null ? "" : $"--tag-bg:{Background};",
        TextColor is null ? "" : $"--tag-color:{TextColor};",
        BorderColor is null ? "" : $"--tag-border:{BorderColor};",
        Size is null ? "" : $"--tag-size:{N(Size.Value)}px;",
        Radius is null ? "" : $"--tag-radius:{N(Radius.Value)}px;");
}
