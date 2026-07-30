using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// The row (or, for horizontal bars, the column) of category labels beside the marks. Goes in a chart's
/// <c>CategoryAxis</c> slot on <see cref="AtomLineChart"/> or <see cref="AtomBarChart"/>.
/// </summary>
/// <remarks>
/// <para>Reads the chart's <c>Labels</c>, which stay on the chart because they are data — they also name
/// each mark's <c>&lt;title&gt;</c> tooltip whether or not this element is present. Adding it is what puts
/// them on the axis.</para>
/// <para>HTML rather than SVG text: real text inherits the page's font, respects the reader's font-size
/// preference, wraps and ellipsises. SVG text inside a scaled <c>viewBox</c> does none of that. The cost
/// is that these labels are not part of the graphic if someone extracts the SVG, which is the right way
/// round for a component whose output is a live page.</para>
/// <para>A label with no value renders as an empty span rather than being skipped, so each mark keeps its
/// own equal share of the track. Dropping the empty ones would slide every later label off its mark.</para>
/// </remarks>
public partial class AtomChartCategoryAxis : AtomChartElementBase
{
    /// <summary>
    /// Lets a long label wrap onto more lines instead of being cut off with an ellipsis. Default false.
    /// </summary>
    /// <remarks>
    /// Off by default because wrapping changes the row's height, which on a dense axis reflows the whole
    /// chart below it. For long labels the better answer is usually a horizontal
    /// <see cref="AtomBarChart"/>, where each label gets a line of its own.
    /// </remarks>
    [Parameter] public bool Wrap { get; set; }

    private IReadOnlyList<string?> Labels => Chart?.CategoryLabels ?? [];

    private string? WrapAttr => Wrap ? "true" : null;

    /// <summary>Whether the end labels anchor to the plot's edges instead of centring in their own slot
    /// — a line chart's call, not a bar chart's. See <see cref="ChartContext.CategoryLabelsAlignEnds"/>.</summary>
    private string? AlignEndsAttr => Chart?.CategoryLabelsAlignEnds == true ? "true" : null;
}
