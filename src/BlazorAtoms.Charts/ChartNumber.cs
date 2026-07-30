using System.Globalization;

namespace BlazorAtoms.Charts;

/// <summary>
/// Invariant-culture coordinate formatting, shared by the chart bases and the element components.
/// </summary>
/// <remarks>
/// Extracted so <see cref="AtomChartElementBase"/> can format coordinates without inheriting from
/// <see cref="AtomChartBase"/> — an element is not a chart, and giving it a chart's parameters to get at
/// one static method would be the wrong trade.
/// </remarks>
internal static class ChartNumber
{
    /// <summary>
    /// Rounded to three decimals, formatted invariant, with negative zero collapsed.
    /// </summary>
    /// <remarks>
    /// All three matter. A locale that writes <c>0,5</c> produces coordinates the browser silently
    /// discards (there is a culture test for it), unrounded doubles bloat the markup, and <c>-0</c>
    /// otherwise appears in every zero-offset dash attribute.
    /// </remarks>
    internal static string N(double v)
    {
        var r = Math.Round(v, 3);
        return r == 0 ? "0" : r.ToString(CultureInfo.InvariantCulture);
    }
}
