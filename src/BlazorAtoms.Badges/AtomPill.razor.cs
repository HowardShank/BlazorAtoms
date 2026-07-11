using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Badges;

/// <summary>
/// A status pill: a fully-rounded, soft-tinted label with a leading status dot (e.g. "Active",
/// "Pending", "Failed"). The simplest, display-only member of the chip/tag/pill family — the dot and
/// text share the color <see cref="Variant"/>. Supply an <see cref="Icon"/> to replace the dot, or
/// set <see cref="Dot"/> to false for a plain pill. Pure CSS. Common styling knobs (colors, size,
/// height, font) come from <see cref="ChipFamilyBase"/>.
/// </summary>
public partial class AtomPill : ChipFamilyBase
{
    protected override string Prefix => "pill";

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

    /// <summary>Status-dot color override. Sets <c>--pill-dot</c>. Null = accent (text color on solid).</summary>
    [Parameter] public string? DotColor { get; set; }

    protected override StyleVars Vars(StyleVars s) => s.Add("dot", DotColor);

    private RenderFragment Label => ChildContent ?? (builder => builder.AddContent(0, Text));

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
}
