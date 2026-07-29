namespace BlazorAtoms.Progress;

/// <summary>Density preset — drives the default track thickness, step-marker diameter and label
/// font size. An explicit <c>Thickness</c> still overrides the thickness part.</summary>
/// <remarks>Prefixed <c>Progress*</c> per the repo's package-noun convention (see
/// <see cref="ProgressVariant"/>); the parameter is still called <c>Size</c>.</remarks>
public enum ProgressSize
{
    Small,
    Medium,
    Large,
}
