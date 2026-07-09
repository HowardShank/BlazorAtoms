using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Clocks;

/// <summary>
/// A live digital clock for a single time source. <see cref="ClockBase.Kind"/> picks the zone — the
/// server/host zone, UTC, or the auto-detected browser zone — or pass an explicit
/// <see cref="ClockBase.TimeZone"/> to override. Ticks once a second (set
/// <see cref="ClockBase.Live"/> false to freeze). Renders a semantic <c>&lt;time&gt;</c> element
/// formatted by <see cref="Format"/> / <see cref="Culture"/>.
/// </summary>
public partial class AtomClock : ClockBase
{
    /// <summary>.NET date/time format string for the displayed text (default <c>"h:mm:ss tt"</c>).</summary>
    [Parameter] public string Format { get; set; } = "h:mm:ss tt";

    /// <summary>Culture used to format the text. Null = <see cref="CultureInfo.CurrentCulture"/>.</summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>Optional caption rendered before the time (e.g. "Server", "Local").</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Clock size in px (drives font-size). Sets <c>--clk-size</c>.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Background override. Sets <c>--clk-bg</c>.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Text color override. Sets <c>--clk-color</c>.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Accessible label for the clock. Falls back to the surrounding text.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    private string FormatTime(DateTimeOffset t) => t.ToString(Format, EffectiveCulture);

    private static string IsoOf(DateTimeOffset t) =>
        t.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);

    private string RootStyle => string.Concat(
        Size is null ? "" : $"--clk-size:{N(Size.Value)}px;",
        Background is null ? "" : $"--clk-bg:{Background};",
        TextColor is null ? "" : $"--clk-color:{TextColor};");

    private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);
}
