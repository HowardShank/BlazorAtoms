using System.Globalization;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Badges;

/// <summary>
/// A status pill: a fully-rounded, soft-tinted label with a leading status dot (e.g. "Active",
/// "Pending", "Failed"). The simplest, display-only member of the chip/tag/pill family — the dot and
/// text share the color <see cref="Variant"/>. Supply an <see cref="Icon"/> to replace the dot, or
/// set <see cref="Dot"/> to false for a plain pill. Pure CSS.
/// </summary>
public partial class AtomPill : AtomComponentBase
{
    /// <summary>Label content. Overrides <see cref="Text"/> when both are set.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Label text (used when <see cref="ChildContent"/> is null).</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Optional leading slot — replaces the status dot when supplied.</summary>
    [Parameter] public RenderFragment? Icon { get; set; }

    /// <summary>Show the leading status dot (default true). Ignored when <see cref="Icon"/> is set.</summary>
    [Parameter] public bool Dot { get; set; } = true;

    /// <summary>Color scheme (overridden by explicit color params).</summary>
    [Parameter] public Variant Variant { get; set; } = Variant.Default;

    /// <summary>Fill treatment: Soft (default) / Solid / Outline.</summary>
    [Parameter] public Appearance Appearance { get; set; } = Appearance.Soft;

    /// <summary>Pill height in px (drives font-size and padding). Sets <c>--pill-size</c>.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Background / accent override. Sets <c>--pill-bg</c>. Null = variant default.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Text color override. Sets <c>--pill-color</c>. Null = variant default.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Border color override. Sets <c>--pill-border</c>. Null = variant/appearance default.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Accessible label for the pill. Falls back to the visible label text.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private RenderFragment Label => ChildContent ?? (builder => builder.AddContent(0, Text));

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
        Appearance.Solid => "solid",
        Appearance.Outline => "outline",
        _ => "soft",
    };

    private string RootStyle => string.Concat(
        Background is null ? "" : $"--pill-bg:{Background};",
        TextColor is null ? "" : $"--pill-color:{TextColor};",
        BorderColor is null ? "" : $"--pill-border:{BorderColor};",
        Size is null ? "" : $"--pill-size:{N(Size.Value)}px;");
}
