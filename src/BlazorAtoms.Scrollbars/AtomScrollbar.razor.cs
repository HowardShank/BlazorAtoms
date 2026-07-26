using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Scrollbars;

/// <summary>Wraps arbitrary <see cref="ChildContent"/> in a scroll box with a custom-themed
/// scrollbar. Zero JS — full styling on WebKit browsers, a reduced (color-only) fallback on
/// Firefox via <c>scrollbar-color</c>/<c>scrollbar-width</c>.</summary>
public partial class AtomScrollbar
{
    /// <summary>Content to render inside the scroll box.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Which direction(s) the box scrolls.</summary>
    [Parameter] public ScrollbarAxis Axis { get; set; } = ScrollbarAxis.Vertical;

    /// <summary>Height of the scroll box. Any CSS length. Only matters when <see cref="Axis"/>
    /// is <see cref="ScrollbarAxis.Vertical"/> or <see cref="ScrollbarAxis.Both"/>.</summary>
    [Parameter] public string BoxHeight { get; set; } = "300px";

    /// <summary>Width of the scroll box. Any CSS length.</summary>
    [Parameter] public string BoxWidth { get; set; } = "100%";

    /// <summary>Thickness of the scrollbar itself (WebKit track/thumb width or height). Also
    /// drives the Firefox <c>scrollbar-width</c> fallback: "thin" at 10px or below, "auto" above.</summary>
    [Parameter] public string ScrollbarSize { get; set; } = "12px";

    /// <summary>Track (the channel the thumb slides in) background color.</summary>
    [Parameter] public string TrackColor { get; set; } = "#f5f5f5";

    /// <summary>Corner radius of the track. Any CSS length.</summary>
    [Parameter] public string TrackBorderRadius { get; set; } = "0px";

    /// <summary>Thumb (the draggable handle) color.</summary>
    [Parameter] public string ThumbColor { get; set; } = "#555";

    /// <summary>Optional second color stop — set together with <see cref="ThumbColor"/> to make
    /// the thumb a linear gradient on WebKit. Ignored on Firefox (<c>scrollbar-color</c> takes
    /// solid colors only, so it falls back to <see cref="ThumbColor"/> there).</summary>
    [Parameter] public string? ThumbGradientEnd { get; set; }

    /// <summary>Gradient angle, only used when <see cref="ThumbGradientEnd"/> is set.</summary>
    [Parameter] public string ThumbGradientAngle { get; set; } = "180deg";

    /// <summary>Thumb color while hovered (WebKit only). Defaults to <see cref="ThumbColor"/>
    /// (i.e. no visible hover change) when unset.</summary>
    [Parameter] public string? ThumbHoverColor { get; set; }

    /// <summary>Corner radius of the thumb. Any CSS length.</summary>
    [Parameter] public string ThumbBorderRadius { get; set; } = "0px";

    /// <summary>Raw CSS <c>border</c> shorthand for the thumb, e.g. <c>"2px solid #555"</c>. Null
    /// (default) renders no border.</summary>
    [Parameter] public string? ThumbBorder { get; set; }

    private string AxisClass => Axis switch
    {
        ScrollbarAxis.Horizontal => "atom-scrollbar-axis-horizontal",
        ScrollbarAxis.Both => "atom-scrollbar-axis-both",
        _ => "atom-scrollbar-axis-vertical",
    };

    private string ThumbBackground => string.IsNullOrEmpty(ThumbGradientEnd)
        ? ThumbColor
        : $"linear-gradient({ThumbGradientAngle}, {ThumbColor}, {ThumbGradientEnd})";

    private string ThumbHoverBackground => string.IsNullOrEmpty(ThumbHoverColor)
        ? ThumbBackground
        : ThumbHoverColor;

    /// <summary>Firefox has no numeric <c>scrollbar-width</c> — only the "thin"/"auto"/"none"
    /// keywords — so <see cref="ScrollbarSize"/>'s CSS length is heuristically mapped by reading
    /// its leading numeric value (unit-agnostic: works for "12px", "0.75rem", etc.).</summary>
    private string FirefoxScrollbarWidth => LeadingNumber(ScrollbarSize) is double n && n <= 10 ? "thin" : "auto";

    private static double? LeadingNumber(string cssLength)
    {
        var end = 0;
        while (end < cssLength.Length && (char.IsDigit(cssLength[end]) || cssLength[end] == '.')) end++;
        return end > 0 && double.TryParse(cssLength[..end], out var n) ? n : null;
    }

    private string RootStyle =>
        $"--atom-scrollbar-box-height:{BoxHeight};" +
        $"--atom-scrollbar-box-width:{BoxWidth};" +
        $"--atom-scrollbar-size:{ScrollbarSize};" +
        $"--atom-scrollbar-track-color:{TrackColor};" +
        $"--atom-scrollbar-track-radius:{TrackBorderRadius};" +
        $"--atom-scrollbar-thumb-bg:{ThumbBackground};" +
        $"--atom-scrollbar-thumb-hover-bg:{ThumbHoverBackground};" +
        $"--atom-scrollbar-thumb-radius:{ThumbBorderRadius};" +
        $"--atom-scrollbar-thumb-border:{ThumbBorder ?? "none"};" +
        $"--atom-scrollbar-firefox-color:{ThumbColor} {TrackColor};" +
        $"--atom-scrollbar-firefox-width:{FirefoxScrollbarWidth};";
}
