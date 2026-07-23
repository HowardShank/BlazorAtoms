namespace BlazorAtoms.Layout;

/// <summary>
/// Defines what <see cref="AtomDrawer"/> and its backdrop are positioned against.
/// </summary>
public enum AtomDrawerAnchor
{
    /// <summary>Fixed to the browser viewport (the default) — the drawer floats over the whole
    /// page and its own scroll position, unaffected by any ancestor's layout or scrolling.</summary>
    Viewport,

    /// <summary>Absolutely positioned against the nearest ancestor that has
    /// <c>position: relative/absolute/fixed/sticky</c>. The drawer and its backdrop fill that
    /// ancestor instead of the viewport, so both stay confined to (and scroll with) that container.
    /// The consumer is responsible for giving that ancestor a non-static <c>position</c> — without
    /// one, this falls back to the nearest positioned ancestor further up the tree, same as any
    /// other absolutely positioned element.</summary>
    Container,
}
