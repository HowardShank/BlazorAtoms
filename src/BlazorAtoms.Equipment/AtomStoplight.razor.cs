using System.Globalization;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Equipment;

/// <summary>
/// A traditional red/yellow/green traffic signal, drawn as pure inline SVG. Each lamp is a rim +
/// glass + sheen stack under a protective visor; the active lamp (picked by <see cref="State"/>)
/// switches from a dim <c>color-mix</c> tint to its full hue and grows a <c>currentColor</c> glow —
/// no gradients, so there are no <c>&lt;defs&gt;</c> ids to keep unique across instances on one page.
/// </summary>
public partial class AtomStoplight : AtomComponentBase
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private const double LampRadius = 36;
    private const double MarginCross = 24;
    private const double MarginAlong = 28;
    private const double Spacing = 100;

    private static readonly string[] HueNames = ["red", "yellow", "green"];
    private static readonly StoplightState[] LampOrder =
        [StoplightState.Red, StoplightState.Yellow, StoplightState.Green];

    /// <summary>Which lamp is lit. Default <see cref="StoplightState.Red"/>.</summary>
    [Parameter] public StoplightState State { get; set; } = StoplightState.Red;

    /// <summary>Lamp stack direction. Default <see cref="StoplightOrientation.Vertical"/>.</summary>
    [Parameter] public StoplightOrientation Orientation { get; set; } = StoplightOrientation.Vertical;

    /// <summary>Rendered width in px; height follows from the fixed housing aspect ratio. Maps to
    /// <c>--stoplight-width</c>. Default 96.</summary>
    [Parameter] public double Width { get; set; } = 96;

    /// <summary>Lit-red color override (CSS color). Maps to <c>--stoplight-red</c>.</summary>
    [Parameter] public string? RedColor { get; set; }

    /// <summary>Lit-yellow color override (CSS color). Maps to <c>--stoplight-yellow</c>.</summary>
    [Parameter] public string? YellowColor { get; set; }

    /// <summary>Lit-green color override (CSS color). Maps to <c>--stoplight-green</c>.</summary>
    [Parameter] public string? GreenColor { get; set; }

    /// <summary>Accessible name. Default generated from <see cref="State"/>.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private bool IsVertical => Orientation == StoplightOrientation.Vertical;

    // Cross-axis = the housing's short dimension (fixed, 3 lamps wide regardless of stack length).
    private static double CrossSize => 2 * MarginCross + 2 * LampRadius;

    // Along-axis = the stack direction, sized for exactly 3 lamps at fixed spacing.
    private static double AlongSize => 2 * MarginAlong + 2 * LampRadius + 2 * Spacing;

    private double ViewBoxWidth => IsVertical ? CrossSize : AlongSize;
    private double ViewBoxHeight => IsVertical ? AlongSize : CrossSize;

    private static double LampCenter(int index) => MarginAlong + LampRadius + index * Spacing;

    private double LampCx(int index) => IsVertical ? CrossSize / 2 : LampCenter(index);
    private double LampCy(int index) => IsVertical ? LampCenter(index) : CrossSize / 2;

    private bool IsActive(int index) => State == LampOrder[index];

    private string AriaLabelValue => AriaLabel ?? $"Stoplight showing {State}";

    private string RootStyle => new StyleVars("stoplight")
        .Add("width", Width)
        .Add("red", RedColor)
        .Add("yellow", YellowColor)
        .Add("green", GreenColor)
        .ToString();

    private static string Fmt(double v) => v.ToString("F2", Inv);

    /// <summary>Brim path over the top ~140° of the lamp at (<paramref name="cx"/>,
    /// <paramref name="cy"/>) — an annulus segment from <see cref="LampRadius"/> + 2 out to + 12,
    /// spanning 200°..340° (flanking straight up at 270°) so it reads as a small awning rather than a
    /// full ring.</summary>
    private static string VisorPath(double cx, double cy, double r)
    {
        var outerR = r + 12;
        var innerR = r + 2;
        var a1 = DegToRad(200);
        var a2 = DegToRad(340);

        var (ox1, oy1) = PointOn(cx, cy, outerR, a1);
        var (ox2, oy2) = PointOn(cx, cy, outerR, a2);
        var (ix2, iy2) = PointOn(cx, cy, innerR, a2);
        var (ix1, iy1) = PointOn(cx, cy, innerR, a1);

        return string.Create(Inv, $"M {ox1:F2} {oy1:F2} A {outerR:F2} {outerR:F2} 0 0 1 {ox2:F2} {oy2:F2} " +
                                   $"L {ix2:F2} {iy2:F2} A {innerR:F2} {innerR:F2} 0 0 0 {ix1:F2} {iy1:F2} Z");
    }

    private static double DegToRad(double deg) => deg * Math.PI / 180.0;

    private static (double X, double Y) PointOn(double cx, double cy, double r, double angleRad) =>
        (cx + r * Math.Cos(angleRad), cy + r * Math.Sin(angleRad));
}
