namespace BlazorAtoms.Cards;

/// <summary>How far <see cref="AtomCard"/> appears to lift off the page. Only visible on
/// <see cref="CardVariant.Elevated"/>, which is the one variant that draws a shadow at all.</summary>
public enum CardElevation
{
    /// <summary>No shadow.</summary>
    None,

    /// <summary>Hairline shadow — barely off the page.</summary>
    Small,

    /// <summary>Default resting shadow.</summary>
    Medium,

    /// <summary>Pronounced shadow, for a card that floats over content.</summary>
    Large,
}
