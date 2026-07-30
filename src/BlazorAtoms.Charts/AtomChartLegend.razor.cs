using System.Globalization;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Charts;

/// <summary>
/// A key listing each mark with its colour, label and value. Goes in a chart's <c>Legend</c> slot.
/// </summary>
/// <remarks>
/// <para><b>Worth adding to anything a stranger will read.</b> The per-mark <c>&lt;title&gt;</c> tooltips
/// only appear on hover, and <b>touch devices have no hover</b> — so on a phone an unlabelled donut is a
/// ring of colours with no key at all.</para>
/// <para>HTML rather than SVG text, so it inherits the page's font, wraps, and ellipsises a long label
/// instead of overflowing the graphic. Where it sits is the chart's decision
/// (<c>LegendPlacement</c>), because that picks a layout area and the chart has to know it before it
/// renders; everything about how it looks is decided here.</para>
/// </remarks>
public partial class AtomChartLegend : AtomChartElementBase
{
    /// <summary>How many columns to lay the rows out in. Default 1.</summary>
    /// <remarks>Clamped to 1..6. Beyond that each row is narrower than its own label and the ellipsis
    /// does all the talking.</remarks>
    [Parameter] public int Columns { get; set; } = 1;

    /// <summary>Prints each row's formatted value. Default true.</summary>
    [Parameter] public bool ShowValues { get; set; } = true;

    /// <summary>
    /// Prints each row's share of the total as a percentage. Null (the default) shows it only when the
    /// chart's values actually sum to something.
    /// </summary>
    /// <remarks>
    /// Auto rather than a plain <c>false</c> default because the answer is knowable: a donut's slices are
    /// shares of a whole and their percentages are the interesting number, whereas a line chart's values
    /// do not sum to anything and every row would read "0%". The chart reports a share of zero when there
    /// is no meaningful total, so "any row has a share" is the test.
    /// </remarks>
    [Parameter] public bool? ShowPercent { get; set; }

    private IReadOnlyList<ChartLegendEntry> Rows => Chart?.Legend ?? [];

    private bool ShowsPercent => ShowPercent ?? Rows.Any(r => r.Share > 0);

    /// <summary>Falls back to the inherited series colour, so a chart whose marks are all one colour gets
    /// a swatch that matches them rather than a transparent gap.</summary>
    private static string SwatchColor(ChartLegendEntry row) =>
        string.IsNullOrEmpty(row.Color) ? "var(--chart-series-color, currentColor)" : row.Color;

    private string Format(double v) => Chart?.Format(v) ?? v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string PercentText(ChartLegendEntry row) =>
        Math.Round(row.Share, 1).ToString(CultureInfo.InvariantCulture) + "%";

    /// <summary>
    /// Emits the column count as a custom property, and only when it is more than one — the stylesheet's
    /// own default is a single column, so a redundant declaration would be dead markup.
    /// </summary>
    /// <remarks>
    /// The <c>string</c> overload, not the <c>double?</c> one: that appends <c>px</c>, which would make
    /// <c>repeat(2px, ...)</c> and be dropped as invalid.
    /// </remarks>
    private string? RootStyle
    {
        get
        {
            var columns = Math.Clamp(Columns, 1, 6);
            return new StyleVars("chart")
                .Add("legend-columns", columns > 1 ? columns.ToString(CultureInfo.InvariantCulture) : null)
                .ToString();
        }
    }
}
