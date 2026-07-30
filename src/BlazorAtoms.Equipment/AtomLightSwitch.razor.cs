using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Equipment;

/// <summary>
/// A traditional wall toggle switch, drawn as pure inline SVG. Interactive: click or Space/Enter
/// toggles <see cref="IsOn"/> and raises <see cref="IsOnChanged"/>, so <c>@bind-IsOn</c> works
/// directly. Deliberately distinct from <c>BlazorAtoms.Inputs</c>' <c>AtomSwitch</c> — that is a
/// generic pill-shaped form toggle; this is a realistic physical wall-switch object, part of this
/// library's "a real thing with a state" family alongside <see cref="AtomStoplight"/> and
/// <see cref="AtomLightBulb"/>.
/// </summary>
public partial class AtomLightSwitch : AtomComponentBase
{
    /// <summary>Whether the switch is flipped on. Bind with <c>@bind-IsOn</c>.</summary>
    [Parameter] public bool IsOn { get; set; }

    /// <summary>Raised when a click or keyboard toggle changes <see cref="IsOn"/>. Backs
    /// <c>@bind-IsOn</c>.</summary>
    [Parameter] public EventCallback<bool> IsOnChanged { get; set; }

    /// <summary>Blocks all interaction and dims the switch. Default false.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Rendered width in px; height follows from the fixed plate aspect ratio. Maps to
    /// <c>--lightswitch-width</c>. Default 48.</summary>
    [Parameter] public double Width { get; set; } = 48;

    /// <summary>Accessible name. Default generated from <see cref="IsOn"/>.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private bool Interactive => !Disabled;

    private string AriaLabelValue => AriaLabel ?? (IsOn ? "Light switch, on" : "Light switch, off");

    private string RootStyle => new StyleVars("lightswitch").Add("width", Width).ToString();

    // Real toggle switches pivot at a fixed point near the housing's bottom, same technique
    // AtomAnalogClock uses for its hands: an SVG rotate(angle cx cy) transform, not a CSS one, so it
    // needs no separate transform-origin support.
    private string LeverTransform => IsOn ? "rotate(-18 40 82)" : "rotate(18 40 82)";

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
