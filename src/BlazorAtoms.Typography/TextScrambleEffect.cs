namespace BlazorAtoms.Typography;

/// <summary>Which per-character entrance animation <see cref="AtomTextScramble"/> plays.</summary>
public enum TextScrambleEffect
{
    /// <summary>Characters fly in from the upper-left, rotating and shrinking from an oversized start.</summary>
    RevolveScale,

    /// <summary>Characters drop in from the upper-right like a bouncing ball.</summary>
    BallDrop,

    /// <summary>Characters slide in from the left, overshoot, then settle with a color flash.</summary>
    SideSlide,

    /// <summary>Characters spin down from above, unrolling into place.</summary>
    RevolveDrop,

    /// <summary>Like <see cref="RevolveDrop"/>, but flings off to the upper-left mid-flight before
    /// settling back into place.</summary>
    DropVanish,

    /// <summary>Characters twist in from a rotated, offset start position.</summary>
    Twister,

    /// <summary>Characters slide in from the left, overshoot past center with a color change, then
    /// settle.</summary>
    LeftRight,
}
