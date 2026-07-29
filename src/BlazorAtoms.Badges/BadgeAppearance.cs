namespace BlazorAtoms.Badges;

/// <summary>Fill treatment for chips and tags. Drives how the color <see cref="BadgeVariant"/> paints the
/// background, text and border.</summary>
/// <remarks>
/// Prefixed <c>Badge*</c> to match the rest of this package's enums and to stay clear of another
/// package's fill treatment — <c>BlazorAtoms.Buttons</c> has <c>ButtonAppearance</c>, whose
/// Solid / Soft / Outline members are deliberately the same three. The parameter is still called
/// <c>Appearance</c>.
/// </remarks>
public enum BadgeAppearance
{
    /// <summary>Solid accent fill with contrasting text (no border).</summary>
    Solid,
    /// <summary>Low-opacity tint of the accent as background, accent-colored text (no border).</summary>
    Soft,
    /// <summary>Transparent fill, accent-colored text and a 1px accent border.</summary>
    Outline,
}
