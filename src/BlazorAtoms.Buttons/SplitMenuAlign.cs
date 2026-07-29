namespace BlazorAtoms.Buttons;

/// <summary>
/// Which edge of <see cref="AtomSplitButton"/> its dropped menu panel lines up with. There is no
/// automatic flip on collision — that needs measurement, i.e. JS (see the component's remarks).
/// </summary>
public enum SplitMenuAlign
{
    /// <summary>Panel's leading edge matches the button's. Default.</summary>
    Start,

    /// <summary>Panel's trailing edge matches the button's — for a control near the right edge.</summary>
    End,
}
