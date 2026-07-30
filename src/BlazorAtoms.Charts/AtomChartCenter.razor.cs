using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// Content for the hole in the middle — a total, a label, an icon. Goes in the <c>Center</c> slot on
/// <see cref="AtomDonut"/> or <see cref="AtomGauge"/>.
/// </summary>
/// <remarks>
/// <para>HTML overlaid on the graphic rather than SVG text inside it, so it inherits the page's typography
/// instead of scaling with the <c>viewBox</c>.</para>
/// <para><b>Not a hit target.</b> Pointer events pass straight through to the arcs underneath, whose
/// <c>&lt;title&gt;</c> elements are the tooltips — a centred div would otherwise swallow hovers over the
/// middle of the ring and the tooltips would go quiet there.</para>
/// <para>On a gauge with a needle, prefer <see cref="AtomChartReadout"/> for the value itself: it knows to
/// sit clear of the hub, which is drawn at the exact centre. This element centres precisely, which on a
/// partial dial means on top of it.</para>
/// </remarks>
public partial class AtomChartCenter : AtomChartElementBase
{
    /// <summary>What goes in the hole.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
