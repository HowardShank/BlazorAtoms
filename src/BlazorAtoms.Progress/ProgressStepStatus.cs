namespace BlazorAtoms.Progress;

/// <summary>Per-step state on <c>AtomProgressSteps</c>, emitted as <c>data-status</c> on each step so
/// every part of a step's look is one CSS block. Derived from <c>Current</c> by default; override
/// per index with <c>StatusFor</c>.</summary>
public enum ProgressStepStatus
{
    /// <summary>Not reached yet.</summary>
    Pending,

    /// <summary>The step the user is on.</summary>
    Active,

    /// <summary>Finished.</summary>
    Complete,

    /// <summary>Finished (or attempted) and failed. Never inferred from <c>Current</c> — only
    /// <c>StatusFor</c> can produce it.</summary>
    Error,
}
