using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Clocks;

/// <summary>
/// A live analog clock face for a single time source. Shares the time-source, tick, and
/// browser-timezone plumbing with <see cref="AtomClock"/> via <see cref="ClockBase"/> — set
/// <see cref="ClockBase.Kind"/> (server / UTC / browser) or an explicit
/// <see cref="ClockBase.TimeZone"/>, and it ticks once a second unless <see cref="ClockBase.Live"/>
/// is false. Renders a scalable SVG dial with hour / minute / (optional) second hands; works the
/// same under interactive server and WebAssembly render modes.
/// </summary>
public partial class AtomAnalogClock : ClockBase
{
    /// <summary>Face diameter in px (default 160). Sets <c>--aclk-size</c>.</summary>
    [Parameter] public double Size { get; set; } = 160;

    /// <summary>Draw the sweeping second hand (default true).</summary>
    [Parameter] public bool ShowSeconds { get; set; } = true;

    /// <summary>Draw the 60 minute tick marks (default true). Hour ticks always show.</summary>
    [Parameter] public bool ShowMinuteTicks { get; set; } = true;

    /// <summary>Draw 1–12 hour numerals (default false).</summary>
    [Parameter] public bool ShowNumerals { get; set; }

    /// <summary>Optional caption rendered under the dial.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Face fill override. Sets <c>--aclk-face</c>.</summary>
    [Parameter] public string? FaceColor { get; set; }

    /// <summary>Ring + tick + hour/minute hand color override. Sets <c>--aclk-hand</c>.</summary>
    [Parameter] public string? HandColor { get; set; }

    /// <summary>Second-hand accent color override. Sets <c>--aclk-accent</c>.</summary>
    [Parameter] public string? AccentColor { get; set; }

    // Snapshot the time once per render (assigned at the top of the .razor markup) so all three
    // hands agree on the same instant — ClockBase drives ticks through StateHasChanged, so the
    // snapshot has to refresh on every render, not just OnParametersSet.
    private DateTimeOffset _now;

    private double SecondsOfMinute => _now.Second;
    private double MinutesOfHour => _now.Minute + SecondsOfMinute / 60.0;
    private double HoursOfDay => (_now.Hour % 12) + MinutesOfHour / 60.0;

    private double HourAngle => HoursOfDay * 30.0;    // 360 / 12
    private double MinuteAngle => MinutesOfHour * 6.0; // 360 / 60
    private double SecondAngle => SecondsOfMinute * 6.0;

    private string EffectiveAriaLabel =>
        $"{(Label is null ? "" : Label + " ")}{_now.ToString("t", CultureInfo.CurrentCulture)}".Trim();

    private string RootStyle => string.Concat(
        $"--aclk-size:{N(Size)}px;",
        FaceColor is null ? "" : $"--aclk-face:{FaceColor};",
        HandColor is null ? "" : $"--aclk-hand:{HandColor};",
        AccentColor is null ? "" : $"--aclk-accent:{AccentColor};");

    private static string Rotate(double angle) =>
        $"rotate({N(angle)} 50 50)";

    // 60 ticks: every 5th is a longer/heavier hour tick.
    private static readonly int[] Minutes = Enumerable.Range(0, 60).ToArray();

    private static bool IsHourTick(int minute) => minute % 5 == 0;

    // Razor reserves the <text> tag, so hour numerals can't be emitted as normal SVG markup.
    // Build them as a raw string instead; scoped CSS can't reach injected markup, so the styling
    // rides on inline presentation attributes (fill uses the same --aclk-hand var as the hands).
    private MarkupString NumeralsMarkup
    {
        get
        {
            var sb = new StringBuilder();
            for (var h = 1; h <= 12; h++)
            {
                var rad = h * 30.0 * Math.PI / 180.0;
                var x = 50 + 37 * Math.Sin(rad);
                var y = 50 - 37 * Math.Cos(rad);
                sb.Append($"<text x=\"{Fmt(x)}\" y=\"{Fmt(y)}\" text-anchor=\"middle\" ")
                  .Append("dominant-baseline=\"central\" ")
                  .Append("style=\"fill:var(--aclk-hand,currentColor);font-size:7px;font-weight:600\">")
                  .Append(h.ToString(CultureInfo.InvariantCulture))
                  .Append("</text>");
            }
            return (MarkupString)sb.ToString();
        }
    }

    private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);
    internal static string Fmt(double v) => N(Math.Round(v, 3));
}
