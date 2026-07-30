using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Skeletons;

/// <summary>
/// A paragraph placeholder: <see cref="Lines"/> stacked bars with the last one short, which is what
/// makes it read as prose rather than as a table.
/// </summary>
/// <remarks>
/// <para>Line widths are <b>deterministic</b> — full width for every line except the last. Randomised
/// widths would look more organic but would differ between the prerender and interactive passes,
/// producing a visible jump on hydration; <c>AtomTextSparkle</c> hit exactly that and had to replace
/// <c>Random</c> with a function of the index. If you want variation, vary
/// <see cref="LastLineWidth"/>.</para>
/// <para>The narrowing is skipped when <see cref="Lines"/> is 1: a lone short bar looks like a mistake
/// rather than the end of a paragraph.</para>
/// </remarks>
public partial class AtomSkeletonText : AtomSkeletonBase
{
    /// <summary>How many lines to draw. Clamped at 0, which renders an empty (still styled) container
    /// rather than throwing — a caller binding this to <c>items.Count</c> should not have to guard.</summary>
    [Parameter] public int Lines { get; set; } = 3;

    /// <summary>Height of each line, any CSS length. Default <c>0.8rem</c> — deliberately shorter than
    /// <see cref="AtomSkeletonBlock"/>'s own <c>1rem</c>, which reads as chunky against the line gap.</summary>
    [Parameter] public string LineHeight { get; set; } = "0.8rem";

    /// <summary>Corner radius of each line, any CSS length. Default <c>4px</c> (CSS, via the block).</summary>
    [Parameter] public string? LineRadius { get; set; }

    /// <summary>Vertical space between lines → <c>--skeleton-gap</c>. Default <c>0.55rem</c> (CSS).</summary>
    [Parameter] public string? Gap { get; set; }

    /// <summary>Width of the final line → the ragged edge that makes this look like text. Default
    /// <c>60%</c>. Ignored when <see cref="Lines"/> is 1.</summary>
    [Parameter] public string LastLineWidth { get; set; } = "60%";

    /// <summary>Width of the block of text → <c>--skeleton-width</c>. Default <c>100%</c> (CSS).</summary>
    [Parameter] public string? Width { get; set; }

    private int LineCount => Math.Max(0, Lines);

    /// <summary>Last line short, every other line full — see the class remarks on determinism.</summary>
    private string? LineWidth(int index) =>
        LineCount > 1 && index == LineCount - 1 ? LastLineWidth : null;

    private string? RootStyle => BuildRootStyle(
        new StyleVars("skeleton")
            .Add("width", Width)
            .Add("gap", Gap)
            .ToString());
}
