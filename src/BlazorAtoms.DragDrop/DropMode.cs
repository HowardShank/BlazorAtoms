namespace BlazorAtoms.DragDrop;

/// <summary>
/// How a dropped item settles into the target list. Governs <see cref="DropzoneEngine"/> and the
/// component's on-drop behavior.
/// </summary>
public enum DropMode
{
    /// <summary>Insert the active item at the target slot; existing items shift.</summary>
    InsertBefore,

    /// <summary>Move the active item onto the target's slot and place the target where the active
    /// item came from — one-for-one swap. Only meaningful within a single list.</summary>
    Swap,

    /// <summary>Swap immediately on drag-hover (WYSIWYG) rather than on drop.</summary>
    InstantReplace,
}
