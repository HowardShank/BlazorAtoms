namespace BlazorAtoms.Tabs;

/// <summary>How the tabs are distributed along the strip. Maps onto <c>justify-content</c>, except
/// <see cref="Stretch"/>, which grows the tabs themselves.</summary>
public enum TabsAlign
{
    /// <summary>Packed at the start of the strip. Default.</summary>
    Start,

    /// <summary>Centered in the strip.</summary>
    Center,

    /// <summary>Packed at the end of the strip.</summary>
    End,

    /// <summary>Tabs share the full width equally — the usual look for a segmented
    /// <see cref="TabsVariant.Bar"/>.</summary>
    Stretch,
}
