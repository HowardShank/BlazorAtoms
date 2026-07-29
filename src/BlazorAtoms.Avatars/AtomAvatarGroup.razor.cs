using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Avatars;

/// <summary>
/// A row of overlapping avatars. Give it <see cref="Names"/> and it renders an
/// <see cref="AtomInitialsAvatar"/> per name, capping at <see cref="Max"/> and adding a "+N"
/// overflow chip for the rest. Alternatively supply <see cref="ChildContent"/> with your own
/// avatars (image/silhouette) — they overlap the same way, but no automatic overflow chip.
/// </summary>
public partial class AtomAvatarGroup : AtomComponentBase
{
    /// <summary>Names → one initials avatar each (with the automatic overflow chip). Ignored when null.</summary>
    [Parameter] public IReadOnlyList<string>? Names { get; set; }

    /// <summary>Max avatars shown before collapsing the rest into a "+N" chip. 0 = show all. Only for <see cref="Names"/>.</summary>
    [Parameter] public int Max { get; set; }

    /// <summary>Free-form avatars, used when <see cref="Names"/> is null. No automatic overflow chip.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Avatar size (px) for generated avatars.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Crop shape for generated avatars.</summary>
    [Parameter] public AvatarShape Shape { get; set; } = AvatarShape.Circle;

    /// <summary>How far each avatar overlaps the previous one, in px. Sets <c>--avg-overlap</c>.</summary>
    [Parameter] public double Overlap { get; set; } = 12;

    /// <summary>Ring color drawn around each avatar to separate overlaps. Sets <c>--avg-ring-color</c>.</summary>
    [Parameter] public string RingColor { get; set; } = "#ffffff";

    /// <summary>Ring width in px. Sets <c>--avg-ring</c>.</summary>
    [Parameter] public double RingWidth { get; set; } = 2;

    private static string N(double v) => v.ToString(CultureInfo.InvariantCulture);

    private IEnumerable<string> VisibleNames =>
        Names is null ? Enumerable.Empty<string>()
        : (Max > 0 ? Names.Take(Max) : Names);

    private int OverflowCount => Names is not null && Max > 0 && Names.Count > Max ? Names.Count - Max : 0;

    private string RootStyle => string.Concat(
        $"--avg-overlap:{N(Overlap)}px;",
        $"--avg-ring:{N(RingWidth)}px;",
        $"--avg-ring-color:{RingColor};");
}
