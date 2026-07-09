namespace BlazorAtoms.Badges;

/// <summary>Fill treatment for chips and tags. Drives how the color <see cref="Variant"/> paints the
/// background, text and border.</summary>
public enum Appearance
{
    /// <summary>Solid accent fill with contrasting text (no border).</summary>
    Solid,
    /// <summary>Low-opacity tint of the accent as background, accent-colored text (no border).</summary>
    Soft,
    /// <summary>Transparent fill, accent-colored text and a 1px accent border.</summary>
    Outline,
}
