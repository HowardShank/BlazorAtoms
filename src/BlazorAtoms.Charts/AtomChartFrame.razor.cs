using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// Layout infrastructure. Arranges a chart's heading, plot, legend, caption and empty state, and
/// cascades the <see cref="ChartContext"/> the element components read from.
/// </summary>
/// <remarks>
/// <para><b>Not something you place yourself.</b> Each chart renders one of these inside its own root
/// element and hands it the slots the caller filled. It is public only because a Razor component cannot
/// be made internal — the Razor compiler emits <c>public partial class</c>, so an <c>internal</c>
/// code-behind would be a <c>CS0262</c> accessibility conflict.</para>
/// <para><b>Why a component rather than markup repeated in each chart.</b> Scoped CSS belongs to the
/// component that declares the markup, so five copies of these area divs would need five copies of the
/// layout rules that position them — and a layout change would then have to land in five stylesheets
/// without drifting. One component means one scope and one stylesheet.</para>
/// <para>The areas stack in the chart root's own flex column; the body is a flex row so a legend placed
/// at the end sits beside the plot and wraps beneath it when there is no room.</para>
/// </remarks>
public partial class AtomChartFrame : ComponentBase
{
    /// <summary>The chart's per-render state, cascaded to every element inside.</summary>
    [Parameter, EditorRequired] public ChartContext? Chart { get; set; }

    /// <summary>The chart's own drawing — its <c>&lt;svg&gt;</c> plus any HTML label rows that have to
    /// stay aligned with it.</summary>
    [Parameter] public RenderFragment? Plot { get; set; }

    /// <summary>Caller's <c>Heading</c> slot.</summary>
    [Parameter] public RenderFragment? Heading { get; set; }

    /// <summary>Caller's <c>Caption</c> slot.</summary>
    [Parameter] public RenderFragment? Caption { get; set; }

    /// <summary>Caller's <c>Legend</c> slot.</summary>
    [Parameter] public RenderFragment? Legend { get; set; }

    /// <summary>Caller's <c>EmptyState</c> slot.</summary>
    [Parameter] public RenderFragment? EmptyState { get; set; }

    /// <summary>Which area <see cref="Legend"/> renders into.</summary>
    [Parameter] public ChartLegendPlacement Placement { get; set; }

    private bool ShowsLegendBeside => Legend is not null && Placement == ChartLegendPlacement.End;

    private bool ShowsLegendBelow => Legend is not null && Placement == ChartLegendPlacement.Below;

    /// <summary>
    /// The empty state shows only when the chart has data to show and hasn't got any.
    /// </summary>
    /// <remarks>
    /// <c>Chart?.HasData == false</c> rather than <c>!Chart?.HasData ?? false</c>: a null context means
    /// nothing is known about the data, and covering the plot on that basis would hide a chart that is
    /// drawing perfectly well.
    /// </remarks>
    private bool ShowsEmptyState => EmptyState is not null && Chart?.HasData == false;
}
