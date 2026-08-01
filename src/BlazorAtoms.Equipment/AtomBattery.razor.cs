using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Equipment;

/// <summary>
/// A battery charge indicator, drawn as pure inline SVG — an outline shell with a terminal nub and a
/// fill rect sized to <see cref="Level"/>, plus an optional badge (<see cref="Status"/>) for a
/// condition layered on top of the charge (plugged in, faulty, unrecognized). Presentational only:
/// a battery's own charge state is sensor data, not something a click should be able to change.
/// </summary>
public partial class AtomBattery : AtomComponentBase
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private const double Margin = 8;
    private const double BodyLen = 78;
    private const double BodyThick = 40;
    private const double NubLen = 10;
    private const double NubThick = 16;
    private const double Rx = 8;
    private const double NubRx = 3;
    private const double Pad = 6;
    private const double FillRx = 4;
    private const double BadgeR = 13;

    /// <summary>Charge fill level. Default <see cref="BatteryLevel.Full"/>.</summary>
    [Parameter] public BatteryLevel Level { get; set; } = BatteryLevel.Full;

    /// <summary>Condition badge drawn over the body, independent of <see cref="Level"/>. Default
    /// <see cref="BatteryStatus.None"/>.</summary>
    [Parameter] public BatteryStatus Status { get; set; } = BatteryStatus.None;

    /// <summary>Housing layout. Default <see cref="BatteryOrientation.Horizontal"/>.</summary>
    [Parameter] public BatteryOrientation Orientation { get; set; } = BatteryOrientation.Horizontal;

    /// <summary>Rendered width in px; height follows from the fixed housing aspect ratio. Maps to
    /// <c>--battery-width</c>. Default 64.</summary>
    [Parameter] public double Width { get; set; } = 64;

    /// <summary>Shell/nub/badge outline color override (CSS color). Maps to <c>--battery-outline</c>.</summary>
    [Parameter] public string? OutlineColor { get; set; }

    /// <summary>Fill color override (CSS color) — set this to fix the fill to one color regardless of
    /// <see cref="Level"/>; leave unset for the default red/amber/green-by-level. Maps to
    /// <c>--battery-fill</c>.</summary>
    [Parameter] public string? FillColor { get; set; }

    /// <summary>When true, ignores level/status colors and renders outline + fill in a single neutral
    /// color, like a flat monochrome icon.</summary>
    [Parameter] public bool Monochrome { get; set; }

    /// <summary>Accessible name. Default generated from <see cref="Level"/> and <see cref="Status"/>.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private bool IsVertical => Orientation == BatteryOrientation.Vertical;

    private double AlongTotal => Margin * 2 + BodyLen + NubLen;
    private double CrossTotal => Margin * 2 + BodyThick;

    private double ViewBoxWidth => IsVertical ? CrossTotal : AlongTotal;
    private double ViewBoxHeight => IsVertical ? AlongTotal : CrossTotal;

    private double BodyX => IsVertical ? Margin : Margin;
    private double BodyY => IsVertical ? Margin + NubLen : Margin;
    private double BodyW => IsVertical ? BodyThick : BodyLen;
    private double BodyH => IsVertical ? BodyLen : BodyThick;

    private double NubX => IsVertical ? Margin + (BodyThick - NubThick) / 2 : BodyX + BodyLen;
    private double NubY => IsVertical ? Margin : Margin + (BodyThick - NubThick) / 2;
    private double NubW => IsVertical ? NubThick : NubLen;
    private double NubH => IsVertical ? NubLen : NubThick;

    private double FillFraction => Level switch
    {
        BatteryLevel.Empty => 0.0,
        BatteryLevel.Quarter => 0.25,
        BatteryLevel.Half => 0.5,
        BatteryLevel.ThreeQuarter => 0.75,
        BatteryLevel.Full => 1.0,
        _ => 0.0,
    };

    // Fill grows from the end of the shell farthest from the nub, toward the nub — the same "gauge
    // filling toward the terminal" reading regardless of which edge that is per orientation.
    private double FillMaxLen => BodyLen - Pad * 2;
    private double FillLen => FillMaxLen * FillFraction;
    private double FillThick => BodyThick - Pad * 2;

    private double FillX => IsVertical ? BodyX + Pad : BodyX + Pad;
    private double FillY => IsVertical ? BodyY + BodyH - Pad - FillLen : BodyY + Pad;
    private double FillWidth => IsVertical ? FillThick : FillLen;
    private double FillHeight => IsVertical ? FillLen : FillThick;

    private double BadgeCx => BodyX + BodyW / 2;
    private double BadgeCy => BodyY + BodyH / 2;

    private bool HasBadge => Status != BatteryStatus.None;

    private static string LevelAttr(BatteryLevel level) => level switch
    {
        BatteryLevel.Empty => "empty",
        BatteryLevel.Quarter => "quarter",
        BatteryLevel.Half => "half",
        BatteryLevel.ThreeQuarter => "threequarter",
        BatteryLevel.Full => "full",
        _ => "empty",
    };

    private static string StatusAttr(BatteryStatus status) => status switch
    {
        BatteryStatus.Charging => "charging",
        BatteryStatus.Warning => "warning",
        BatteryStatus.Error => "error",
        BatteryStatus.Slash => "slash",
        BatteryStatus.Unknown => "unknown",
        BatteryStatus.Check => "check",
        _ => "none",
    };

    private string AriaLabelValue => AriaLabel ?? BuildDefaultLabel();

    private string BuildDefaultLabel()
    {
        var levelText = Level switch
        {
            BatteryLevel.Empty => "empty",
            BatteryLevel.Quarter => "quarter charge",
            BatteryLevel.Half => "half charge",
            BatteryLevel.ThreeQuarter => "three-quarter charge",
            BatteryLevel.Full => "full charge",
            _ => "unknown charge",
        };

        var statusText = Status switch
        {
            BatteryStatus.Charging => "charging",
            BatteryStatus.Warning => "warning",
            BatteryStatus.Error => "error",
            BatteryStatus.Slash => "disconnected",
            BatteryStatus.Unknown => "unknown status",
            BatteryStatus.Check => "ok",
            _ => null,
        };

        return statusText is null ? $"Battery, {levelText}" : $"Battery, {levelText}, {statusText}";
    }

    private string RootStyle => new StyleVars("battery")
        .Add("width", Width)
        .Add("outline", OutlineColor)
        .Add("fill", FillColor)
        .ToString();

    private static string Fmt(double v) => v.ToString("F2", Inv);

    /// <summary>Diagonal cut line spanning the whole shell (body + nub), used only for
    /// <see cref="BatteryStatus.Slash"/> — a full-icon slash reads as "disconnected", unlike the other
    /// badges which sit inside the body as a small glyph.</summary>
    private string SlashLine => IsVertical
        ? string.Create(Inv, $"M {Fmt(BodyX - 3)} {Fmt(NubY - 3)} L {Fmt(BodyX + BodyW + 3)} {Fmt(BodyY + BodyH + 3)}")
        : string.Create(Inv, $"M {Fmt(BodyX - 3)} {Fmt(BodyY - 3)} L {Fmt(NubX + NubW + 3)} {Fmt(BodyY + BodyH + 3)}");

    /// <summary>Lightning-bolt path for <see cref="BatteryStatus.Charging"/>, centered on
    /// (<paramref name="cx"/>, <paramref name="cy"/>).</summary>
    private static string BoltPath(double cx, double cy) => string.Create(Inv,
        $"M {Fmt(cx + 2)} {Fmt(cy - 11)} L {Fmt(cx - 6)} {Fmt(cy + 1)} L {Fmt(cx)} {Fmt(cy + 1)} " +
        $"L {Fmt(cx - 2)} {Fmt(cy + 11)} L {Fmt(cx + 6)} {Fmt(cy - 1)} L {Fmt(cx)} {Fmt(cy - 1)} Z");

    /// <summary>Checkmark path for <see cref="BatteryStatus.Check"/>.</summary>
    private static string CheckPath(double cx, double cy) => string.Create(Inv,
        $"M {Fmt(cx - 6)} {Fmt(cy)} L {Fmt(cx - 1)} {Fmt(cy + 5)} L {Fmt(cx + 7)} {Fmt(cy - 7)}");

    /// <summary>Renders a single centered "?" mark for <see cref="BatteryStatus.Unknown"/> — an SVG
    /// <c>&lt;text&gt;</c> has to be built via <see cref="RenderTreeBuilder"/> rather than written as
    /// markup, since Razor reserves <c>&lt;text&gt;</c> as a control construct and rejects attributes
    /// on it (RZ1023).</summary>
    private RenderFragment QuestionMark(double cx, double cy) => builder =>
    {
        builder.OpenElement(0, "text");
        builder.AddAttribute(1, "class", "atom-battery-badge-glyph atom-battery-badge-text");
        builder.AddAttribute(2, "x", Fmt(cx));
        builder.AddAttribute(3, "y", Fmt(cy));
        builder.AddAttribute(4, "text-anchor", "middle");
        builder.AddAttribute(5, "dominant-baseline", "central");
        builder.AddContent(6, "?");
        builder.CloseElement();
    };
}
