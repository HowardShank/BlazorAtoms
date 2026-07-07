namespace BlazorAtoms.Badges;

/// <summary>Corner (or centered edge) the badge sits at when it overlays a host element.
/// Ignored when the badge renders inline (no host <c>ChildContent</c>).</summary>
public enum Placement
{
    TopEnd,
    TopStart,
    BottomEnd,
    BottomStart,
    TopCenter,
    BottomCenter,
}
