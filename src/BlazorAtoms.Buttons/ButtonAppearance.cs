namespace BlazorAtoms.Buttons;

/// <summary>
/// Fill treatment — how the <see cref="ButtonVariant"/> accent paints background, text, and border.
/// Mirrors <c>BlazorAtoms.Badges</c>'s Solid/Soft/Outline set, with two button-only additions.
/// </summary>
public enum ButtonAppearance
{
    /// <summary>Solid accent fill with contrasting text, no border. Default.</summary>
    Solid,

    /// <summary>Low-opacity tint of the accent as background, accent-colored text, no border.</summary>
    Soft,

    /// <summary>Transparent fill, accent-colored text, accent border.</summary>
    Outline,

    /// <summary>No fill or border until hover, accent-colored text — for toolbar/secondary actions.</summary>
    Ghost,

    /// <summary>Renders as inline text with an underline on hover. Keeps button semantics; use it when
    /// the action must look like a link but isn't navigation.</summary>
    Link,
}
