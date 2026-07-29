namespace BlazorAtoms.Progress;

/// <summary>What <c>AtomProgressSteps</c> draws in each step's marker circle.</summary>
public enum ProgressStepMarker
{
    /// <summary>1-based step number. Default.</summary>
    Number,

    /// <summary>A filled dot, no text.</summary>
    Dot,

    /// <summary>The step number until complete, then a checkmark.</summary>
    Check,

    /// <summary>Nothing — the marker is an empty circle, for callers styling it themselves via
    /// <c>--progress-*</c> or a <c>::before</c>.</summary>
    None,
}
