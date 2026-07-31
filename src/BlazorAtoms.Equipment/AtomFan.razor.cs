using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Equipment;

/// <summary>
/// A spinning fan (desk or ceiling), drawn as pure inline SVG plus an HTML speed-readout label
/// (SVG can't hold a Razor-authored <c>&lt;text&gt;</c> — RZ1023 — so the label lives outside the
/// <c>&lt;svg&gt;</c>, not inside it). Interactive: click or Space/Enter cycles
/// <see cref="FanSpeed.Off"/> → <see cref="FanSpeed.Low"/> → <see cref="FanSpeed.Medium"/> →
/// <see cref="FanSpeed.High"/> → back to <see cref="FanSpeed.Off"/> and raises
/// <see cref="SpeedChanged"/>, so <c>@bind-Speed</c> works directly. <see cref="Direction"/> is
/// one-way — real ceiling fans reverse via a separate switch, not the same control that cycles speed.
/// </summary>
public partial class AtomFan : AtomComponentBase
{
    /// <summary>Current speed. Bind with <c>@bind-Speed</c>.</summary>
    [Parameter] public FanSpeed Speed { get; set; } = FanSpeed.Off;

    /// <summary>Raised when a click or keyboard cycle changes <see cref="Speed"/>. Backs
    /// <c>@bind-Speed</c>.</summary>
    [Parameter] public EventCallback<FanSpeed> SpeedChanged { get; set; }

    /// <summary>Blade rotation direction. Default <see cref="FanDirection.Forward"/>.</summary>
    [Parameter] public FanDirection Direction { get; set; } = FanDirection.Forward;

    /// <summary>Housing artwork. Default <see cref="FanStyle.Desk"/>.</summary>
    [Parameter] public FanStyle Kind { get; set; } = FanStyle.Desk;

    /// <summary>Blocks all interaction and dims the fan. Default false.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Rendered width in px; height follows from the fixed aspect ratio. Maps to
    /// <c>--fan-width</c>. Default 96.</summary>
    [Parameter] public double Width { get; set; } = 96;

    /// <summary>Blade color override (CSS color). Maps to <c>--fan-blade</c>.</summary>
    [Parameter] public string? BladeColor { get; set; }

    /// <summary>Hub/grille/base color override (CSS color). Maps to <c>--fan-housing</c>.</summary>
    [Parameter] public string? HousingColor { get; set; }

    /// <summary>Direction-arrow and speed-label color override (CSS color). Maps to
    /// <c>--fan-accent</c>.</summary>
    [Parameter] public string? AccentColor { get; set; }

    /// <summary>Show the small curved direction arrow near the hub. Default true.</summary>
    [Parameter] public bool ShowDirectionIndicator { get; set; } = true;

    /// <summary>Show the OFF/LOW/MED/HIGH text readout below the fan. Default true.</summary>
    [Parameter] public bool ShowSpeedLabel { get; set; } = true;

    /// <summary>Accessible name. Default generated from <see cref="Speed"/>.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private bool Interactive => !Disabled;

    private bool IsCeiling => Kind == FanStyle.Ceiling;

    private string SpeedAttr => Speed switch
    {
        FanSpeed.Off => "off",
        FanSpeed.Low => "low",
        FanSpeed.Medium => "medium",
        FanSpeed.High => "high",
        _ => "off",
    };

    private string SpeedLabelText => Speed switch
    {
        FanSpeed.Off => "OFF",
        FanSpeed.Low => "LOW",
        FanSpeed.Medium => "MED",
        FanSpeed.High => "HIGH",
        _ => "OFF",
    };

    // Higher speeds spin faster, i.e. a shorter animation-duration. Off never reaches this (no
    // data-spinning attribute at all when off, so no animation runs regardless of the value).
    private string SpinDuration => Speed switch
    {
        FanSpeed.Low => "2.4s",
        FanSpeed.Medium => "1.2s",
        FanSpeed.High => "0.5s",
        _ => "1.2s",
    };

    private string AriaLabelValue => AriaLabel ?? $"Fan, speed {Speed}";

    private string RootStyle => new StyleVars("fan")
        .Add("width", Width)
        .Add("blade", BladeColor)
        .Add("housing", HousingColor)
        .Add("accent", AccentColor)
        .Add("duration", SpinDuration)
        .ToString();

    private async Task Toggle()
    {
        if (!Interactive) return;
        var next = (FanSpeed)(((int)Speed + 1) % 4);
        Speed = next;
        await SpeedChanged.InvokeAsync(next);
    }

    private async Task OnKey(KeyboardEventArgs e)
    {
        if (!Interactive) return;
        if (e.Key is " " or "Enter") await Toggle();
    }
}
