using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Progress;

/// <summary>
/// Adds a scalar value on a <see cref="Min"/>..<see cref="Max"/> scale to
/// <see cref="AtomProgressBase"/>, plus the clamped fraction every continuous indicator draws from.
/// Inherited by <see cref="AtomProgressBar"/>, <see cref="AtomProgressRing"/> and
/// <see cref="AtomMeter"/>; <see cref="AtomProgressSteps"/> deliberately does not inherit it.
/// </summary>
/// <remarks>
/// <para><b><see cref="Value"/> is nullable, and null means indeterminate</b> — one parameter rather
/// than a value plus a separate flag, so a caller cannot express the contradictory
/// "<c>Value=40</c> but ignore it" state. Same reasoning as <c>AtomRating</c>'s <c>double?</c>, where
/// null is a real "unrated" distinct from 0.</para>
/// <para>An out-of-range value is clamped for drawing but reported verbatim in
/// <c>aria-valuenow</c>-adjacent readouts only after clamping, so the visual and the announced value
/// never disagree.</para>
/// </remarks>
public abstract class AtomProgressValueBase : AtomProgressBase
{
    /// <summary>Current amount on the <see cref="Min"/>..<see cref="Max"/> scale. <b>Null (the
    /// default) means indeterminate</b>: the component plays its own "unknown amount" animation, and
    /// <c>aria-valuenow</c> is omitted so assistive tech announces a busy state rather than a
    /// number.</summary>
    [Parameter] public double? Value { get; set; }

    /// <summary>Low end of the scale. Default 0.</summary>
    [Parameter] public double Min { get; set; } = 0;

    /// <summary>High end of the scale. Default 100. A <see cref="Max"/> at or below
    /// <see cref="Min"/> collapses the scale, and <see cref="Fraction"/> reports 0 rather than
    /// dividing by zero.</summary>
    [Parameter] public double Max { get; set; } = 100;

    /// <summary>Formats the value for display. Receives the clamped value. Null (default) renders a
    /// whole-number percentage of the scale (<c>"42%"</c>).</summary>
    [Parameter] public Func<double, string>? Formatter { get; set; }

    /// <summary>True when no <see cref="Value"/> was supplied.</summary>
    protected bool IsIndeterminate => Value is null;

    /// <summary>The value clamped into <see cref="Min"/>..<see cref="Max"/>, or null when
    /// indeterminate.</summary>
    protected double? ClampedValue =>
        Value is null ? null : Math.Clamp(Value.Value, Min, Math.Max(Min, Max));

    /// <summary>Progress as 0..1. Zero when indeterminate or when the scale has no span.</summary>
    protected double Fraction
    {
        get
        {
            if (ClampedValue is not { } v) return 0;
            var span = Max - Min;
            return span <= 0 ? 0 : (v - Min) / span;
        }
    }

    /// <summary>Progress as 0..100, for CSS percentage lengths.</summary>
    protected double Percent => Fraction * 100;

    /// <summary>Invariant-culture percentage for a CSS <c>width</c>/<c>inset</c> value.</summary>
    protected string PercentCss => Invariant(Math.Round(Percent, 4)) + "%";

    /// <summary>The readout text: <see cref="Formatter"/> if supplied, else a whole-number percent.
    /// Null when indeterminate (there is no number to show).</summary>
    protected string? DisplayValue =>
        ClampedValue is not { } v ? null
        : Formatter is not null ? Formatter(v)
        : Invariant(Math.Round(Percent)) + "%";

    /// <summary><c>aria-valuenow</c>, omitted when indeterminate per the ARIA spec (a progressbar
    /// with no valuenow is the indeterminate case).</summary>
    protected string? AriaValueNow => ClampedValue is { } v ? Invariant(v) : null;

    protected string AriaValueMin => Invariant(Min);

    protected string AriaValueMax => Invariant(Max);

    /// <summary><c>data-indeterminate</c>, emitted only when true so determinate is attribute-free.</summary>
    protected string? IndeterminateAttr => IsIndeterminate ? "true" : null;
}
