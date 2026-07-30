using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// A chart's title, with an optional second line. Goes in a chart's <c>Heading</c> slot.
/// </summary>
/// <remarks>
/// <para>Visual, not structural: it renders a <c>&lt;div&gt;</c> of spans rather than an
/// <c>&lt;h1&gt;</c>–<c>&lt;h6&gt;</c>, because the right heading level depends on the page's outline and
/// a component cannot know it. Put this inside your own heading element if the chart's title is a
/// document heading.</para>
/// <para>It is also not the chart's accessible name. The chart root is <c>role="img"</c> with its own
/// <c>aria-label</c>; set <c>AriaLabel</c> for what assistive tech reads and use this for what sighted
/// readers see. Repeating the title in both is fine and usually right.</para>
/// </remarks>
public partial class AtomChartHeading : AtomChartElementBase
{
    /// <summary>The title itself.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>A quieter second line beneath the title — a period, a unit, a source. Omitted entirely
    /// when null or empty, so there is no empty element to collapse.</summary>
    [Parameter] public string? Subtitle { get; set; }
}
