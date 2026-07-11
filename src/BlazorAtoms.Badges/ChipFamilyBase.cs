using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Badges;

/// <summary>
/// Shared styling surface for the chip/tag/pill family (<see cref="AtomChip"/> / <see cref="AtomTag"/> /
/// <see cref="AtomPill"/>). Carries the common typed knobs — colors, size, height and the font-styling
/// set — and builds the root <c>style</c> string of <c>--{Prefix}-*</c> custom properties via
/// <see cref="StyleVars"/>. Each component supplies its <see cref="Prefix"/> and, through the
/// <see cref="Vars"/> hook, its own extra tokens (Chip/Tag radius, Pill dot color).
/// </summary>
public abstract class ChipFamilyBase : AtomComponentBase
{
    /// <summary>CSS custom-property prefix, e.g. <c>"chip"</c> → <c>--chip-bg</c>.</summary>
    protected abstract string Prefix { get; }

    /// <summary>Background / accent override. Sets <c>--{Prefix}-bg</c>. Null = variant default.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Text color override. Sets <c>--{Prefix}-color</c>. Null = variant default.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Border color override. Sets <c>--{Prefix}-border</c>. Null = variant/appearance default.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Height in px (drives font-size and padding). Sets <c>--{Prefix}-size</c>.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Box height in px, independent of <see cref="Size"/> (which keeps driving font/padding).
    /// Sets <c>--{Prefix}-height</c>. Null = follows <see cref="Size"/>.</summary>
    [Parameter] public double? Height { get; set; }

    /// <summary>Font family override. Sets <c>--{Prefix}-font-family</c>. Null = inherit.</summary>
    [Parameter] public string? FontFamily { get; set; }

    /// <summary>Font size in px, overriding the <see cref="Size"/>-derived size. Sets <c>--{Prefix}-font-size</c>.</summary>
    [Parameter] public double? FontSize { get; set; }

    /// <summary>Font weight (e.g. "600" or "bold"). Sets <c>--{Prefix}-font-weight</c>. Null = default.</summary>
    [Parameter] public string? FontWeight { get; set; }

    /// <summary>Font style (e.g. "italic"). Sets <c>--{Prefix}-font-style</c>. Null = normal.</summary>
    [Parameter] public string? FontStyle { get; set; }

    /// <summary>Letter spacing (e.g. ".05em"). Sets <c>--{Prefix}-letter-spacing</c>. Null = normal.</summary>
    [Parameter] public string? LetterSpacing { get; set; }

    /// <summary>Text transform (e.g. "uppercase"). Sets <c>--{Prefix}-text-transform</c>. Null = none.</summary>
    [Parameter] public string? TextTransform { get; set; }

    /// <summary>Accessible label. Falls back to the visible label text.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Hook for a subclass to add its component-unique tokens to the style builder.</summary>
    protected virtual StyleVars Vars(StyleVars s) => s;

    /// <summary>Root inline style — the common tokens plus subclass extras (via <see cref="Vars"/>).</summary>
    protected string RootStyle => Vars(new StyleVars(Prefix)
        .Add("bg", Background).Add("color", TextColor).Add("border", BorderColor)
        .Add("size", Size).Add("height", Height)
        .Add("font-family", FontFamily).Add("font-size", FontSize).Add("font-weight", FontWeight)
        .Add("font-style", FontStyle).Add("letter-spacing", LetterSpacing).Add("text-transform", TextTransform))
        .ToString();
}
