using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Equipment;

/// <summary>
/// A classic screw-base light bulb, drawn as pure inline SVG. Interactive: click or Space/Enter
/// toggles <see cref="IsOn"/> and raises <see cref="IsOnChanged"/>, so <c>@bind-IsOn</c> works
/// directly — unlike <see cref="AtomStoplight"/>, this one owns its own state rather than just
/// displaying a host-supplied one. The filament glows via a <c>currentColor</c> drop-shadow through
/// the glass, the same layered technique as the stoplight's lamps.
/// </summary>
public partial class AtomLightBulb : AtomComponentBase
{
    /// <summary>Whether the bulb is lit. Bind with <c>@bind-IsOn</c>.</summary>
    [Parameter] public bool IsOn { get; set; }

    /// <summary>Raised when a click or keyboard toggle changes <see cref="IsOn"/>. Backs
    /// <c>@bind-IsOn</c>.</summary>
    [Parameter] public EventCallback<bool> IsOnChanged { get; set; }

    /// <summary>Blocks all interaction and dims the bulb. Default false.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Rendered width in px; height follows from the fixed bulb aspect ratio. Maps to
    /// <c>--lightbulb-width</c>. Default 64.</summary>
    [Parameter] public double Width { get; set; } = 64;

    /// <summary>Glow/filament-lit color override (CSS color). Maps to <c>--lightbulb-glow</c>.</summary>
    [Parameter] public string? GlowColor { get; set; }

    /// <summary>Accessible name. Default generated from <see cref="IsOn"/>.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private bool Interactive => !Disabled;

    private string AriaLabelValue => AriaLabel ?? (IsOn ? "Light bulb, on" : "Light bulb, off");

    private string RootStyle => new StyleVars("lightbulb")
        .Add("width", Width)
        .Add("glow", GlowColor)
        .ToString();

    private async Task Toggle()
    {
        if (!Interactive) return;
        IsOn = !IsOn;
        await IsOnChanged.InvokeAsync(IsOn);
    }

    private async Task OnKey(KeyboardEventArgs e)
    {
        if (!Interactive) return;
        if (e.Key is " " or "Enter") await Toggle();
    }
}
