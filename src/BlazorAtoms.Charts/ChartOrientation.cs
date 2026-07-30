namespace BlazorAtoms.Charts;

/// <summary>Which way an <see cref="AtomBarChart"/>'s bars grow. Emitted as <c>data-orientation</c>.</summary>
public enum ChartOrientation
{
    /// <summary>Bars rise from a bottom baseline. The default.</summary>
    Vertical,

    /// <summary>Bars extend rightward from a left baseline — the one to use when labels are long, since
    /// they then sit on their own line beside each bar instead of competing for width beneath it.</summary>
    Horizontal,
}
