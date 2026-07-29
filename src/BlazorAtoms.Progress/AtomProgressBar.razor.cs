using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Progress;

/// <summary>
/// A horizontal determinate progress bar: a track with a fill sized to
/// <see cref="AtomProgressValueBase.Value"/>, an optional secondary <see cref="Buffer"/> span, and an
/// optional formatted readout. A null <c>Value</c> switches it to an indeterminate sweep. Pure CSS —
/// no JS in any render mode.
/// </summary>
public partial class AtomProgressBar : AtomProgressValueBase
{
    /// <summary>Where the readout goes when <see cref="AtomProgressBase.ShowValue"/> is true.
    /// Default <see cref="ProgressValuePosition.Inside"/>.</summary>
    [Parameter] public ProgressValuePosition ValuePosition { get; set; } = ProgressValuePosition.Inside;

    /// <summary>Optional secondary amount on the same <c>Min</c>..<c>Max</c> scale, drawn as a
    /// dimmer band behind the fill — the classic "buffered but not yet played" span. Null (default)
    /// draws nothing. Ignored while indeterminate, where there is no meaningful scale position.</summary>
    [Parameter] public double? Buffer { get; set; }

    /// <summary>Overall bar width. Any CSS length. Null (default) fills the parent.</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Track corner radius in px → <c>--progress-radius</c>. <c>0</c> squares the track off.
    /// Declared here rather than on <see cref="AtomProgressBase"/> because only this component and
    /// <see cref="AtomMeter"/> have a rectangular track to round — on the ring and the step markers it
    /// would be a parameter with nothing to do.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <inheritdoc />
    protected override string DefaultAriaLabel => "Progress";

    private string ValuePositionAttr => Kebab(ValuePosition.ToString());

    /// <summary>Fill width, or none at all while indeterminate — the sweep keyframe animates the
    /// fill's own <c>width</c>/<c>translate</c>, so an inline width would fight it.</summary>
    private string? FillStyle => IsIndeterminate ? null : $"width:{PercentCss};";

    private string? BufferCss
    {
        get
        {
            if (IsIndeterminate || Buffer is null) return null;

            var span = Max - Min;
            if (span <= 0) return null;

            var clamped = Math.Clamp(Buffer.Value, Min, Max);
            return Invariant(Math.Round((clamped - Min) / span * 100, 4)) + "%";
        }
    }

    private string? RootStyle => BuildRootStyle(
        (Radius is null ? null : $"--progress-radius:{Invariant(Radius.Value)}px;") +
        (Width is null ? null : $"width:{Width};"));
}
