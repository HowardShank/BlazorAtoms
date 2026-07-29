namespace BlazorAtoms.Inputs;

/// <summary>
/// Which native DOM event commits a text-ish field's value back through <c>ValueChanged</c>.
/// </summary>
public enum InputUpdateOn
{
    /// <summary>Every keystroke (native <c>oninput</c>). Default — live binding.</summary>
    Input,

    /// <summary>Only on commit: blur or Enter (native <c>onchange</c>). Cheaper on Blazor Server,
    /// where each event is a round trip.</summary>
    Change,
}
