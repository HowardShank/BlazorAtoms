namespace BlazorAtoms.Progress;

/// <summary>Axis <c>AtomProgressSteps</c> lays its steps out along.</summary>
public enum ProgressStepsOrientation
{
    /// <summary>Steps run left-to-right, labels under each marker. Default.</summary>
    Horizontal,

    /// <summary>Steps stack top-to-bottom, labels beside each marker.</summary>
    Vertical,
}
