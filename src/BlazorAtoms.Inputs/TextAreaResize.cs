namespace BlazorAtoms.Inputs;

/// <summary>
/// Which axes the user may drag <see cref="AtomTextArea"/>'s native resize grip along
/// (CSS <c>resize</c>).
/// </summary>
public enum TextAreaResize
{
    /// <summary>No grip — size is fixed by <c>Rows</c>/<c>Height</c>/<c>Width</c>.</summary>
    None,

    /// <summary>Height only. Default.</summary>
    Vertical,

    /// <summary>Width only.</summary>
    Horizontal,

    /// <summary>Both axes.</summary>
    Both,
}
