namespace BlazorAtoms.Avatars;

/// <summary>Avatar crop shape. The image/silhouette is clipped to this outline.</summary>
/// <remarks>
/// Prefixed <c>Avatar*</c> per the repo convention that a cross-package enum name carries its
/// package's noun — <c>BadgeShape</c>, <c>ButtonShape</c>, <c>TooltipShape</c> and this one would
/// otherwise all be a bare <c>Shape</c>, leaving no unambiguous name for a page that
/// <c>@using</c>s more than one. The parameter on each component is still called <c>Shape</c>.
/// </remarks>
public enum AvatarShape
{
    /// <summary>Circle (default).</summary>
    Circle,
    /// <summary>Square with sharp corners.</summary>
    Square,
    /// <summary>Rounded rectangle; corner radius via <c>Radius</c>.</summary>
    Rounded,
    /// <summary>Squircle — heavily rounded square.</summary>
    Squircle,
    /// <summary>Pointy-top hexagon (clip-path).</summary>
    Hexagon,
}
