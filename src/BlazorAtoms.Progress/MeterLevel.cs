namespace BlazorAtoms.Progress;

/// <summary>How good <c>AtomMeter</c>'s current value is, given <c>Low</c>/<c>High</c>/<c>Optimum</c>.
/// Emitted as <c>data-level</c> so the fill's color is one CSS block per band. Member names are the
/// HTML <c>&lt;meter&gt;</c> spec's own terms.</summary>
public enum MeterLevel
{
    /// <summary>In the span the stated <c>Optimum</c> falls in — the good band.</summary>
    Optimum,

    /// <summary>One band away from optimum — the caution band.</summary>
    Suboptimum,

    /// <summary>Two bands away — the alarm band. Only reachable when <c>Optimum</c> lies outside
    /// <c>Low</c>..<c>High</c>; with an <c>Optimum</c> between them the spec defines no third
    /// band.</summary>
    SubSuboptimum,
}
