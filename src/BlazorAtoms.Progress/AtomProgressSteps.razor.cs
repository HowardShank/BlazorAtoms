using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Progress;

/// <summary>
/// A discrete step tracker (wizard / checkout / onboarding): one marker per step with a connector
/// between them, each carrying a <see cref="ProgressStepStatus"/>. Pure CSS — no JS in any render
/// mode.
/// </summary>
/// <remarks>
/// <para>Inherits <see cref="AtomProgressBase"/> and <b>not</b>
/// <see cref="AtomProgressValueBase"/>: there is no continuous value here, so <c>Value</c>/<c>Min</c>/
/// <c>Max</c> would be dead parameters. Progress is <see cref="Current"/> — an index into
/// <see cref="Steps"/>.</para>
/// <para><see cref="ProgressStepStatus.Error"/> is never inferred from <see cref="Current"/>; it
/// requires <see cref="StatusFor"/>, because "the user is on step 3" says nothing about whether step
/// 2 failed.</para>
/// </remarks>
public partial class AtomProgressSteps : AtomProgressBase
{
    /// <summary>The step captions, in order. An empty or null list renders an empty list element.</summary>
    [Parameter] public IReadOnlyList<string>? Steps { get; set; }

    /// <summary>Zero-based index of the step the user is on. Earlier steps are
    /// <see cref="ProgressStepStatus.Complete"/>, later ones
    /// <see cref="ProgressStepStatus.Pending"/>. A value at or past <c>Steps.Count</c> marks every
    /// step complete (the "finished" state); a negative value marks them all pending.</summary>
    [Parameter] public int Current { get; set; }

    /// <summary>Axis the steps lay out along → <c>data-orientation</c>. Default
    /// <see cref="ProgressStepsOrientation.Horizontal"/>.</summary>
    [Parameter] public ProgressStepsOrientation Orientation { get; set; } = ProgressStepsOrientation.Horizontal;

    /// <summary>What each marker draws. Default <see cref="ProgressStepMarker.Number"/>.</summary>
    [Parameter] public ProgressStepMarker Marker { get; set; } = ProgressStepMarker.Number;

    /// <summary>Overrides the status derived from <see cref="Current"/> for a given index — the only
    /// way to produce <see cref="ProgressStepStatus.Error"/>. Null (default) uses the derived
    /// status throughout.</summary>
    [Parameter] public Func<int, ProgressStepStatus>? StatusFor { get; set; }

    /// <summary>Custom markup for a step's caption, receiving the step index. Null (default) renders
    /// the plain string from <see cref="Steps"/>.</summary>
    [Parameter] public RenderFragment<int>? StepTemplate { get; set; }

    /// <summary>Raised with the clicked step's index. When supplied, every marker renders as a real
    /// <c>&lt;button&gt;</c> — focusable, keyboard-activatable, and named for assistive tech. When
    /// not, the markers are inert <c>&lt;span&gt;</c>s marked <c>aria-hidden</c>, so a
    /// non-navigable tracker adds nothing to the tab order.</summary>
    [Parameter] public EventCallback<int> OnStepClick { get; set; }

    /// <inheritdoc />
    protected override string DefaultAriaLabel => "Progress steps";

    private IReadOnlyList<string> StepList => Steps ?? [];

    private string OrientationAttr => Kebab(Orientation.ToString());

    /// <summary>Readout for <see cref="AtomProgressBase.ShowValue"/>: the 1-based position in the
    /// list, clamped so a <see cref="Current"/> past the end reads as the last step rather than
    /// "6 of 5".</summary>
    private string CountText
    {
        get
        {
            var total = StepList.Count;
            if (total == 0) return "0 of 0";
            var position = Math.Clamp(Current + 1, 1, total);
            return $"{position} of {total}";
        }
    }

    private ProgressStepStatus StatusOf(int index)
    {
        if (StatusFor is not null) return StatusFor(index);
        if (index < Current) return ProgressStepStatus.Complete;
        return index == Current ? ProgressStepStatus.Active : ProgressStepStatus.Pending;
    }

    /// <summary>Accessible name for a clickable marker. The visible caption is a sibling the marker
    /// doesn't contain, so without this the button would announce only its number.</summary>
    private string StepAriaLabel(int index, ProgressStepStatus status)
    {
        var caption = index < StepList.Count ? StepList[index] : $"Step {index + 1}";
        return $"{caption} ({status.ToString().ToLowerInvariant()})";
    }

    private string? RootStyle => BuildRootStyle();
}
