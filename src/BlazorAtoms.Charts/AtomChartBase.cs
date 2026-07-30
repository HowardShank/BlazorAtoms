using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Charts;

/// <summary>
/// What all five charts share: the CSS box, the series colour, the draw-in animation and the
/// accessibility wrapper. Deliberately does <b>not</b> carry the data — see the remarks.
/// </summary>
/// <remarks>
/// <para><b>Three bases, not one.</b> <see cref="AtomGauge"/> plots a single <c>Value</c>, so
/// <c>Values</c>/<c>Labels</c> live one level down on <see cref="AtomSeriesChartBase"/>; and gridlines
/// mean nothing on a dial or a donut, so those live one level further down on
/// <see cref="AtomCartesianChartBase"/>. Same split, for the same reason, as
/// <c>AtomProgressBase</c>/<c>AtomProgressValueBase</c>: a parameter that is silently meaningless on the
/// type carrying it is worse than a slightly deeper hierarchy.</para>
/// <para><b>Geometry is computed in C#, in fixed view units.</b> Each component declares its own
/// <c>viewBox</c> in user units and lays its marks out inside it; <see cref="Width"/> and
/// <see cref="Height"/> only size the CSS box. The alternative — a unit <c>viewBox</c> with
/// <c>preserveAspectRatio="none"</c> — would stretch non-uniformly, which turns a point marker circle
/// into an ellipse and needs <c>vector-effect</c> on every stroke to stop line weights distorting.
/// Uniform scaling costs nothing and keeps round things round.</para>
/// <para><b>Hover and naming come from SVG, not from interop.</b> Each mark carries a
/// <c>&lt;title&gt;</c>, which the browser shows as a tooltip and assistive tech reads — so there is no
/// positioning code, no <c>JSInterop</c>, and nothing to fail in a torn-down circuit. The chart root is
/// <c>role="img"</c> with a generated name, because a bag of <c>&lt;rect&gt;</c>s is not otherwise
/// something a screen reader can describe.</para>
/// </remarks>
public abstract class AtomChartBase : AtomComponentBase
{
    /// <summary>CSS width of the chart box. Default <c>100%</c> (CSS).</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>CSS height of the chart box. Default is per-component CSS.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Colour of the data marks → <c>--chart-series-color</c>. Defaults to
    /// <c>currentColor</c>, so a chart inherits its surrounding text colour.</summary>
    [Parameter] public string? SeriesColor { get; set; }

    /// <summary>When true (the default) the chart draws itself in on first render. Suppressed entirely
    /// under <c>prefers-reduced-motion: reduce</c>.</summary>
    [Parameter] public bool Animate { get; set; } = true;

    /// <summary>Draw-in duration as a CSS duration, e.g. <c>"600ms"</c> →
    /// <c>--chart-duration</c>.</summary>
    [Parameter] public string? Duration { get; set; }

    /// <summary>Accessible name for the chart. Falls back to a generated summary, so the graphic is
    /// never unnamed.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>When false, hidden via CSS <c>display:none</c> (stays in the DOM). Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>Above the plot. Put an <see cref="AtomChartHeading"/> here.</summary>
    /// <remarks>Named <c>Heading</c> rather than <c>Title</c>: a <c>&lt;Title&gt;</c> tag in consumer
    /// markup sits one letter of casing away from SVG's own <c>&lt;title&gt;</c> and reads as a bug.</remarks>
    [Parameter] public RenderFragment? Heading { get; set; }

    /// <summary>Beneath everything else. Put an <see cref="AtomChartCaption"/> here.</summary>
    [Parameter] public RenderFragment? Caption { get; set; }

    /// <summary>Beside or beneath the plot per <see cref="LegendPlacement"/>. Put an
    /// <see cref="AtomChartLegend"/> here.</summary>
    [Parameter] public RenderFragment? Legend { get; set; }

    /// <summary>Shown over the plot when there is nothing to draw. Put an
    /// <see cref="AtomChartEmptyState"/> here.</summary>
    /// <remarks>Without it an empty series renders a silently blank box, which looks like a broken
    /// component rather than an empty result set.</remarks>
    [Parameter] public RenderFragment? EmptyState { get; set; }

    /// <summary>Which layout area <see cref="Legend"/> renders into. Null uses the per-chart default —
    /// beside the plot on <see cref="AtomDonut"/>, beneath it everywhere else.</summary>
    [Parameter] public ChartLegendPlacement? LegendPlacement { get; set; }

    /// <summary>Per-component fallback for <see cref="LegendPlacement"/>.</summary>
    protected virtual ChartLegendPlacement DefaultLegendPlacement => ChartLegendPlacement.Below;

    /// <summary>The placement actually used. Nullable parameter plus <c>??</c>, the same precedence
    /// mechanism <c>AtomCardSectionBase</c> uses — null means "not set", so no
    /// <c>ParameterView</c> inspection is needed to tell a default from a choice.</summary>
    protected ChartLegendPlacement EffectiveLegendPlacement =>
        LegendPlacement ?? DefaultLegendPlacement;

    /// <summary>Per-component fallback name, used when <see cref="AriaLabel"/> is not set.</summary>
    protected abstract string DefaultAriaLabel { get; }

    /// <summary>The name actually rendered — never null.</summary>
    protected string EffectiveAriaLabel => AriaLabel ?? DefaultAriaLabel;

    /// <summary>Null when <see cref="Animate"/> is false, so the attribute is absent and the CSS
    /// animation rules simply do not apply.</summary>
    protected string? AnimateAttr => Animate ? "true" : null;

    /// <summary>Shared <c>--chart-*</c> block plus the visibility toggle. Derived components append
    /// their own declarations via <paramref name="extra"/> (last, so they win).</summary>
    protected string? BuildRootStyle(string? extra = null)
    {
        var vars = new StyleVars("chart")
            .Add("width", Width)
            .Add("height", Height)
            .Add("series-color", SeriesColor)
            .Add("duration", Duration)
            .ToString();

        var s = (Visible ? "" : "display:none;") + vars + extra;
        return string.IsNullOrEmpty(s) ? null : s;
    }

    /// <summary>Invariant-culture number formatting, rounded to keep the markup readable. Required for
    /// SVG and CSS alike: a locale that writes <c>0,5</c> produces coordinates the browser discards.</summary>
    protected static string N(double v) => ChartNumber.N(v);
}
