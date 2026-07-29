namespace BlazorAtoms.Inputs;

/// <summary>
/// Frame treatment shared by every <see cref="AtomInputBase{TValue}"/>-derived field. Emitted as
/// <c>data-variant</c> on the component root; each member is one CSS rule block, so adding a
/// variant later needs no C# change beyond the enum member.
/// </summary>
public enum InputVariant
{
    /// <summary>Border on all four sides (default).</summary>
    Outline,

    /// <summary>Tinted background, no border except the bottom edge on focus.</summary>
    Filled,

    /// <summary>Bottom rule only — no box.</summary>
    Underline,
}
