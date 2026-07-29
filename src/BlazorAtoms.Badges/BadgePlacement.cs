namespace BlazorAtoms.Badges;

/// <summary>Corner (or centered edge) the badge sits at when it overlays a host element.
/// Ignored when the badge renders inline (no host <c>ChildContent</c>).</summary>
/// <remarks>
/// Prefixed <c>Badge*</c> because <c>BlazorAtoms.Tooltips</c> declares its own <c>Placement</c>: a
/// page that <c>@using</c>s both packages would otherwise have no unambiguous <c>Placement</c>. The
/// parameter on <see cref="AtomBadge"/> is still called <c>Placement</c>.
/// </remarks>
public enum BadgePlacement
{
    TopEnd,
    TopStart,
    BottomEnd,
    BottomStart,
    TopCenter,
    BottomCenter,
}
