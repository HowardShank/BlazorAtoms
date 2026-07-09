using System.Globalization;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Clocks;

/// <summary>
/// Two <see cref="AtomClock"/>s shown together — defaulting to the server zone and the browser-local
/// zone — arranged side-by-side or stacked (<see cref="Layout"/>). Shared <see cref="Format"/> /
/// <see cref="Live"/> / <see cref="Size"/> apply to both sides; each side has its own
/// <c>Kind</c> / <c>Label</c> / <c>TimeZone</c>.
/// </summary>
public partial class AtomClockPair : AtomComponentBase
{
    /// <summary>Side-by-side (default) or stacked.</summary>
    [Parameter] public ClockLayout Layout { get; set; } = ClockLayout.SideBySide;

    /// <summary>Left/top clock source (default <see cref="ClockKind.Server"/>).</summary>
    [Parameter] public ClockKind PrimaryKind { get; set; } = ClockKind.Server;

    /// <summary>Left/top caption (default "Server").</summary>
    [Parameter] public string? PrimaryLabel { get; set; } = "Server";

    /// <summary>Explicit timezone for the left/top clock (overrides <see cref="PrimaryKind"/>).</summary>
    [Parameter] public TimeZoneInfo? PrimaryTimeZone { get; set; }

    /// <summary>Right/bottom clock source (default <see cref="ClockKind.Browser"/>).</summary>
    [Parameter] public ClockKind SecondaryKind { get; set; } = ClockKind.Browser;

    /// <summary>Right/bottom caption (default "Local").</summary>
    [Parameter] public string? SecondaryLabel { get; set; } = "Local";

    /// <summary>Explicit timezone for the right/bottom clock (overrides <see cref="SecondaryKind"/>).</summary>
    [Parameter] public TimeZoneInfo? SecondaryTimeZone { get; set; }

    /// <summary>Format string shared by both clocks (default <c>"h:mm:ss tt"</c>).</summary>
    [Parameter] public string Format { get; set; } = "h:mm:ss tt";

    /// <summary>Tick both clocks once a second (default true).</summary>
    [Parameter] public bool Live { get; set; } = true;

    /// <summary>Size in px shared by both clocks. Sets each clock's <c>--clk-size</c>.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Gap between the two clocks in px. Sets <c>--clkp-gap</c>.</summary>
    [Parameter] public double? Gap { get; set; }

    private string LayoutValue => Layout == ClockLayout.Stacked ? "stacked" : "side-by-side";

    private string RootStyle =>
        Gap is null ? "" : $"--clkp-gap:{Gap.Value.ToString(CultureInfo.InvariantCulture)}px;";
}
