namespace BlazorAtoms.Avatars;

/// <summary>Avatar crop shape. The image/silhouette is clipped to this outline.</summary>
public enum Shape
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
