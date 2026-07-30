namespace BlazorAtoms.Charts;

/// <summary>
/// A coloured zone on an <see cref="AtomGauge"/>'s track, ending at <paramref name="UpTo"/>.
/// </summary>
/// <param name="UpTo">Upper edge of the band, in the gauge's own value units (not a fraction). Bands are
/// read in order and each starts where the previous one ended, so only the ends need stating.</param>
/// <param name="Color">Any CSS colour.</param>
/// <remarks>
/// A library type rather than a tuple because it is configuration a caller writes out by hand, where
/// named members document themselves — unlike the chart <i>data</i>, which stays a plain
/// <c>IEnumerable&lt;double&gt;</c> precisely so callers never have to construct our types to plot
/// theirs.
/// </remarks>
public readonly record struct GaugeBand(double UpTo, string Color);
