using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Skeletons;

/// <summary>
/// A portrait placeholder — one square block with the corner radius its <see cref="Shape"/> implies.
/// </summary>
/// <remarks>
/// <para>Deliberately has <b>no <c>Radius</c> parameter</b>: <see cref="Shape"/> owns the corners, and a
/// <c>Radius</c> that the default <see cref="SkeletonAvatarShape.Circle"/> silently ignored would be a
/// parameter that is invalid for the default value. Callers who want an arbitrary radius want
/// <see cref="AtomSkeletonBlock"/>, which has one.</para>
/// <para>Likewise one <see cref="Size"/> rather than Width+Height: an avatar is square by definition, and
/// two independent axes would let callers build a shape no real avatar can occupy — a "circle" avatar
/// with different width and height would render as an ellipse.</para>
/// <para>This component adds no markup and has no stylesheet: it renders an
/// <see cref="AtomSkeletonBlock"/> and forwards everything. A wrapper element would only add a box to
/// lay out, and a stylesheet here could not style the block anyway (see
/// <see cref="AtomSkeletonBase"/>).</para>
/// </remarks>
public partial class AtomSkeletonAvatar : AtomSkeletonBase
{
    /// <summary>Both dimensions, any CSS length. Default <c>40px</c>.</summary>
    [Parameter] public string Size { get; set; } = "40px";

    /// <summary>Outline. Default <see cref="SkeletonAvatarShape.Circle"/>.</summary>
    [Parameter] public SkeletonAvatarShape Shape { get; set; } = SkeletonAvatarShape.Circle;

    /// <summary>Corner radius for the current <see cref="Shape"/>. A percentage for
    /// <see cref="SkeletonAvatarShape.Circle"/> so it stays round at any <see cref="Size"/>.</summary>
    private string ShapeRadius => Shape switch
    {
        SkeletonAvatarShape.Square => "0",
        SkeletonAvatarShape.Rounded => "0.5rem",
        _ => "50%",
    };
}
