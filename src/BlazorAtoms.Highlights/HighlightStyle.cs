namespace BlazorAtoms.Highlights;

/// <summary>
/// Visual treatment applied to highlighted text matches.
/// </summary>
public enum HighlightStyle
{
    /// <summary>Painted background highlight, like a marker. Default.</summary>
    Mark,

    /// <summary>Text underline in the highlight color.</summary>
    Underline,

    /// <summary>Outlined box around matched text.</summary>
    Outline,
}
