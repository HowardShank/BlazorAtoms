namespace BlazorAtoms.Inputs;

/// <summary>
/// Shape of <see cref="AtomRangeInput{TValue}"/>'s drag handle. Purely CSS-driven via
/// <c>data-handle-shape</c> on the native input — new shapes can be added without changing the
/// component's C# surface or markup.
/// </summary>
public enum HandleShape
{
    /// <summary>Default circular handle.</summary>
    Round,

    /// <summary>Square handle with slightly rounded corners.</summary>
    Square,
}
