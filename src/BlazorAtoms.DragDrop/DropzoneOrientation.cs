namespace BlazorAtoms.DragDrop;

/// <summary>
/// Layout direction of a <see cref="AtomDropzone{TItem}"/>. Drives the scoped CSS grid/flex mode
/// so the spacer bars sit on the correct axis.
/// </summary>
public enum DropzoneOrientation
{
    /// <summary>Column layout — items stack top-to-bottom, spacers are horizontal bars.</summary>
    Vertical,

    /// <summary>Row layout — items sit left-to-right, spacers are vertical bars.</summary>
    Horizontal,

    /// <summary>Wrapped grid layout — items flow left-to-right and wrap.</summary>
    Grid,
}
