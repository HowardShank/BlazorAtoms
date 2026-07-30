namespace BlazorAtoms.Skeletons;

/// <summary>
/// Outline of an <see cref="AtomSkeletonAvatar"/>. Chosen to mirror <c>BlazorAtoms.Avatars</c>, so a
/// skeleton can be swapped for the real avatar without the shape changing under the reader.
/// </summary>
/// <remarks>
/// This is why <see cref="AtomSkeletonAvatar"/> has no <c>Radius</c> parameter: the shape owns the
/// corner radius, and a <c>Radius</c> that <see cref="Circle"/> silently ignored would be a parameter
/// that is invalid for the default value.
/// </remarks>
public enum SkeletonAvatarShape
{
    /// <summary>Fully round. The default.</summary>
    Circle,

    /// <summary>Hard corners.</summary>
    Square,

    /// <summary>Softly rounded corners.</summary>
    Rounded,
}
