namespace BlazorAtoms.Tabs;

/// <summary>Density preset — drives tab padding, font size and indicator thickness →
/// <c>data-size</c>.</summary>
/// <remarks>Prefixed <c>Tabs*</c> per the repo's package-noun convention (see
/// <see cref="TabsVariant"/>); the parameter is still called <c>Size</c>.</remarks>
public enum TabsSize
{
    Small,
    Medium,
    Large,
}
