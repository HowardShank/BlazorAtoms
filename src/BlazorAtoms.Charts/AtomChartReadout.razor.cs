using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Charts;

/// <summary>
/// Prints the gauge's value in the middle of the dial. Goes in <see cref="AtomGauge"/>'s <c>Readout</c>
/// slot.
/// </summary>
/// <remarks>
/// <para>Positioned just below centre on a partial dial rather than dead centre, because the needle pivots
/// at the centre and its hub is drawn there — a centred readout renders underneath it, which is how this
/// once shipped and looked like a missing value. A full 360° dial has no gap at the bottom to move into, so
/// it stays centred. The chart works out that default from its own sweep angle;
/// <see cref="Offset"/> overrides it.</para>
/// <para>For anything other than the value itself, use <see cref="AtomChartCenter"/>, which takes arbitrary
/// content and centres it exactly.</para>
/// </remarks>
public partial class AtomChartReadout : AtomChartElementBase
{
    /// <summary>
    /// How far below centre to sit, as a fraction of the box. Null uses the chart's sweep-aware default.
    /// </summary>
    /// <remarks>
    /// Negative values move it above centre, which is what a dial sweeping downward wants. Not clamped:
    /// pushing the readout outside the box is a strange thing to ask for but not an incoherent one.
    /// </remarks>
    [Parameter] public double? Offset { get; set; }

    private double EffectiveOffset => Offset ?? Chart?.ReadoutOffset ?? 0;

    /// <summary>
    /// Emits the offset as a percentage on this element's own root, which its stylesheet then reads.
    /// </summary>
    /// <remarks>
    /// A percentage rather than a length, because the CSS resolves it against the dial's <i>height</i> via
    /// <c>top</c> — the one place a percentage does resolve vertically, which is what makes this exact. The
    /// element emits it rather than the chart precisely so <see cref="Offset"/> can live here.
    /// </remarks>
    private string? RootStyle =>
        new StyleVars("chart").Add("readout-offset", N(EffectiveOffset * 100) + "%").ToString();
}
