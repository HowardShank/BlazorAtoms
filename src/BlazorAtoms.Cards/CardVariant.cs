namespace BlazorAtoms.Cards;

/// <summary>Frame treatment for <see cref="AtomCard"/> — how the card separates itself from the page.
/// Independent of <see cref="CardElevation"/>, which controls only how far it appears to lift.</summary>
/// <remarks>
/// Prefixed <c>Card*</c> per the repo convention that a cross-package enum name carries its package's
/// noun — <c>BadgeVariant</c>, <c>ButtonVariant</c>, <c>InputVariant</c>, <c>ProgressVariant</c> and
/// this one would otherwise all be a bare <c>Variant</c>. The parameter is still called
/// <c>Variant</c>.
/// </remarks>
public enum CardVariant
{
    /// <summary>Background plus a shadow, no border. Default.</summary>
    Elevated,

    /// <summary>Background plus a 1px border, no shadow.</summary>
    Outlined,

    /// <summary>Tinted background, no border, no shadow.</summary>
    Filled,

    /// <summary>No background, border or shadow — structure and spacing only.</summary>
    Flat,
}
