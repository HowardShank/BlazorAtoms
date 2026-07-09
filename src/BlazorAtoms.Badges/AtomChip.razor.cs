using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Badges;

/// <summary>
/// An interactive chip: a compact element with an optional leading icon/avatar, a label and an
/// optional trailing remove button. When <see cref="OnClick"/> has a handler the chip becomes a
/// keyboard-operable button (Enter/Space, <c>role="button"</c>, <c>aria-pressed</c>) — for filter,
/// choice and selectable chips. Set <see cref="Removable"/> for a dismiss (×) affordance. Painted by
/// a color <see cref="Variant"/> in a Solid / Soft / Outline <see cref="Appearance"/>. Pure CSS + SVG.
/// </summary>
public partial class AtomChip : AtomComponentBase
{
    /// <summary>Label content. Overrides <see cref="Text"/> when both are set.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Label text (used when <see cref="ChildContent"/> is null).</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Optional leading slot — an icon, avatar or status dot rendered before the label.</summary>
    [Parameter] public RenderFragment? Icon { get; set; }

    /// <summary>Color scheme (overridden by explicit color params).</summary>
    [Parameter] public Variant Variant { get; set; } = Variant.Default;

    /// <summary>Fill treatment: Solid / Soft (default) / Outline.</summary>
    [Parameter] public Appearance Appearance { get; set; } = Appearance.Soft;

    /// <summary>Selected/active state. Renders the accent emphasis and sets <c>aria-pressed</c>.</summary>
    [Parameter] public bool Selected { get; set; }

    /// <summary>Disabled state — dims the chip and blocks click/keyboard/remove.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Invoked when the chip is activated (click or Enter/Space). Having a handler makes the
    /// chip a keyboard-operable button; without one the chip is a static label.</summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>Show a trailing remove (×) button.</summary>
    [Parameter] public bool Removable { get; set; }

    /// <summary>Invoked when the remove (×) button is clicked. Click does not bubble to <see cref="OnClick"/>.</summary>
    [Parameter] public EventCallback OnRemove { get; set; }

    /// <summary>Accessible label for the remove button. Default: "Remove".</summary>
    [Parameter] public string RemoveLabel { get; set; } = "Remove";

    /// <summary>Chip height in px (drives font-size and padding). Sets <c>--chip-size</c>.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Corner radius in px. Sets <c>--chip-radius</c>. Default is a fully-rounded stadium.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <summary>Background / accent override. Sets <c>--chip-bg</c>. Null = variant default.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Text color override. Sets <c>--chip-color</c>. Null = variant default.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Border color override. Sets <c>--chip-border</c>. Null = variant/appearance default.</summary>
    [Parameter] public string? BorderColor { get; set; }

    /// <summary>Accessible label for the chip itself. Falls back to the visible label text.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private bool Clickable => OnClick.HasDelegate;

    private RenderFragment Label => ChildContent ?? (builder => builder.AddContent(0, Text));

    private async Task HandleClick()
    {
        if (Disabled || !Clickable) return;
        await OnClick.InvokeAsync();
    }

    private async Task HandleKey(KeyboardEventArgs e)
    {
        if (Disabled || !Clickable) return;
        if (e.Key is "Enter" or " " or "Spacebar")
            await OnClick.InvokeAsync();
    }

    private async Task HandleRemove()
    {
        if (Disabled) return;
        await OnRemove.InvokeAsync();
    }

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
        Background is null ? "" : $"--chip-bg:{Background};",
        TextColor is null ? "" : $"--chip-color:{TextColor};",
        BorderColor is null ? "" : $"--chip-border:{BorderColor};",
        Size is null ? "" : $"--chip-size:{N(Size.Value)}px;",
        Radius is null ? "" : $"--chip-radius:{N(Radius.Value)}px;");
}
